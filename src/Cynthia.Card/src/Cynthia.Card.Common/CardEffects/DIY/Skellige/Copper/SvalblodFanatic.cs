using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
using System;

namespace Cynthia.Card
{
    [CardEffectId("70041")]//斯瓦勃洛狂信者
    public class SvalblodFanatic : CardEffect, IHandlesEvent<AfterTurnStart>
    {//回合开始时对自身造成2点伤害，然后随机对一个敌军单位造成2点伤害。
        public SvalblodFanatic(GameCard card) : base(card) { }
        public async Task HandleEvent(AfterTurnStart @event)
        {
            if (@event.PlayerIndex != Card.PlayerIndex || !Card.Status.CardRow.IsOnPlace())
            {
                return;
            }
            await Card.Effect.Damage(2, Card);
            var cards = Game.GetPlaceCards(AnotherPlayer);
            if (cards.Count() == 0)
            {
                return;
            }
            await cards.Mess(RNG).First().Effect.Damage(2, Card);
        }
    }
}
