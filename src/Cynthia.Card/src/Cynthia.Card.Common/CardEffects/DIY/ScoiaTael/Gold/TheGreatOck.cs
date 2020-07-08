using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
using System;

namespace Cynthia.Card
{
    [CardEffectId("70032")]//Charli
    public class TheGreatOak : CardEffect, IHandlesEvent<AfterCardMove>
    {//若位于手牌、牌组或己方半场：有敌军单位被摧毁时获得1点增益。
        public TheGreatOak(GameCard card) : base(card) { }
        public async Task HandleEvent(AfterCardMove @event)
        {
            if (Game.GameRound.ToPlayerIndex(Game) == PlayerIndex 
                && (Card.Status.CardRow.IsOnPlace() || Card.Status.CardRow.IsInDeck() || Card.Status.CardRow.IsInHand()))
            {
                await Card.Effect.Boost(1, Card);
            }
            return;
        }

    }
}
