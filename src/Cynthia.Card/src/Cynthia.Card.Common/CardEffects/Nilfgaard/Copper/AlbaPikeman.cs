using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("34024")]//阿尔巴师枪兵  
    public class AlbaPikeman : CardEffect, IHandlesEvent<AfterTurnOver>
    {//回合结束时从卡组召唤1张同名牌。
        public AlbaPikeman(GameCard card) : base(card) { }
        public async Task HandleEvent(AfterTurnOver @event)
        {
            if (!(Card.Status.CardRow.IsOnPlace() && PlayerIndex == @event.PlayerIndex))
                return;
            if (Game.PlayersDeck[PlayerIndex].Any(t => t.Status.CardId == Card.Status.CardId))
            {
                if (!Game.PlayersDeck[PlayerIndex].Where(x => x.Status.CardId == Card.Status.CardId).TryMessOne(out var card, Game.RNG))
                {
                    return;
                }
                await card.Effect.Summon(Card.GetLocation() + 1, Card);
            }
            return;
        }
    }
}