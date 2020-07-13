using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("70037")]//化妆舞会
    public class MasqueradeBall : CardEffect
    {//生成不重复的己方起始牌组铜色单位牌的轶亡原始同名牌到同排，将其基础战力设为2。
        public MasqueradeBall(GameCard card) : base(card) { }
        public override async Task<int> CardUseEffect()
        {
            var row = await Game.GetSelectRow(Card.PlayerIndex, Card, TurnType.My.GetRow());
            //var cardlist = Game.PlayerBaseDeck[PlayerIndex].Where(x => x.CardInfo().CardUseInfo == CardUseInfo.MyRow &&  x.Is(Group.Copper, CardType.Unit)).ToList();
            //var list = Game.PlayersDeck[Card.PlayerIndex].Where(x => x.CardInfo().CardUseInfo == CardUseInfo.MyRow && (x.Status.Group == Group.Silver || x.Status.Group == Group.Copper) && (x.CardInfo().CardType == CardType.Unit));
            var cardlist = Game.PlayerBaseDeck[PlayerIndex].Deck
                .Where(x => x.Group == Group.Copper)
                .ToList();

            if (Game.RowToList(PlayerIndex, row).Count < Game.RowMaxCount && cardlist.Count > 0)
            {//单排有位置，牌库有铜单位
                var cardsId = Game.PlayerBaseDeck[PlayerIndex].Deck
                .Distinct()
                .Where(x => x.Is(type: CardType.Unit, filter: x => x.IsAnyGroup(Group.Copper) && !x.HasAnyCategorie(Categorie.Agent)))
                .Mess(Game.RNG)
                .Take(Game.RowMaxCount - Game.RowToList(PlayerIndex, row).Count)
                .Select(x => x.CardId).ToArray();

                int num = 0;

                while ((Game.RowToList(PlayerIndex, row).Count < Game.RowMaxCount && cardsId.Length > num))
                {
                    await Game.CreateCardAtEnd(cardsId[num], PlayerIndex, row, setting: Lesser);
                    num++;
                }
            }
            return 0;
        }
        private void Lesser(CardStatus status)
        {
            status.IsDoomed = true;
            status.Strength = 2;
        }
    }
}