using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
using System;

namespace Cynthia.Card
{
    [CardEffectId("70032")]//Gascon
    public class Gascon : CardEffect, IHandlesEvent<AfterCardMove>
    {//若位于手牌、牌组或己方半场：己方回合中，有单位被改变所在排别时获得1点增益。
        public Gascon(GameCard card) : base(card) { }
        public async Task HandleEvent(AfterCardMove @event)
        {
            if (Game.GameRound.ToPlayerIndex(Game) == PlayerIndex 
                && (Card.Status.CardRow.IsOnPlace() || Card.Status.CardRow.IsInDeck() || Card.Status.CardRow.IsInHand())
                && @event.Target != @event.Source)
            {
                await Card.Effect.Boost(1, Card);
            }
            return;
        }

    }
}
