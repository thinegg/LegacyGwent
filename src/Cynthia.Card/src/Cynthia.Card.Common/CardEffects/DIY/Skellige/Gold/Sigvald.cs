using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("70038")]//西格瓦尔德
    public class Sigvald : CardEffect, IHandlesEvent<AfterTurnOver>
    {//回合结束时，复活至随机排，获得1点强化。
        public Sigvald(GameCard card) : base(card) { }
        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            await Task.CompletedTask;
            return 0;
        }
        public async Task HandleEvent(AfterTurnOver @event)
        {
            if (@event.PlayerIndex != Card.PlayerIndex || !Card.Status.CardRow.IsInCemetery())
            {
                return;
            }
            await Card.Effect.Strengthen(1, Card);
            //复活到最右侧
            await Card.Effect.Resurrect(new CardLocation() { RowPosition = Game.GetRandomCanPlayLocation(Card.PlayerIndex, true).RowPosition, CardIndex = int.MaxValue }, Card);
            return;
        }
    }
}