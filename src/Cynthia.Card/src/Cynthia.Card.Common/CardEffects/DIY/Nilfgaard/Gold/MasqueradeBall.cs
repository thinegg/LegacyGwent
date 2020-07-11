using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("70037")]//化妆舞会
    public class MasqueradeBall : CardEffect
    {//随机生成己方起始牌组中不重复的非间谍铜色单位牌的轶亡原始同名牌到同排，将其基础战力设为2，直至填满此排。
        public MasqueradeBall(GameCard card) : base(card) { }
        public override async Task<int> CardUseEffect()
        {
            var row = await Game.GetSelectRow(Card.PlayerIndex, Card, TurnType.My.GetRow());
            //var cardlist = Game.PlayerBaseDeck[PlayerIndex].Where(x => x.CardInfo().CardUseInfo == CardUseInfo.MyRow &&  x.Is(Group.Copper, CardType.Unit)).ToList();
            //var list = Game.PlayersDeck[Card.PlayerIndex].Where(x => x.CardInfo().CardUseInfo == CardUseInfo.MyRow && (x.Status.Group == Group.Silver || x.Status.Group == Group.Copper) && (x.CardInfo().CardType == CardType.Unit));
            var cardlist = Game.PlayerBaseDeck[PlayerIndex].Deck
                .Where(x => x.Group == Group.Copper)
                .GroupBy(x => x.CardId).ToList();

            if (Game.RowToList(PlayerIndex, row).Count < Game.RowMaxCount && cardlist.Count > 0)
            {//单排有位置，牌库有铜单位
                /*
                var cardsId = Game.PlayerBaseDeck[PlayerIndex].Deck
                .Distinct()
                .Where(x => x.CardInfo().CardUseInfo == CardUseInfo.MyRow && x.Is(Group.Copper, CardType.Unit))
                .Where(x => x.Is(type: CardType.Unit, filter: x => x.IsAnyGroup(Group.Copper) && !x.HasAnyCategorie(Categorie.Agent)))
                .Mess(Game.RNG)
                .Take(Game.RowMaxCount - Game.RowToList(PlayerIndex, row).Count)
                .Select(x => x.CardId).ToArray();
                
                var cardsId = Game.PlayerBaseDeck[PlayerIndex].Deck
                    .Distinct()
                    .Where(x => x.Group == Group.Copper)
                    .GroupBy(x => x.CardId).ToList();
                */
                var cardsId = Game.PlayerBaseDeck[PlayerIndex].Deck
                .Distinct()
                .Where(x => x.Is(type: CardType.Unit, filter: x => x.IsAnyGroup(Group.Copper, Group.Silver) && !x.HasAnyCategorie(Categorie.Agent)))
                .Mess(Game.RNG)
                .Take(Game.RowMaxCount - Game.RowToList(PlayerIndex, row).Count)
                .Select(x => x.CardId).ToArray();

                int num = 0;
                while (!(Game.RowToList(PlayerIndex, row).Count >= Game.RowMaxCount || cardsId.Length  > 0))
                {
                    
                    var position = Card.GetLocation();
                    await Game.CreateCard(cardsId[num], PlayerIndex, position, setting: ToDoomed);
                    
                    //await target.Effect.Transform(CardId.Draugir, Card, x => x.Status.Strength = 1);
                    // await target.Effect.Resurrect(new CardLocation(row, int.MaxValue), Card);
                    num++;
                }
            }
            return 0;
        }
        private void ToDoomed(CardStatus status)
        {
            status.IsDoomed = true;
        }
    }
}