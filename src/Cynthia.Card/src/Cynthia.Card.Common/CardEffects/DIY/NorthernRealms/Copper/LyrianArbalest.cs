using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
using System;


namespace Cynthia.Card
{
    [CardEffectId("70040")]//莱里亚强弩手
    public class LyrianArbalest : CardEffect, IHandlesEvent<AfterCardBoost>
    {//每当相邻单位获得增益，对随机敌军单位造成1点伤害；自身增益大于或等于基础战力一半时，改为造成2点伤害。
        public LyrianArbalest(GameCard card) : base(card) { }
        
        public async Task HandleEvent(AfterCardBoost @event)
        {
            CardLocation myLoc = Card.GetLocation();
            CardLocation boostLoc = @event.Target.GetLocation();
            if (boostLoc.RowPosition == myLoc.RowPosition && Math.Abs(boostLoc.CardIndex - myLoc.CardIndex) <= 1 && Math.Abs(boostLoc.CardIndex - myLoc.CardIndex) != 0)
            {
                var cards = Game.GetPlaceCards(AnotherPlayer);
                if (cards.Count() == 0)
                {
                    return;
                }
                if(Card.Status.HealthStatus >= Card.Status.Strength / 2)
                {
                    await cards.Mess(RNG).First().Effect.Damage(2, Card);
                }
                else
                {
                    await cards.Mess(RNG).First().Effect.Damage(1, Card);
                }
            }
            await Task.CompletedTask;
        }
    }
}