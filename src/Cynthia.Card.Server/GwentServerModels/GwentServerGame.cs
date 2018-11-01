using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alsein.Utilities;

namespace Cynthia.Card.Server
{
    public class GwentServerGame : IGwentServerGame, IGwentEvent
    {
        public Player[] Players { get; set; } = new Player[2]; //玩家数据传输/
        public bool[] IsPlayersLeader { get; set; } = { true, true };//玩家领袖是否可用/
        public IList<GameCard>[] PlayersLeader { get; set; } = new IList<GameCard>[2];//玩家领袖是?/
        public TwoPlayer GameRound { get; set; }//谁的的回合----
        public int RoundCount { get; set; } = 0;//有效比分的回合数
        public int CurrentRoundCount { get; set; } = 0;//当前小局
        public int[] PlayersWinCount { get; set; } = new int[2] { 0, 0 };//玩家胜利场数/
        public int[][] PlayersRoundResult { get; set; } = new int[3][];//三局r1 r2 r3
        public IList<GameCard>[] PlayersDeck { get; set; } = new IList<GameCard>[2];//玩家卡组/
        public IList<GameCard>[] PlayersHandCard { get; set; } = new IList<GameCard>[2];//玩家手牌/
        public IList<GameCard>[][] PlayersPlace { get; set; } = new IList<GameCard>[2][];//玩家场地/
        public RowStatus[][] GameRowStatus { get; set; } = new RowStatus[2][];//玩家天气
        public IList<GameCard>[] PlayersCemetery { get; set; } = new IList<GameCard>[2];//玩家墓地/
        public IList<GameCard>[] PlayersStay { get; set; } = new IList<GameCard>[2];//玩家悬牌
        public Faction[] PlayersFaction { get; set; } = new Faction[2];//玩家们的势力
        public bool[] IsPlayersPass { get; set; } = new bool[2] { false, false };
        public bool[] IsPlayersMulligan { get; set; } = new bool[2] { false, false };
        public int Player1Index { get; } = 0;
        public int Player2Index { get; } = 1;
        public async Task<bool> Play()
        {
            //###游戏开始###
            //双方抽牌10张
            await LogicDrawCard(Player1Index, 10);//不会展示动画的,逻辑层抽牌
            await LogicDrawCard(Player2Index, 10);
            await SetAllInfo();//更新玩家所有数据
            //----------------------------------------------------------------------------------------
            await PlayerBigRound(3, 3);//双方轮流执行回合|第一小局
            await DrawCard(2, 2);
            await PlayerBigRound(2, 2);//双方轮流执行回合|第二小局
            if (PlayersWinCount[Player1Index] < 2 && PlayersWinCount[Player2Index] < 2)//如果前两局没有分出结果
            {
                await DrawCard(1, 1);
                await PlayerBigRound(1, 1);//双方轮流执行回合|第三小局
            }
            //-----------------------------------------------------------------------------------------
            await GameOverExecute();//发送游戏结束信息
            return true;
        }
        public async Task BigRoundEnd()//小局结束,进行收场
        {
            await Task.Delay(500);
            var player1Row1Point = PlayersPlace[Player1Index][0].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus);
            var player1Row2Point = PlayersPlace[Player1Index][1].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus);
            var player1Row3Point = PlayersPlace[Player1Index][2].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus);
            var player2Row1Point = PlayersPlace[Player2Index][0].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus);
            var player2Row2Point = PlayersPlace[Player2Index][1].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus);
            var player2Row3Point = PlayersPlace[Player2Index][2].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus);
            var player1PlacePoint = (player1Row1Point + player1Row2Point + player1Row3Point);
            var player2PlacePoint = (player2Row1Point + player2Row2Point + player2Row3Point);
            PlayersRoundResult[CurrentRoundCount][Player1Index] = player1PlacePoint;
            PlayersRoundResult[CurrentRoundCount][Player2Index] = player2PlacePoint;
            if (player1PlacePoint >= player2PlacePoint)
            {
                GameRound = TwoPlayer.Player1;
                PlayersWinCount[Player1Index]++;
            }
            if (player2PlacePoint >= player1PlacePoint)
            {
                GameRound = TwoPlayer.Player2;
                PlayersWinCount[Player2Index]++;
            }
            RoundCount++;//有效回合的总数
            CurrentRoundCount++;//当前回合
            IsPlayersPass[Player1Index] = false;
            IsPlayersPass[Player2Index] = false;
            await SetWinCountInfo();//设置小皇冠图标
            await SetPassInfo();//重置pass标记
            //首先应该先表示信息
            //-+/*/展示点数
            await Task.WhenAll
            (
                Players[Player1Index].SendAsync(ServerOperationType.BigRoundShowPoint, new BigRoundInfomation()
                {
                    MyPoint = player1PlacePoint,
                    EnemyPoint = player2PlacePoint,
                    MyWinCount = PlayersWinCount[Player1Index],
                    EnemyWinCount = PlayersWinCount[Player2Index],
                    GameStatus = player2PlacePoint == player1PlacePoint ? GameStatus.Draw :
                    (player2PlacePoint > player1PlacePoint ? GameStatus.Lose : GameStatus.Win),
                    Title = player2PlacePoint == player1PlacePoint ? "本局平局" :
                    (player2PlacePoint > player1PlacePoint ? "本局失败!" : "本局胜利!")
                }),
                Players[Player2Index].SendAsync(ServerOperationType.BigRoundShowPoint, new BigRoundInfomation()
                {
                    MyPoint = player2PlacePoint,
                    EnemyPoint = player1PlacePoint,
                    MyWinCount = PlayersWinCount[Player2Index],
                    EnemyWinCount = PlayersWinCount[Player1Index],
                    GameStatus = player2PlacePoint == player1PlacePoint ? GameStatus.Draw :
                    (player2PlacePoint < player1PlacePoint ? GameStatus.Lose : GameStatus.Win),
                    Title = player2PlacePoint == player1PlacePoint ? "本局平局" :
                    (player2PlacePoint < player1PlacePoint ? "本局失败!" : "本局胜利!"),
                })
            );
            await Task.Delay(1500);
            if (PlayersWinCount[Player1Index] >= 2 || PlayersWinCount[Player2Index] >= 2)//如果前两局没有分出结果
            {
                await Task.WhenAll(Players[Player1Index].SendAsync(ServerOperationType.BigRoundShowClose)
                            , Players[Player2Index].SendAsync(ServerOperationType.BigRoundShowClose));
                return;
            }
            //-+/*/展示信息
            await Task.WhenAll(Players[Player1Index].SendAsync(ServerOperationType.BigRoundSetMessage, RoundCount <= 1 ? "第 2 小局开始!" : "决胜局开始!")
                            , Players[Player2Index].SendAsync(ServerOperationType.BigRoundSetMessage, RoundCount <= 1 ? "第 2 小局开始!" : "决胜局开始!"));
            await Task.Delay(1500);
            await Task.WhenAll(Players[Player1Index].SendAsync(ServerOperationType.BigRoundShowClose)
                            , Players[Player2Index].SendAsync(ServerOperationType.BigRoundShowClose));
            await Task.Delay(100);
            //888888888888888888888888888888888888888888888888888888888888888888888888
            await OnRoundOver(RoundCount, player1PlacePoint, player2PlacePoint);
            //888888888888888888888888888888888888888888888888888888888888888888888888
            await SendBigRoundEndToCemetery();//将所有牌移到墓地
            await Task.WhenAll(SetCemeteryInfo(Player1Index), SetCemeteryInfo(Player2Index));
            //清空所有场上的牌
        }
        //进行一轮回合
        public async Task<bool> PlayerRound()
        {
            //判断这是谁的回合
            var playerIndex = GameRound == TwoPlayer.Player1 ? Player1Index : Player2Index;
            //切换回合
            //----------------------------------------------------
            //这里是回合开始卡牌(如剑船)的逻辑和动画<待补充>
            //888888888888888888888888888888888888888888888888888888888888888888888888
            await OnTurnStart(playerIndex);
            //888888888888888888888888888888888888888888888888888888888888888888888888
            //----------------------------------------------------
            //这是硬币动画
            await Players[playerIndex].SendAsync(ServerOperationType.SetCoinInfo, true);
            await Players[AnotherPlayer(playerIndex)].SendAsync(ServerOperationType.SetCoinInfo, false);
            if (!IsPlayersPass[playerIndex])
                await Players[playerIndex].SendAsync(ServerOperationType.RemindYouRoundStart);
            await Task.Delay(500);

            //判断当前是否已经处于pass状态
            if (IsPlayersPass[playerIndex])
            {
                //如果双方都pass...小局结束
                if (IsPlayersPass[AnotherPlayer(playerIndex)] == true)
                    return false;
                return true;
            }
            else if (PlayersHandCard[playerIndex].Count + (IsPlayersLeader[playerIndex] ? 1 : 0) == 0)
            {//如果没有手牌,强制pass
                IsPlayersPass[playerIndex] = true;
                await SetPassInfo();
                //888888888888888888888888888888888888888888888888888888888888888888888888
                await OnPlayerPass(playerIndex);
                //888888888888888888888888888888888888888888888888888888888888888888888888
                if (IsPlayersPass[AnotherPlayer(playerIndex)] == true)
                {
                    //如果对方也pass,结束游戏
                    return false;
                }
                return true;
            }
            //让玩家选择拖拽,或者Pass
            await Players[playerIndex].SendAsync(ServerOperationType.GetDragOrPass);
            //获取信息
            var roundInfo = (await Players[playerIndex].ReceiveAsync()).Arguments.ToArray()[0].ToType<string>().ToType<RoundInfo>();//接收玩家的选择,提取结果
            if (roundInfo.IsPass)
            {//Pass时候执行
                IsPlayersPass[playerIndex] = true;
                await SetPassInfo();
                //888888888888888888888888888888888888888888888888888888888888888888888888
                await OnPlayerPass(playerIndex);
                //888888888888888888888888888888888888888888888888888888888888888888888888
                //判断对手是否pass
                if (IsPlayersPass[AnotherPlayer(playerIndex)] == true)
                {
                    return false;
                }
            }
            else
            {//放置卡牌(单位和法术都是)时执行
             //以上应该不需要改变,至少不是大改动(动画,pass判断之类的)
                await RoundPlayCard(playerIndex, roundInfo);
                //宣告双方效果结束#########################
                //可能会变更, 计划封装到卡牌效果中
                //########################################
                await Task.Delay(400);
            }
            //宣告回合结束(应该不需要更改)
            //await Players[playerIndex].SendAsync(ServerOperationType.RoundEnd);
            return true;
        }

        public async Task PlayerBigRound(int player1Mulligan = 0, int player2Mulligan = 0)
        {
            await Task.WhenAll(MulliganCard(Player1Index, player1Mulligan), MulliganCard(Player2Index, player2Mulligan));
            while (await PlayerRound())
            {
                await OnTurnOver(TwoPlayerToPlayerIndex(GameRound));
                GameRound = ((GameRound == TwoPlayer.Player1) ? TwoPlayer.Player2 : TwoPlayer.Player1);
            }
            await BigRoundEnd();
        }
        public async Task RoundPlayCard(int playerIndex, RoundInfo cardInfo)//哪一位玩家,打出第几张手牌,打到了第几排,第几列
        {   //获取卡牌,手牌或者领袖,将那个GameCard存起来
            var card = cardInfo.HandCardIndex == -1 ? PlayersLeader[playerIndex][0] : PlayersHandCard[playerIndex][cardInfo.HandCardIndex];
            //如果打出的是领袖,那么设定领袖已经被打出
            if (cardInfo.HandCardIndex == -1)
                IsPlayersLeader[playerIndex] = false;
            //如果是直接丢墓地,触发丢墓地方法
            if (cardInfo.CardLocation.RowPosition == RowPosition.MyCemetery)
                await card.Effect.ToCemetery();
            else
            {   //如果是法术,使用,如果是单位,打出
                if (cardInfo.CardLocation.RowPosition == RowPosition.SpecialPlace)
                    await card.Effect.CardUse();
                else
                    await card.Effect.Play(cardInfo.CardLocation);
            }
        }
        //玩家抽卡
        public async Task<IList<GameCard>> LogicDrawCard(int playerIndex, int count)//或许应该播放抽卡动画和更新数值
        {
            if (count > PlayersDeck[playerIndex].Count) count = PlayersDeck[playerIndex].Count;
            var list = new List<GameCard>();
            for (var i = 0; i < count; i++)
            {
                //将卡组顶端的卡牌抽到手牌
                (await LogicCardMove(PlayersDeck[playerIndex], 0, PlayersHandCard[playerIndex], 0)).To(list.Add);
            }
            return list;
        }

        //封装的抽卡
        public async Task<(List<GameCard>, List<GameCard>)> DrawCard(int player1Count, int player2Count)
        {
            //抽卡限制,不至于抽空卡组
            if (player1Count > PlayersDeck[Player1Index].Count) player1Count = PlayersDeck[Player1Index].Count;
            if (player2Count > PlayersDeck[Player2Index].Count) player2Count = PlayersDeck[Player2Index].Count;
            var player1Task = DrawCardAnimation(Player1Index, player1Count, Player2Index, player2Count);
            var player2Task = DrawCardAnimation(Player2Index, player2Count, Player1Index, player1Count);
            await Task.WhenAll(player1Task, player2Task);
            await SetCountInfo();
            return (player1Task.Result, player2Task.Result);
        }
        public async Task<List<GameCard>> DrawCardAnimation(int myPlayerIndex, int myPlayerCount, int enemyPlayerIndex, int enemyPlayerCount)
        {
            var list = new List<GameCard>();
            for (var i = 0; i < myPlayerCount; i++)
            {
                //await GetCardFrom(myPlayerIndex, RowPosition.MyDeck, RowPosition.MyStay, 0, PlayersDeck[myPlayerIndex][0].CardStatus);
                await SendCardMove(myPlayerIndex, new MoveCardInfo()
                {
                    Soure = new CardLocation() { RowPosition = RowPosition.MyDeck },
                    Taget = new CardLocation() { RowPosition = RowPosition.MyStay, CardIndex = 0 },
                    Card = PlayersDeck[myPlayerIndex][0].Status
                });
                //真实抽的卡只有自己的
                var drawcard = (await LogicDrawCard(myPlayerIndex, 1)).Single();
                list.Add(drawcard);
                await Task.Delay(800);
                await SendCardMove(myPlayerIndex, new MoveCardInfo()
                {
                    Soure = new CardLocation() { RowPosition = RowPosition.MyStay, CardIndex = 0 },
                    Taget = new CardLocation() { RowPosition = RowPosition.MyHand, CardIndex = 0 },
                });
                await Task.Delay(300);
                //88888888888888888888888888888888888888888888888888888
                await OnPlayerDraw(myPlayerIndex, drawcard);
                //88888888888888888888888888888888888888888888888888888
            }
            for (var i = 0; i < enemyPlayerCount; i++)
            {
                await SendCardMove(myPlayerIndex, new MoveCardInfo()
                {
                    Soure = new CardLocation() { RowPosition = RowPosition.EnemyDeck },
                    Taget = new CardLocation() { RowPosition = RowPosition.EnemyStay, CardIndex = 0 },
                    Card = new CardStatus() { IsCardBack = true, DeckFaction = PlayersFaction[enemyPlayerIndex] }
                });
                //await GetCardFrom(myPlayerIndex, RowPosition.EnemyDeck, RowPosition.EnemyStay, 0, new CardStatus() { IsCardBack = true, DeckFaction = PlayersFaction[enemyPlayerIndex] });
                await Task.Delay(400);
                await SendCardMove(myPlayerIndex, new MoveCardInfo()
                {
                    Soure = new CardLocation() { RowPosition = RowPosition.EnemyStay, CardIndex = 0 },
                    Taget = new CardLocation() { RowPosition = RowPosition.EnemyHand, CardIndex = 0 },
                });
                //await SetCardTo(myPlayerIndex, RowPosition.EnemyStay, 0, RowPosition.EnemyHand, 0);
                await Task.Delay(300);
            }
            return list;
        }

        //封装的调度
        public async Task MulliganCard(int playerIndex, int count)
        {
            if (PlayersDeck[playerIndex].Count <= 0)
                return;
            await Players[playerIndex].SendAsync(ServerOperationType.MulliganStart, PlayersHandCard[playerIndex].Select(x => x.Status), count);
            IsPlayersMulligan[playerIndex] = true;
            await SetMulliganInfo();
            for (var i = 0; i < count; i++)
            {
                await Players[playerIndex].SendAsync(ServerOperationType.GetMulliganInfo);
                var mulliganCardIndex = (await Players[playerIndex].ReceiveAsync()).Arguments.ToArray()[0].ToType<string>().ToType<int>();
                if (mulliganCardIndex == -1)
                    break;
                //逻辑处理
                //总总总之！先关掉揭示啦！
                PlayersHandCard[playerIndex][mulliganCardIndex].Status.IsReveal = false;
                //当然调度走揭示单位,要给对手说一声啦
                await Players[AnotherPlayer(playerIndex)].SendAsync
                (
                    ServerOperationType.SetCard,
                    new CardLocation()
                    {
                        RowPosition = RowPosition.EnemyHand,
                        CardIndex = mulliganCardIndex
                    },
                    new CardStatus() { IsCardBack = true, DeckFaction = PlayersHandCard[playerIndex][mulliganCardIndex].Status.DeckFaction }
                );
                //将手牌中需要调度的牌,移动到卡组最后
                await LogicCardMove(PlayersHandCard[playerIndex], mulliganCardIndex, PlayersDeck[playerIndex], PlayersDeck[playerIndex].Count);
                //将卡组中第一张牌抽到手牌调度走的位置
                var card = (await LogicCardMove(PlayersDeck[playerIndex], 0, PlayersHandCard[playerIndex], mulliganCardIndex));
                await Players[playerIndex].SendAsync(ServerOperationType.MulliganData, mulliganCardIndex, card.Status);
            }
            await Task.Delay(500);
            await Players[playerIndex].SendAsync(ServerOperationType.MulliganEnd);
            IsPlayersMulligan[playerIndex] = false;
            await SetMulliganInfo();
        }
        //----------------------------------------------------------------------------------------------------------------------
        //<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>
        //几个从用户那里获得信息的途径
        public Task<IList<int>> GetSelectMenuCards(int playerIndex, IList<CardStatus> selectList, int selectCount = 1, bool isCanOver = false, string title = "选择一张卡牌")//返回点击列表卡牌的顺序
        {
            return GetSelectMenuCards(playerIndex, new MenuSelectCardInfo() { SelectList = selectList, SelectCount = selectCount, IsCanOver = isCanOver, Title = title });
        }
        public async Task<IList<GameCard>> GetSelectMenuCards(int playerIndex, IList<GameCard> selectList, int selectCount = 1, string title = "选择一张卡牌", bool isEnemyBack = true, bool isCanOver = true)//返回点击列表卡牌的顺序
        {
            selectCount = selectCount > selectList.Count() ? selectList.Count() : selectCount;
            return
            (
                await GetSelectMenuCards
                (
                    playerIndex,
                    new MenuSelectCardInfo()
                    {
                        SelectList = selectList.Select(x =>
                        {
                            return (!isEnemyBack || x.PlayerIndex == playerIndex) ? x.Status : new CardStatus() { DeckFaction = x.Status.DeckFaction, IsCardBack = true };
                        }).ToList(),
                        SelectCount = selectCount,
                        IsCanOver = isCanOver,
                        Title = title
                    }
                )
            ).Select(x => selectList[x]).ToList();
        }
        public async Task<IList<int>> GetSelectMenuCards(int playerIndex, MenuSelectCardInfo info)
        {
            await Players[playerIndex].SendAsync(ServerOperationType.SelectMenuCards, info);
            return (await Players[playerIndex].ReceiveAsync()).Arguments.ToArray()[0].ToType<string>().ToType<IList<int>>();
        }
        public async Task<IList<CardLocation>> GetSelectPlaceCards(int playerIndex, PlaceSelectCardsInfo info)//指示器向边缘扩展格数
        {
            await Players[playerIndex].SendAsync(ServerOperationType.SelectPlaceCards, info);
            return (await Players[playerIndex].ReceiveAsync()).Arguments.ToArray()[0].ToType<string>().ToType<IList<CardLocation>>();
        }
        public async Task<RowPosition> GetSelectRow(int playerIndex, CardLocation selectCard, IList<RowPosition> rowPart)//选择排
        {
            await Players[playerIndex].SendAsync(ServerOperationType.SelectRow, rowPart, selectCard);
            return (await Players[playerIndex].ReceiveAsync()).Arguments.ToArray()[0].ToType<string>().ToType<RowPosition>();
        }
        public async Task<CardLocation> GetPlayCard(GameCard card, bool isAnother = false)//选择放置一张牌
        {
            var playerIndex = isAnother ? AnotherPlayer(card.PlayerIndex) : card.PlayerIndex;
            await Players[playerIndex].SendAsync(ServerOperationType.PlayCard, GetCardLocation(playerIndex, card));
            return (await Players[playerIndex].ReceiveAsync()).Arguments.ToArray()[0].ToType<string>().ToType<CardLocation>();
        }
        //<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>
        //------------------------------------------------------------------------------------------------------------------------
        //下面是发送数据包,或者进行一些初始化信息
        //根据当前信息,处理游戏结果

        public async Task<IList<GameCard>> GetSelectPlaceCards(int count, GameCard card, Func<GameCard, bool> sizer = null, SelectModeType SelectMode = SelectModeType.AllRow, CardType selectType = CardType.Unit, int range = 0)
        {
            //自定义规则, 是否过滤特殊卡, 过滤自身
            var canSelect = GetGameCardsPart(card.PlayerIndex,
            (
                x => (sizer == null ? (true) : sizer(x)) &&
                (!x.Status.IsImmue) &&//如果没有免疫
                (selectType == CardType.Unit ? (GwentMap.CardMap[x.Status.CardId].CardType == CardType.Unit) :
                (selectType == CardType.Special ? (GwentMap.CardMap[x.Status.CardId].CardType == CardType.Special) : true)) &&
                (x != card)
            ), SelectMode);
            if (GameCardsPartCount(canSelect) < count) count = GameCardsPartCount(canSelect);
            if (count <= 0)
                return new List<GameCard>();
            //落雷术测试
            var taget = await GetSelectPlaceCards
            (
                card.PlayerIndex,
                new PlaceSelectCardsInfo()
                {
                    CanSelect = canSelect,
                    SelectCard = GetCardLocation(card.PlayerIndex, card),
                    SelectCount = count,
                    Range = range
                }
            );
            return taget.Select(x => GetCard(card.PlayerIndex, x)).ToList();
        }
        //将某个列表中的元素,移动到另一个列表的某个位置,然后返回被移动的元素     
        public async Task<GameCard> LogicCardMove(IList<GameCard> soure, int soureIndex, IList<GameCard> taget, int tagetIndex)
        {
            var player1SoureRow = ListToRow(Player1Index, soure);
            var player1TagetRow = ListToRow(Player1Index, soure);

            var item = soure[soureIndex];
            soure.RemoveAt(soureIndex);
            taget.Insert(tagetIndex, item);
            item.Status.CardRow = ListToRow(WhoRow(taget), taget);
            item.PlayerIndex = WhoRow(taget);

            if (player1SoureRow.IsInCemetery() || player1TagetRow.IsInCemetery())
            {
                await SetCemeteryInfo(Player1Index);
                await SetCemeteryInfo(Player2Index);
            }
            return item;
        }
        public async Task GameOverExecute()
        {
            if (PlayersRoundResult[0][Player1Index] >= PlayersRoundResult[0][Player2Index])
                PlayersWinCount[Player1Index]++;
            if (PlayersRoundResult[0][Player1Index] <= PlayersRoundResult[0][Player2Index])
                PlayersWinCount[Player2Index]++;
            if (PlayersRoundResult[1][Player1Index] >= PlayersRoundResult[1][Player2Index])
                PlayersWinCount[Player1Index]++;
            if (PlayersRoundResult[1][Player1Index] <= PlayersRoundResult[1][Player2Index])
                PlayersWinCount[Player2Index]++;
            if (PlayersRoundResult[2][Player1Index] >= PlayersRoundResult[2][Player2Index])
                PlayersWinCount[Player1Index]++;
            if (PlayersRoundResult[2][Player1Index] <= PlayersRoundResult[2][Player2Index])
                PlayersWinCount[Player2Index]++;
            await SendGameResult(TwoPlayer.Player1);
            await SendGameResult(TwoPlayer.Player2);
        }
        public IList<GameCard> RowToList(int myPlayerIndex, RowPosition row)
        {
            var enemyPlayerIndex = (AnotherPlayer(myPlayerIndex));
            switch (row)
            {
                case RowPosition.MyHand:
                    return PlayersHandCard[myPlayerIndex];
                case RowPosition.EnemyHand:
                    return PlayersHandCard[enemyPlayerIndex];
                case RowPosition.MyDeck:
                    return PlayersDeck[myPlayerIndex];
                case RowPosition.EnemyDeck:
                    return PlayersDeck[enemyPlayerIndex];
                case RowPosition.MyCemetery:
                    return PlayersCemetery[myPlayerIndex];
                case RowPosition.EnemyCemetery:
                    return PlayersCemetery[enemyPlayerIndex];
                case RowPosition.MyRow1:
                    return PlayersPlace[myPlayerIndex][0];
                case RowPosition.EnemyRow1:
                    return PlayersPlace[enemyPlayerIndex][0];
                case RowPosition.MyRow2:
                    return PlayersPlace[myPlayerIndex][1];
                case RowPosition.EnemyRow2:
                    return PlayersPlace[enemyPlayerIndex][1];
                case RowPosition.MyRow3:
                    return PlayersPlace[myPlayerIndex][2];
                case RowPosition.EnemyRow3:
                    return PlayersPlace[enemyPlayerIndex][2];
                case RowPosition.MyStay:
                    return PlayersStay[myPlayerIndex];
                case RowPosition.EnemyStay:
                    return PlayersStay[enemyPlayerIndex];
                case RowPosition.MyLeader:
                    return PlayersLeader[myPlayerIndex];
                case RowPosition.EnemyLeader:
                    return PlayersLeader[enemyPlayerIndex];
                default:
                    return null;
            }
        }
        public RowPosition ListToRow(int myPlayerIndex, IList<GameCard> list)
        {//这一行对于这个玩家是哪一行
            var enemyPlayerIndex = AnotherPlayer(myPlayerIndex);
            if (list == PlayersHandCard[myPlayerIndex])
                return RowPosition.MyHand;
            if (list == PlayersHandCard[enemyPlayerIndex])
                return RowPosition.EnemyHand;
            //
            if (list == PlayersDeck[myPlayerIndex])
                return RowPosition.MyDeck;
            if (list == PlayersDeck[enemyPlayerIndex])
                return RowPosition.EnemyDeck;
            //
            if (list == PlayersCemetery[myPlayerIndex])
                return RowPosition.MyCemetery;
            if (list == PlayersCemetery[enemyPlayerIndex])
                return RowPosition.EnemyCemetery;
            //
            if (list == PlayersPlace[myPlayerIndex][0])
                return RowPosition.MyRow1;
            if (list == PlayersPlace[enemyPlayerIndex][0])
                return RowPosition.EnemyRow1;
            //
            if (list == PlayersPlace[myPlayerIndex][1])
                return RowPosition.MyRow2;
            if (list == PlayersPlace[enemyPlayerIndex][1])
                return RowPosition.EnemyRow2;
            //
            if (list == PlayersPlace[myPlayerIndex][2])
                return RowPosition.MyRow3;
            if (list == PlayersPlace[enemyPlayerIndex][2])
                return RowPosition.EnemyRow3;
            //
            if (list == PlayersStay[myPlayerIndex])
                return RowPosition.MyStay;
            if (list == PlayersStay[enemyPlayerIndex])
                return RowPosition.EnemyStay;
            //
            //
            if (list == PlayersLeader[myPlayerIndex])
                return RowPosition.MyLeader;
            if (list == PlayersLeader[enemyPlayerIndex])
                return RowPosition.EnemyLeader;
            //
            return RowPosition.SpecialPlace;
        }
        public int WhoRow(IList<GameCard> list)
        {
            if (ListToRow(Player1Index, list).IsMyRow())
                return Player1Index;
            else
                return Player2Index;
        }
        //另一个玩家
        public CardLocation GetCardLocation(int playerIndex, GameCard card)
        {
            var row = (playerIndex == card.PlayerIndex ? card.Status.CardRow : card.Status.CardRow.RowMirror());
            var list = RowToList(playerIndex, row);
            return new CardLocation()
            {
                RowPosition = row,
                CardIndex = list.IndexOf(card)
            };
        }
        public int AnotherPlayer(int playerIndex) => playerIndex == Player1Index ? Player2Index : Player1Index;
        public async Task Debug(string msg)
        {
            await Players[Player1Index].SendAsync(ServerOperationType.Debug, msg);
            await Players[Player2Index].SendAsync(ServerOperationType.Debug, msg);
        }
        public GameCardsPart GetGameCardsPart(int playerIndex, Func<GameCard, bool> Sizer, SelectModeType selectMode = SelectModeType.All)
        {   //根据游戏与条件,筛选出符合条件的选择对象
            var cardsPart = new GameCardsPart();
            if (selectMode.IsHaveMy())
            {
                if (selectMode.IsHaveHand())
                    PlayersHandCard[playerIndex].Select((x, i) => (x: x, i: i)).Where(x => Sizer(x.x)).ForAll(item => cardsPart.MyHandCards.Add(item.i));
                if (selectMode.IsHaveRow())
                {
                    PlayersPlace[playerIndex][0].Select((x, i) => (x: x, i: i)).Where(x => Sizer(x.x)).ForAll(item => cardsPart.MyRow1Cards.Add(item.i));
                    PlayersPlace[playerIndex][1].Select((x, i) => (x: x, i: i)).Where(x => Sizer(x.x)).ForAll(item => cardsPart.MyRow2Cards.Add(item.i));
                    PlayersPlace[playerIndex][2].Select((x, i) => (x: x, i: i)).Where(x => Sizer(x.x)).ForAll(item => cardsPart.MyRow3Cards.Add(item.i));
                }
            }
            if (selectMode.IsHaveEnemy())
            {
                if (selectMode.IsHaveHand())
                    PlayersHandCard[AnotherPlayer(playerIndex)].Select((x, i) => (x: x, i: i)).Where(x => Sizer(x.x)).ForAll(item => cardsPart.EnemyHandCards.Add(item.i));
                if (selectMode.IsHaveRow())
                {
                    PlayersPlace[AnotherPlayer(playerIndex)][0].Select((x, i) => (x: x, i: i)).Where(x => Sizer(x.x)).ForAll(item => cardsPart.EnemyRow1Cards.Add(item.i));
                    PlayersPlace[AnotherPlayer(playerIndex)][1].Select((x, i) => (x: x, i: i)).Where(x => Sizer(x.x)).ForAll(item => cardsPart.EnemyRow2Cards.Add(item.i));
                    PlayersPlace[AnotherPlayer(playerIndex)][2].Select((x, i) => (x: x, i: i)).Where(x => Sizer(x.x)).ForAll(item => cardsPart.EnemyRow3Cards.Add(item.i));
                }
            }
            return cardsPart;
        }
        public int GameCardsPartCount(GameCardsPart part)
        {
            var count = 0;
            if (part.IsSelectEnemyLeader) count++;
            if (part.IsSelectMyLeader) count++;
            count += part.MyHandCards.Count();
            count += part.MyRow1Cards.Count();
            count += part.MyRow2Cards.Count();
            count += part.MyRow3Cards.Count();
            count += part.MyStayCards.Count();
            count += part.EnemyHandCards.Count();
            count += part.EnemyRow1Cards.Count();
            count += part.EnemyRow2Cards.Count();
            count += part.EnemyRow3Cards.Count();
            count += part.EnemyStayCards.Count();
            return count;
        }
        public GameCardsPart MirrorGameCardsPart(GameCardsPart part)
        {
            var cardsPart = new GameCardsPart();
            cardsPart.IsSelectMyLeader = part.IsSelectEnemyLeader;
            cardsPart.IsSelectEnemyLeader = part.IsSelectMyLeader;
            part.MyHandCards.ForAll(cardsPart.EnemyHandCards.Add);
            part.MyRow1Cards.ForAll(cardsPart.EnemyRow1Cards.Add);
            part.MyRow2Cards.ForAll(cardsPart.EnemyRow2Cards.Add);
            part.MyRow3Cards.ForAll(cardsPart.EnemyRow3Cards.Add);
            part.MyStayCards.ForAll(cardsPart.EnemyStayCards.Add);
            part.EnemyHandCards.ForAll(cardsPart.MyHandCards.Add);
            part.EnemyRow1Cards.ForAll(cardsPart.MyRow1Cards.Add);
            part.EnemyRow2Cards.ForAll(cardsPart.MyRow2Cards.Add);
            part.EnemyRow3Cards.ForAll(cardsPart.MyRow3Cards.Add);
            part.EnemyStayCards.ForAll(cardsPart.MyStayCards.Add);
            return cardsPart;
        }
        public GameCard GetCard(int playerIndex, CardLocation location)
        {
            return RowToList(playerIndex, location.RowPosition)[location.CardIndex];
        }
        public IList<GameCard> GetAllCard(int playerIndex)
        {
            return PlayersPlace[playerIndex][0]
            .Concat(PlayersPlace[playerIndex][1])
            .Concat(PlayersPlace[playerIndex][2])
            .Concat(PlayersHandCard[playerIndex])
            .Concat(PlayersLeader[playerIndex])
            .Concat(PlayersStay[playerIndex])
            .Concat(PlayersCemetery[playerIndex])
            .Concat(PlayersDeck[playerIndex])
            .Concat(PlayersPlace[AnotherPlayer(playerIndex)][0])
            .Concat(PlayersPlace[AnotherPlayer(playerIndex)][1])
            .Concat(PlayersPlace[AnotherPlayer(playerIndex)][2])
            .Concat(PlayersHandCard[AnotherPlayer(playerIndex)])
            .Concat(PlayersLeader[AnotherPlayer(playerIndex)])
            .Concat(PlayersStay[playerIndex])
            .Concat(PlayersCemetery[AnotherPlayer(playerIndex)])
            .Concat(PlayersDeck[AnotherPlayer(playerIndex)])
            .ToList();
        }
        //----------------------------------------------------------------------------------------------
        public Task SetAllInfo()
        {
            var player1Task = Players[Player1Index].SendAsync(ServerOperationType.SetAllInfo, GetAllInfo(TwoPlayer.Player1));
            var player2Task = Players[Player2Index].SendAsync(ServerOperationType.SetAllInfo, GetAllInfo(TwoPlayer.Player2));
            return Task.WhenAll(player1Task, player2Task);
        }
        public Task SetCemeteryInfo(int playerIndex)
        {
            var player1Task = Players[playerIndex].SendAsync(ServerOperationType.SetMyCemetery, PlayersCemetery[playerIndex].Select(x => x.Status));
            var player2Task = Players[AnotherPlayer(playerIndex)].SendAsync(ServerOperationType.SetEnemyCemetery, PlayersCemetery[playerIndex].Select(x => x.Status));
            return Task.WhenAll(player1Task, player2Task);
        }
        public Task SetGameInfo()
        {
            var player1Task = Players[Player1Index].SendAsync(ServerOperationType.SetGameInfo, GetGameInfo(TwoPlayer.Player1));
            var player2Task = Players[Player2Index].SendAsync(ServerOperationType.SetGameInfo, GetGameInfo(TwoPlayer.Player2));
            return Task.WhenAll(player1Task, player2Task);
        }
        public Task SetCardsInfo()
        {
            var player1Task = Players[Player1Index].SendAsync(ServerOperationType.SetCardsInfo, GetCardsInfo(TwoPlayer.Player1));
            var player2Task = Players[Player2Index].SendAsync(ServerOperationType.SetCardsInfo, GetCardsInfo(TwoPlayer.Player2));
            return Task.WhenAll(player1Task, player2Task);
        }
        public Task SetPointInfo()
        {
            var player1Task = Players[Player1Index].SendAsync(ServerOperationType.SetPointInfo, GetPointInfo(TwoPlayer.Player1));
            var player2Task = Players[Player2Index].SendAsync(ServerOperationType.SetPointInfo, GetPointInfo(TwoPlayer.Player2));
            return Task.WhenAll(player1Task, player2Task);
        }
        public Task SetCountInfo()
        {
            var player1Task = Players[Player1Index].SendAsync(ServerOperationType.SetCountInfo, GetCountInfo(TwoPlayer.Player1));
            var player2Task = Players[Player2Index].SendAsync(ServerOperationType.SetCountInfo, GetCountInfo(TwoPlayer.Player2));
            return Task.WhenAll(player1Task, player2Task);
        }
        public Task SetPassInfo()
        {
            var player1Task = Players[Player1Index].SendAsync(ServerOperationType.SetPassInfo, GetPassInfo(TwoPlayer.Player1));
            var player2Task = Players[Player2Index].SendAsync(ServerOperationType.SetPassInfo, GetPassInfo(TwoPlayer.Player2));
            return Task.WhenAll(player1Task, player2Task);
        }
        public Task SetMulliganInfo()
        {
            var player1Task = Players[Player1Index].SendAsync(ServerOperationType.SetMulliganInfo, GetMulliganInfo(TwoPlayer.Player1));
            var player2Task = Players[Player2Index].SendAsync(ServerOperationType.SetMulliganInfo, GetMulliganInfo(TwoPlayer.Player2));
            return Task.WhenAll(player1Task, player2Task);
        }
        public Task SetWinCountInfo()
        {
            var player1Task = Players[Player1Index].SendAsync(ServerOperationType.SetWinCountInfo, GetWinCountInfo(TwoPlayer.Player1));
            var player2Task = Players[Player2Index].SendAsync(ServerOperationType.SetWinCountInfo, GetWinCountInfo(TwoPlayer.Player2));
            return Task.WhenAll(player1Task, player2Task);
        }
        public Task SetNameInfo()
        {
            var player1Task = Players[Player1Index].SendAsync(ServerOperationType.SetNameInfo, GetNameInfo(TwoPlayer.Player1));
            var player2Task = Players[Player2Index].SendAsync(ServerOperationType.SetNameInfo, GetNameInfo(TwoPlayer.Player2));
            return Task.WhenAll(player1Task, player2Task);
        }
        //---------------------------------------------------------
        public GameInfomation GetGameInfo(TwoPlayer player)
        {
            var myPlayerIndex = (player == TwoPlayer.Player1 ? Player1Index : Player2Index);
            var enemyPlayerIndex = (player == TwoPlayer.Player1 ? Player2Index : Player1Index);
            return new GameInfomation()
            {
                MyRow1Point = PlayersPlace[myPlayerIndex][0].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                MyRow2Point = PlayersPlace[myPlayerIndex][1].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                MyRow3Point = PlayersPlace[myPlayerIndex][2].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                EnemyRow1Point = PlayersPlace[enemyPlayerIndex][0].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                EnemyRow2Point = PlayersPlace[enemyPlayerIndex][1].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                EnemyRow3Point = PlayersPlace[enemyPlayerIndex][2].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                IsMyPlayerPass = IsPlayersPass[myPlayerIndex],
                IsEnemyPlayerPass = IsPlayersPass[enemyPlayerIndex],
                MyWinCount = PlayersWinCount[myPlayerIndex],
                EnemyWinCount = PlayersWinCount[enemyPlayerIndex],
                EnemyName = Players[enemyPlayerIndex].PlayerName,
                MyName = Players[myPlayerIndex].PlayerName,
                MyDeckCount = PlayersDeck[myPlayerIndex].Count(),
                EnemyDeckCount = PlayersDeck[enemyPlayerIndex].Count(),
                MyHandCount = PlayersHandCard[myPlayerIndex].Count() + (IsPlayersLeader[myPlayerIndex] ? 1 : 0),
                EnemyHandCount = PlayersHandCard[enemyPlayerIndex].Count() + (IsPlayersLeader[enemyPlayerIndex] ? 1 : 0),
                MyCemeteryCount = PlayersCemetery[myPlayerIndex].Count(),
                EnemyCemeteryCount = PlayersCemetery[enemyPlayerIndex].Count(),
            };
        }
        public GameInfomation GetCardsInfo(TwoPlayer player)
        {
            var myPlayerIndex = (player == TwoPlayer.Player1 ? Player1Index : Player2Index);
            var enemyPlayerIndex = (player == TwoPlayer.Player1 ? Player2Index : Player1Index);
            return new GameInfomation()
            {
                IsMyLeader = IsPlayersLeader[myPlayerIndex],
                IsEnemyLeader = IsPlayersLeader[enemyPlayerIndex],
                MyLeader = PlayersLeader[myPlayerIndex][0].Status,
                EnemyLeader = PlayersLeader[enemyPlayerIndex][0].Status,
                MyHandCard = PlayersHandCard[myPlayerIndex].Select(x => x.Status),
                MyStay = PlayersStay[myPlayerIndex].Select(x => x.Status),
                EnemyStay = PlayersStay[enemyPlayerIndex].Select(x => x.Status),
                EnemyHandCard = PlayersHandCard[enemyPlayerIndex].Select(x => x.Status).Select(x => x.IsReveal ? x : new CardStatus() { IsCardBack = true, DeckFaction = PlayersFaction[enemyPlayerIndex] }),
                MyPlace = PlayersPlace[myPlayerIndex].Select(x => x.Select(c => c.Status)).ToArray(),
                EnemyPlace = PlayersPlace[enemyPlayerIndex].Select
                (
                    x => x.Select(c => c.Status).Select(item => item.Conceal ? new CardStatus() { IsCardBack = true, DeckFaction = PlayersFaction[enemyPlayerIndex] } : item)
                ).ToArray(),
                MyCemetery = PlayersCemetery[myPlayerIndex].Select(x => x.Status),
                EnemyCemetery = PlayersCemetery[enemyPlayerIndex].Select(x => x.Status),
            };
        }
        public GameInfomation GetPointInfo(TwoPlayer player)
        {
            var myPlayerIndex = (player == TwoPlayer.Player1 ? Player1Index : Player2Index);
            var enemyPlayerIndex = (player == TwoPlayer.Player1 ? Player2Index : Player1Index);
            return new GameInfomation()
            {
                MyRow1Point = PlayersPlace[myPlayerIndex][0].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                MyRow2Point = PlayersPlace[myPlayerIndex][1].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                MyRow3Point = PlayersPlace[myPlayerIndex][2].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                EnemyRow1Point = PlayersPlace[enemyPlayerIndex][0].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                EnemyRow2Point = PlayersPlace[enemyPlayerIndex][1].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                EnemyRow3Point = PlayersPlace[enemyPlayerIndex][2].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus)
            };
        }
        public GameInfomation GetCountInfo(TwoPlayer player)
        {
            var myPlayerIndex = (player == TwoPlayer.Player1 ? Player1Index : Player2Index);
            var enemyPlayerIndex = (player == TwoPlayer.Player1 ? Player2Index : Player1Index);
            return new GameInfomation()
            {//手牌/ 卡组/ 墓地/
                MyDeckCount = PlayersDeck[myPlayerIndex].Count(),
                EnemyDeckCount = PlayersDeck[enemyPlayerIndex].Count(),
                MyHandCount = PlayersHandCard[myPlayerIndex].Count() + (IsPlayersLeader[myPlayerIndex] ? 1 : 0),
                EnemyHandCount = PlayersHandCard[enemyPlayerIndex].Count() + (IsPlayersLeader[enemyPlayerIndex] ? 1 : 0),
                MyCemeteryCount = PlayersCemetery[myPlayerIndex].Count(),
                EnemyCemeteryCount = PlayersCemetery[enemyPlayerIndex].Count()
            };
        }
        public GameInfomation GetPassInfo(TwoPlayer player)
        {
            var myPlayerIndex = (player == TwoPlayer.Player1 ? Player1Index : Player2Index);
            var enemyPlayerIndex = (player == TwoPlayer.Player1 ? Player2Index : Player1Index);
            return new GameInfomation()
            {
                IsMyPlayerPass = IsPlayersPass[myPlayerIndex],
                IsEnemyPlayerPass = IsPlayersPass[enemyPlayerIndex]
            };
        }
        public GameInfomation GetMulliganInfo(TwoPlayer player)
        {
            var myPlayerIndex = (player == TwoPlayer.Player1 ? Player1Index : Player2Index);
            var enemyPlayerIndex = (player == TwoPlayer.Player1 ? Player2Index : Player1Index);
            return new GameInfomation()
            {
                IsMyPlayerMulligan = IsPlayersMulligan[myPlayerIndex],
                IsEnemyPlayerMulligan = IsPlayersMulligan[enemyPlayerIndex]
            };
        }
        public GameInfomation GetWinCountInfo(TwoPlayer player)
        {
            var myPlayerIndex = (player == TwoPlayer.Player1 ? Player1Index : Player2Index);
            var enemyPlayerIndex = (player == TwoPlayer.Player1 ? Player2Index : Player1Index);
            return new GameInfomation()
            {
                MyWinCount = PlayersWinCount[myPlayerIndex],
                EnemyWinCount = PlayersWinCount[enemyPlayerIndex]
            };
        }
        public GameInfomation GetNameInfo(TwoPlayer player)
        {
            var myPlayerIndex = (player == TwoPlayer.Player1 ? Player1Index : Player2Index);
            var enemyPlayerIndex = (player == TwoPlayer.Player1 ? Player2Index : Player1Index);
            return new GameInfomation()
            {
                EnemyName = Players[enemyPlayerIndex].PlayerName,
                MyName = Players[myPlayerIndex].PlayerName
            };
        }

        //更新所有信息
        public GameInfomation GetAllInfo(TwoPlayer player)
        {
            var myPlayerIndex = (player == TwoPlayer.Player1 ? Player1Index : Player2Index);
            var enemyPlayerIndex = (player == TwoPlayer.Player1 ? Player2Index : Player1Index);
            return new GameInfomation()
            {
                MyRow1Point = PlayersPlace[myPlayerIndex][0].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                MyRow2Point = PlayersPlace[myPlayerIndex][1].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                MyRow3Point = PlayersPlace[myPlayerIndex][2].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                EnemyRow1Point = PlayersPlace[enemyPlayerIndex][0].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                EnemyRow2Point = PlayersPlace[enemyPlayerIndex][1].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                EnemyRow3Point = PlayersPlace[enemyPlayerIndex][2].Select(x => x.Status).Sum(x => x.Strength + x.HealthStatus),
                IsMyPlayerPass = IsPlayersPass[myPlayerIndex],
                IsEnemyPlayerPass = IsPlayersPass[enemyPlayerIndex],
                MyWinCount = PlayersWinCount[myPlayerIndex],
                EnemyWinCount = PlayersWinCount[enemyPlayerIndex],
                IsMyLeader = IsPlayersLeader[myPlayerIndex],
                IsEnemyLeader = IsPlayersLeader[enemyPlayerIndex],
                MyLeader = PlayersLeader[myPlayerIndex][0].Status,
                EnemyLeader = PlayersLeader[enemyPlayerIndex][0].Status,
                EnemyName = Players[enemyPlayerIndex].PlayerName,
                MyName = Players[myPlayerIndex].PlayerName,
                MyDeckCount = PlayersDeck[myPlayerIndex].Count(),
                EnemyDeckCount = PlayersDeck[enemyPlayerIndex].Count(),
                MyHandCount = PlayersHandCard[myPlayerIndex].Count() + (IsPlayersLeader[myPlayerIndex] ? 1 : 0),
                EnemyHandCount = PlayersHandCard[enemyPlayerIndex].Count() + (IsPlayersLeader[enemyPlayerIndex] ? 1 : 0),
                MyCemeteryCount = PlayersCemetery[myPlayerIndex].Count(),
                EnemyCemeteryCount = PlayersCemetery[enemyPlayerIndex].Count(),
                MyHandCard = PlayersHandCard[myPlayerIndex].Select(x => x.Status),
                EnemyHandCard = PlayersHandCard[enemyPlayerIndex].Select(x => x.Status).Select(x => x.IsReveal ? x : new CardStatus() { IsCardBack = true, DeckFaction = PlayersFaction[enemyPlayerIndex] }),
                MyPlace = PlayersPlace[myPlayerIndex].Select(x => x.Select(c => c.Status)).ToArray(),
                EnemyPlace = PlayersPlace[enemyPlayerIndex].Select
                (
                    x => x.Select(c => c.Status).Select(item => item.Conceal ? new CardStatus() { IsCardBack = true, DeckFaction = PlayersFaction[enemyPlayerIndex] } : item)
                ).ToArray(),
                MyCemetery = PlayersCemetery[myPlayerIndex].Select(x => x.Status),
                EnemyCemetery = PlayersCemetery[enemyPlayerIndex].Select(x => x.Status),
            };
        }
        //--------------------------------------
        public Task ShowWeatherApply(int playerIndex, RowPosition row, RowStatus type)
        {
            return Task.WhenAll(Players[playerIndex].SendAsync(ServerOperationType.ShowWeatherApply, row, type),
                                Players[AnotherPlayer(playerIndex)].SendAsync(ServerOperationType.ShowWeatherApply, row.RowMirror(), type));
        }
        public Task SendCardOn(int playerIndex, CardLocation location)
        {
            if (!location.RowPosition.IsOnRow()) return Task.CompletedTask;
            return Players[playerIndex].SendAsync
            (
                ServerOperationType.CardOn,
                location
            );
        }
        public Task SendCardDown(int playerIndex, CardLocation location)
        {
            if (!location.RowPosition.IsOnRow()) return Task.CompletedTask;
            return Players[playerIndex].SendAsync
            (
                ServerOperationType.CardDown,
                location
            );
        }
        public Task SendCardMove(int playerIndex, MoveCardInfo info)
        {
            return Players[playerIndex].SendAsync
            (
                ServerOperationType.CardMove,
                info
            );
        }
        public Task SendSetCard(int playerIndex, GameCard card)//更新某个玩家的一个卡牌
        {
            //如果处于敌方场地
            var isBack = (card.Status.CardRow.IsOnPlace() && card.Status.Conceal);
            if (card.PlayerIndex != playerIndex) isBack = (card.Status.CardRow.IsInHand() && (!card.Status.IsReveal));
            return Players[playerIndex].SendAsync
                (
                    ServerOperationType.SetCard,
                    GetCardLocation(playerIndex, card),
                    isBack ?
                    new CardStatus()
                    {
                        IsCardBack = true,
                        DeckFaction = card.Status.DeckFaction
                    }
                    : card.Status
                );
        }
        //
        public async Task ShowCardMove(CardLocation location, GameCard card, bool refresh = true)
        {
            await SendCardMove(card.PlayerIndex, new MoveCardInfo()
            {
                Soure = GetCardLocation(card.PlayerIndex, card),
                Taget = location,
                Card = refresh ? card.Status : null
            });
            await SendCardMove(AnotherPlayer(card.PlayerIndex), new MoveCardInfo()
            {
                Soure = GetCardLocation(AnotherPlayer(card.PlayerIndex), card),
                Taget = new CardLocation() { RowPosition = location.RowPosition.RowMirror(), CardIndex = location.CardIndex },
                Card = refresh ? card.Status : null
            });
            var row = RowToList(card.PlayerIndex, card.Status.CardRow);
            var taget = RowToList(card.PlayerIndex, location.RowPosition);
            await LogicCardMove(row, row.IndexOf(card), taget, location.CardIndex);
            await SetCountInfo();
        }
        public async Task ShowSetCard(GameCard card)//更新敌我的一个卡牌
        {
            if (!card.Status.CardRow.IsOnRow()) return;
            await Task.WhenAll(SendSetCard(Player1Index, card), SendSetCard(Player2Index, card));
        }
        public async Task ShowCardDown(GameCard card)//落下(收到天气陷阱,或者其他卡牌)
        {
            if (!card.Status.CardRow.IsOnRow()) return;
            var task1 = Players[card.PlayerIndex].SendAsync(ServerOperationType.CardDown, GetCardLocation(card.PlayerIndex, card));
            var task2 = Players[AnotherPlayer(card.PlayerIndex)].SendAsync(ServerOperationType.CardDown,
                GetCardLocation(AnotherPlayer(card.PlayerIndex), card));
            await Task.WhenAll(task1, task2);
        }
        public async Task ShowCardOn(GameCard card)//落下(收到天气陷阱,或者其他卡牌)
        {
            if (!card.Status.CardRow.IsOnRow()) return;
            var task1 = Players[card.PlayerIndex].SendAsync(ServerOperationType.CardOn, GetCardLocation(card.PlayerIndex, card));
            var task2 = Players[AnotherPlayer(card.PlayerIndex)].SendAsync(ServerOperationType.CardOn,
                GetCardLocation(AnotherPlayer(card.PlayerIndex), card));
            await Task.WhenAll(task1, task2);
        }
        //
        public Task ShowCardNumberChange(GameCard card, int num, NumberType type = NumberType.Normal)
        {
            return Task.WhenAll
            (
                SendCardNumberChange(Player1Index, card, num, type),
                SendCardNumberChange(Player2Index, card, num, type)
            );
        }
        public Task SendCardNumberChange(int playerIndex, GameCard card, int num, NumberType type = NumberType.Normal)
        {
            if (card.IsShowBack(playerIndex))
                return Task.CompletedTask;
            return Players[playerIndex].SendAsync
                (
                    ServerOperationType.ShowCardNumberChange,
                    GetCardLocation(playerIndex, card),
                    num,
                    type
                );
        }
        //--
        public Task SendBullet(int playerIndex, GameCard source, GameCard taget, BulletType type)
        {
            if (source.IsShowBack(playerIndex) || taget.IsShowBack(playerIndex))
                return Task.CompletedTask;
            return Players[playerIndex].SendAsync
            (
                ServerOperationType.ShowBullet,
                GetCardLocation(playerIndex, source),
                GetCardLocation(playerIndex, taget),
                type
            );
        }
        public Task ShowBullet(GameCard source, GameCard taget, BulletType type)
        {
            return Task.WhenAll
            (
                SendBullet(Player1Index, source, taget, type),
                SendBullet(Player2Index, source, taget, type)
            );
        }
        //
        public Task SendCardIconEffect(int playerIndex, GameCard card, CardIconEffectType type)
        {
            return Players[playerIndex].SendAsync
            (
                ServerOperationType.ShowCardIconEffect,
                GetCardLocation(playerIndex, card),
                type
            );
        }
        public Task ShowCardIconEffect(GameCard card, CardIconEffectType type)
        {
            return Task.WhenAll
            (
                SendCardIconEffect(Player1Index, card, type),
                SendCardIconEffect(Player2Index, card, type)
            );
        }
        //
        public Task SendCardBreakEffect(int playerIndex, GameCard card, CardBreakEffectType type)
        {
            return Players[playerIndex].SendAsync
            (
                ServerOperationType.ShowCardBreakEffect,
                GetCardLocation(playerIndex, card),
                type
            );
        }
        public Task ShowCardBreakEffect(GameCard card, CardBreakEffectType type)
        {
            return Task.WhenAll
            (
                SendCardBreakEffect(Player1Index, card, type),
                SendCardBreakEffect(Player2Index, card, type)
            );
        }
        //----------------------------------------------------------------------------------------------
        public Task SendGameResult(TwoPlayer player)
        {
            var myPlayerIndex = (player == TwoPlayer.Player1 ? Player1Index : Player2Index);
            var enemyPlayerIndex = (player == TwoPlayer.Player1 ? Player2Index : Player1Index);
            //---
            int result = 0;//0为平, 1为玩家1胜利, 2为玩家2胜利
            if (PlayersWinCount[myPlayerIndex] == PlayersWinCount[enemyPlayerIndex])
                result = 0;
            if (PlayersWinCount[myPlayerIndex] > PlayersWinCount[enemyPlayerIndex])
                result = 1;
            if (PlayersWinCount[myPlayerIndex] < PlayersWinCount[enemyPlayerIndex])
                result = 2;
            //---
            return Players[myPlayerIndex].SendAsync(ServerOperationType.GameEnd, new GameResultInfomation
            (
                Players[myPlayerIndex].PlayerName,
                Players[enemyPlayerIndex].PlayerName,
                gameStatu:
                (
                    result == 0 ? GameStatus.Draw :
                    (result == 1 ? GameStatus.Win : GameStatus.Lose)
                ),
                RoundCount,
                PlayersRoundResult[0][myPlayerIndex],
                PlayersRoundResult[0][enemyPlayerIndex],
                PlayersRoundResult[1][myPlayerIndex],
                PlayersRoundResult[1][enemyPlayerIndex],
                PlayersRoundResult[2][myPlayerIndex],
                PlayersRoundResult[2][enemyPlayerIndex]
            ));
        }
        public void ToCemeteryInfo(GameCard card)
        {
            card.Status.Armor = 0; //护甲归零
            card.Status.HealthStatus = 0;//没有增益和受伤
            card.Status.IsCardBack = false; //没有背面
            card.Status.IsResilience = false;//没有坚韧
                                             //card.Status.IsGray = false;   //没有灰
            card.Status.IsShield = false; //没有昆恩
            card.Status.IsSpying = false; //没有间谍
            card.Status.Conceal = false;  //没有隐藏
            card.Status.IsReveal = false; //没有揭示
                                          //card.CardStatus.Location.RowPosition = RowPosition.MyCemetery;
        }
        public GwentServerGame(Player player1, Player player2)
        {
            //初始化游戏信息
            GameRound = new Random().Next(2) == 1 ? TwoPlayer.Player1 : TwoPlayer.Player2;
            //随机个先后手
            PlayersRoundResult[0] = new int[2];
            PlayersRoundResult[1] = new int[2];
            PlayersRoundResult[2] = new int[2];
            Players[Player1Index] = player1;
            Players[Player2Index] = player2;
            PlayersPlace[Player1Index] = new List<GameCard>[3];
            PlayersPlace[Player2Index] = new List<GameCard>[3];
            PlayersFaction[Player1Index] = GwentMap.CardMap[player1.Deck.Leader].Faction;
            PlayersFaction[Player2Index] = GwentMap.CardMap[player2.Deck.Leader].Faction;
            //----------------------------------------------------
            PlayersPlace[Player1Index][0] = new List<GameCard>();
            PlayersPlace[Player2Index][0] = new List<GameCard>();
            PlayersPlace[Player1Index][1] = new List<GameCard>();
            PlayersPlace[Player2Index][1] = new List<GameCard>();
            PlayersPlace[Player1Index][2] = new List<GameCard>();
            PlayersPlace[Player2Index][2] = new List<GameCard>();
            //---------------------------------------------------
            GameRowStatus[0] = new RowStatus[3] { RowStatus.None, RowStatus.None, RowStatus.None };//玩家天气
            GameRowStatus[1] = new RowStatus[3] { RowStatus.None, RowStatus.None, RowStatus.None };//玩家天气                                                                 //----------------------------------------------------
            PlayersCemetery[Player1Index] = new List<GameCard>();
            PlayersCemetery[Player2Index] = new List<GameCard>();
            PlayersHandCard[Player1Index] = new List<GameCard>();
            PlayersHandCard[Player2Index] = new List<GameCard>();
            PlayersStay[Player1Index] = new List<GameCard>();
            PlayersStay[Player2Index] = new List<GameCard>();
            IsPlayersLeader[Player1Index] = true;
            IsPlayersLeader[Player2Index] = true;
            PlayersLeader[Player1Index] = new List<GameCard>()
            {
                new GameCard()
                {
                    PlayerIndex = Player1Index,
                    Status = new CardStatus(player1.Deck.Leader)
                    {
                        DeckFaction = PlayersFaction[Player1Index],
                        CardRow = RowPosition.MyLeader,
                    }
                }.With(card => card.Effect = new CardEffect(this, card))
            }.ToList();
            PlayersLeader[Player2Index] = new List<GameCard>
            {
                new GameCard()
                {
                    PlayerIndex = Player2Index,
                    Status = new CardStatus(player2.Deck.Leader)
                    {
                        DeckFaction = PlayersFaction[Player2Index],
                        CardRow = RowPosition.MyLeader,
                    }
                }.With(card => card.Effect = new CardEffect(this, card))
            }.ToList();
            //将卡组转化成实体,并且打乱牌组
            PlayersDeck[Player1Index] = player1.Deck.Deck.Select(x => new GameCard()
            {
                PlayerIndex = Player1Index,
                Status = new CardStatus(x)
                {
                    DeckFaction = GwentMap.CardMap[player1.Deck.Leader].Faction,
                    CardRow = RowPosition.MyDeck
                }
            }.With(card => card.Effect = new CardEffect(this, card)))
            .Mess().ToList();
            //需要更改,将卡牌效果变成对应Id的卡牌效果
            PlayersDeck[Player2Index] = player2.Deck.Deck.Select(x => new GameCard()
            {
                PlayerIndex = Player2Index,
                Status = new CardStatus(x)
                {
                    DeckFaction = GwentMap.CardMap[player1.Deck.Leader].Faction,
                    CardRow = RowPosition.MyDeck
                }
            }.With(card => card.Effect = new CardEffect(this, card)))
            .Mess().ToList();
        }
        public async Task SendBigRoundEndToCemetery()
        {
            //#############################################
            //#                 需要优化                  
            //#############################################
            var player1CardsPart = new GameCardsPart();
            var player2CardsPart = new GameCardsPart();
            for (var i = PlayersPlace[Player1Index][0].Count - 1; i >= 0; i--)
            {
                var card = PlayersPlace[Player1Index][0][i];
                if (card.Status.IsResilience)
                {
                    card.Status.IsResilience = false;
                }
                else
                {
                    player1CardsPart.MyRow1Cards.Add(i);
                    player2CardsPart.EnemyRow1Cards.Add(i);
                    ToCemeteryInfo(card);
                    await LogicCardMove(PlayersPlace[Player1Index][0], i, PlayersCemetery[Player1Index], PlayersCemetery[Player1Index].Count);
                }
            }
            for (var i = PlayersPlace[Player1Index][1].Count - 1; i >= 0; i--)
            {
                var card = PlayersPlace[Player1Index][1][i];
                if (card.Status.IsResilience)
                {
                    card.Status.IsResilience = false;
                }
                else
                {
                    player1CardsPart.MyRow2Cards.Add(i);
                    player2CardsPart.EnemyRow2Cards.Add(i);
                    ToCemeteryInfo(card);
                    await LogicCardMove(PlayersPlace[Player1Index][1], i, PlayersCemetery[Player1Index], PlayersCemetery[Player1Index].Count);
                }
            }
            for (var i = PlayersPlace[Player1Index][2].Count - 1; i >= 0; i--)
            {
                var card = PlayersPlace[Player1Index][2][i];
                if (card.Status.IsResilience)
                {
                    card.Status.IsResilience = false;
                }
                else
                {
                    player1CardsPart.MyRow3Cards.Add(i);
                    player2CardsPart.EnemyRow3Cards.Add(i);
                    ToCemeteryInfo(card);
                    await LogicCardMove(PlayersPlace[Player1Index][2], i, PlayersCemetery[Player1Index], PlayersCemetery[Player1Index].Count);
                }
            }
            for (var i = PlayersPlace[Player2Index][0].Count - 1; i >= 0; i--)
            {
                var card = PlayersPlace[Player2Index][0][i];
                if (card.Status.IsResilience)
                {
                    card.Status.IsResilience = false;
                }
                else
                {
                    player2CardsPart.MyRow1Cards.Add(i);
                    player1CardsPart.EnemyRow1Cards.Add(i);
                    ToCemeteryInfo(card);
                    await LogicCardMove(PlayersPlace[Player2Index][0], i, PlayersCemetery[Player2Index], PlayersCemetery[Player2Index].Count);
                }
            }
            for (var i = PlayersPlace[Player2Index][1].Count - 1; i >= 0; i--)
            {
                var card = PlayersPlace[Player2Index][1][i];
                if (card.Status.IsResilience)
                {
                    card.Status.IsResilience = false;
                }
                else
                {
                    player2CardsPart.MyRow2Cards.Add(i);
                    player1CardsPart.EnemyRow2Cards.Add(i);
                    ToCemeteryInfo(card);
                    await LogicCardMove(PlayersPlace[Player2Index][1], i, PlayersCemetery[Player2Index], PlayersCemetery[Player2Index].Count);
                }
            }
            for (var i = PlayersPlace[Player2Index][2].Count - 1; i >= 0; i--)
            {
                var card = PlayersPlace[Player2Index][2][i];
                if (card.Status.IsResilience)
                {
                    card.Status.IsResilience = false;
                }
                else
                {
                    player2CardsPart.MyRow3Cards.Add(i);
                    player1CardsPart.EnemyRow3Cards.Add(i);
                    ToCemeteryInfo(card);
                    await LogicCardMove(PlayersPlace[Player2Index][2], i, PlayersCemetery[Player2Index], PlayersCemetery[Player2Index].Count);
                }
            }
            var player1Task = Players[Player1Index].SendAsync(ServerOperationType.CardsToCemetery, player1CardsPart);
            var player2Task = Players[Player2Index].SendAsync(ServerOperationType.CardsToCemetery, player2CardsPart);
            await Task.WhenAll(SetCountInfo(), SetPointInfo(), player1Task, player2Task);
        }
        public int TwoPlayerToPlayerIndex(TwoPlayer player)
        {
            return ((player == TwoPlayer.Player1) ? Player1Index : Player2Index);
        }
        public async Task ApplyWeather(int playerIndex, int row, RowStatus type)
        {
            if (row < 0 || row > 2) return;
            GameRowStatus[playerIndex][row] = type;
            await ShowWeatherApply(playerIndex, row.IndexToMyRow(), type);
            await OnWeatherApply(playerIndex, row, type);
        }
        //====================================================================================
        //====================================================================================
        //卡牌事件处理与转发
        public async Task OnWeatherApply(int playerIndex, int row, RowStatus type)//有天气降下
        {
            switch (type)
            {
                case RowStatus.BloodMoon:
                    foreach (var card in PlayersPlace[playerIndex][row])
                    {
                        await card.Effect.Damage(2);
                    }
                    break;
                case RowStatus.PitTrap:
                    foreach (var card in PlayersPlace[playerIndex][row])
                    {
                        await card.Effect.Damage(4);
                    }
                    break;
            }
            foreach (var card in GetAllCard(playerIndex))
            {
                await card.Effect.OnWeatherApply(playerIndex, row, type);
            }
        }
        public async Task OnUnitDown(GameCard taget)//单位卡落下时(二段部署前)
        {
            switch (GameRowStatus[taget.PlayerIndex][taget.Status.CardRow.MyRowToIndex()])
            {
                case RowStatus.BloodMoon:
                    await taget.Effect.Damage(2);
                    break;
                case RowStatus.PitTrap:
                    await taget.Effect.Damage(4);
                    break;
            }
            //有单位落下,如果这排有血月,坑陷...对自己造成伤害
            //-------------------------------------
            await taget.Effect.OnUnitDown(taget);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget)
                    await card.Effect.OnUnitDown(taget);
            }
        }
        public async Task OnTurnStart(int playerIndex)//谁的回合开始了
        {
            for (var i = 0; i < 3; i++)
            {
                var list = RowToList(playerIndex, i.IndexToMyRow());
                if (list.Count() == 0) continue;
                switch (GameRowStatus[playerIndex][i])
                {
                    //灾厄
                    case RowStatus.KorathiHeatwave://科拉兹热浪
                        await list.WhereAllLowest().Mess().First().Effect.Damage(2);
                        break;
                    case RowStatus.RaghNarRoog://终末之战
                        await list.WhereAllHighest().Mess().First().Effect.Damage(2);
                        break;
                    case RowStatus.SkelligeStorm://史凯利杰风暴
                        await list[0].Effect.Damage(2);
                        if (list.Count >= 2)
                            await list[1].Effect.Damage(1);
                        if (list.Count >= 3)
                            await list[2].Effect.Damage(1);
                        break;
                    case RowStatus.BitingFrost://冰霜
                        await list.WhereAllLowest().Mess().First().Effect.Damage(2);
                        break;
                    case RowStatus.ImpenetrableFog://浓雾
                        await list.WhereAllHighest().Mess().First().Effect.Damage(2);
                        break;
                    case RowStatus.TorrentialRain://雨
                        foreach (var card in list.Mess().Take(2))
                            await card.Effect.Damage(1);
                        break;
                    //恩泽 Boon
                    case RowStatus.GoldenFroth://黄金酒沫
                        foreach (var card in list.Mess().Take(2))
                            await card.Effect.Boost(1);
                        break;
                    case RowStatus.FullMoon://满月
                        await list.Mess().First().Effect.Boost(2);
                        //+++++++++++++++++++++++++++++++++++++
                        //等待补充（临时效果:增益随机一个单位2点
                        //+++++++++++++++++++++++++++++++++++++
                        break;
                    //无效果
                    case RowStatus.DragonDream://龙之梦
                    case RowStatus.PitTrap://坑陷
                    case RowStatus.BloodMoon://血月
                    case RowStatus.None:
                        break;
                }
            }
            foreach (var card in GetAllCard(playerIndex))
            {
                await card.Effect.OnTurnStart(playerIndex);
            }
        }
        public async Task OnTurnOver(int playerIndex)//谁的回合结束了
        {
            foreach (var card in GetAllCard(playerIndex))
            {
                await card.Effect.OnTurnOver(playerIndex);
            }
        }
        public async Task OnRoundOver(int RoundCount, int player1Point, int player2Point)//第几轮,谁赢了
        {
            foreach (var card in GetAllCard(player1Point > player2Point ? Player1Index : Player2Index))
            {
                await card.Effect.OnRoundOver(RoundCount, player1Point, player2Point);
            }
        }
        public async Task OnPlayerPass(int playerIndex)//玩家pass的时候触发
        {
            foreach (var card in GetAllCard(playerIndex))
            {
                await card.Effect.OnPlayerPass(playerIndex);
            }
        }
        public async Task OnPlayerDraw(int playerIndex, GameCard taget)//抽卡
        {
            await taget.Effect.OnPlayerDraw(playerIndex, taget);
            foreach (var card in GetAllCard(playerIndex))
            {
                if (card != taget)
                    await card.Effect.OnPlayerDraw(playerIndex, taget);
            }
        }
        public async Task OnCardReveal(GameCard taget, GameCard soure = null)//揭示
        {
            await taget.Effect.OnCardReveal(taget, soure);
            await soure?.Effect.OnCardReveal(taget, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardReveal(taget, soure);
            }
        }
        public async Task OnCardConsume(GameCard taget, GameCard soure)//吞噬
        {
            await taget.Effect.OnCardConsume(taget, soure);
            await soure.Effect.OnCardConsume(taget, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardConsume(taget, soure);
            }
        }
        public async Task OnCardBoost(GameCard taget, int num, GameCard soure = null)//增益
        {
            await taget.Effect.OnCardBoost(taget, num, soure);
            await soure?.Effect.OnCardBoost(taget, num, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardBoost(taget, num, soure);
            }
        }
        public async Task OnCardHurt(GameCard taget, int num, GameCard soure = null)//受伤
        {
            await taget.Effect.OnCardHurt(taget, num, soure);
            await soure?.Effect.OnCardHurt(taget, num, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardHurt(taget, num, soure);
            }
        }
        public async Task OnSpecialPlay(GameCard taget)//法术卡使用前
        {
            await taget.Effect.OnSpecialPlay(taget);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget)
                    await card.Effect.OnSpecialPlay(taget);
            }
        }
        public async Task OnUnitPlay(GameCard taget)//单位卡执行一段部署前
        {
            await taget.Effect.OnUnitPlay(taget);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget)
                    await card.Effect.OnUnitPlay(taget);
            }
        }
        public async Task OnCardDeath(GameCard taget)//有卡牌进入墓地
        {
            await taget.Effect.OnCardDeath(taget);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget)
                    await card.Effect.OnCardDeath(taget);
            }
        }
        public async Task OnCardSpyingChange(GameCard taget, bool isSpying, GameCard soure = null)//场上间谍改变
        {
            await taget.Effect.OnCardSpyingChange(taget, isSpying, soure);
            await soure?.Effect.OnCardSpyingChange(taget, isSpying, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardSpyingChange(taget, isSpying, soure);
            }
        }
        public async Task OnCardDiscard(GameCard taget, GameCard soure = null)//卡牌被丢弃
        {
            await taget.Effect.OnCardDiscard(taget, soure);
            await soure?.Effect.OnCardDiscard(taget, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardDiscard(taget, soure);
            }
        }
        public async Task OnCardAmbush(GameCard taget)//有伏击卡触发
        {
            await taget.Effect.OnCardAmbush(taget);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget)
                    await card.Effect.OnCardAmbush(taget);
            }
        }
        public async Task OnCardSwap(GameCard taget, GameCard soure = null)//卡牌交换
        {
            await taget.Effect.OnCardSwap(taget, soure);
            await soure?.Effect.OnCardSwap(taget, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardSwap(taget, soure);
            }
        }
        public async Task OnCardConceal(GameCard taget, GameCard soure = null)//隐匿
        {
            await taget.Effect.OnCardConceal(taget, soure);
            await soure?.Effect.OnCardConceal(taget, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardConceal(taget, soure);
            }
        }
        public async Task OnCardLockChange(GameCard taget, bool isLock, GameCard soure = null)//锁定状态改变
        {
            await taget.Effect.OnCardLockChange(taget, isLock, soure);
            await soure?.Effect.OnCardLockChange(taget, isLock, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardLockChange(taget, isLock, soure);
            }
        }
        public async Task OnCardAddArmor(GameCard taget, int num, GameCard soure = null)//增加护甲
        {
            await taget.Effect.OnCardAddArmor(taget, num, soure);
            await soure?.Effect.OnCardAddArmor(taget, num, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardAddArmor(taget, num, soure);
            }
        }
        public async Task OnCardSubArmor(GameCard taget, int num, GameCard soure = null)//降低护甲
        {
            await taget.Effect.OnCardSubArmor(taget, num, soure);
            await soure?.Effect.OnCardSubArmor(taget, num, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardSubArmor(taget, num, soure);
            }
        }
        public async Task OnCardArmorBreak(GameCard taget, GameCard soure = null)//护甲被破坏
        {
            await taget.Effect.OnCardResurrect(taget);
            await soure?.Effect.OnCardResurrect(taget);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardArmorBreak(taget, soure);
            }
        }
        public async Task OnCardResurrect(GameCard taget, GameCard soure = null)//有卡牌复活
        {
            await taget.Effect.OnCardResurrect(taget);
            await soure?.Effect.OnCardResurrect(taget, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardResurrect(taget);
            }
        }
        public async Task OnCardResilienceChange(GameCard taget, bool isResilience, GameCard soure = null)//坚韧状态改变
        {
            await taget.Effect.OnCardResilienceChange(taget, isResilience, soure);
            await soure?.Effect.OnCardResilienceChange(taget, isResilience, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardResilienceChange(taget, isResilience, soure);
            }
        }
        public async Task OnCardHeal(GameCard taget, GameCard soure = null)//卡牌被治愈
        {
            await taget.Effect.OnCardHeal(taget, soure);
            await soure?.Effect.OnCardHeal(taget, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardHeal(taget, soure);
            }
        }
        public async Task OnCardReset(GameCard taget, GameCard soure = null)//卡牌被重置
        {
            await taget.Effect.OnCardReset(taget, soure);
            await soure?.Effect.OnCardReset(taget, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardReset(taget, soure);
            }
        }
        public async Task OnCardStrengthen(GameCard taget, int num, GameCard soure = null)//强化
        {
            await taget.Effect.OnCardStrengthen(taget, num, soure);
            await soure?.Effect.OnCardStrengthen(taget, num, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardStrengthen(taget, num, soure);
            }
        }
        public async Task OnCardWeaken(GameCard taget, int num, GameCard soure = null)//削弱
        {
            await taget.Effect.OnCardWeaken(taget, num, soure);
            await soure?.Effect.OnCardWeaken(taget, num, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardWeaken(taget, num, soure);
            }
        }
        public async Task OnCardDrain(GameCard master, int num, GameCard taget)//有单位汲食时
        {
            await master.Effect.OnCardDrain(master, num, taget);
            await taget.Effect.OnCardDrain(master, num, taget);
            foreach (var card in GetAllCard(master.PlayerIndex))
            {
                if (card != master && card != taget)
                    await card.Effect.OnCardDrain(master, num, taget);
            }
        }
        public async Task OnCardCharm(GameCard taget, GameCard soure = null)//被魅惑
        {
            await taget.Effect.OnCardCharm(taget, soure);
            await soure?.Effect.OnCardCharm(taget, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardCharm(taget, soure);
            }
        }
        public async Task OnCardMove(GameCard taget, GameCard soure = null)//位移时
        {
            await taget.Effect.OnCardMove(taget, soure);
            await soure?.Effect.OnCardMove(taget, soure);
            foreach (var card in GetAllCard(taget.PlayerIndex))
            {
                if (card != taget && card != soure)
                    await card.Effect.OnCardMove(taget, soure);
            }
        }
    }
}