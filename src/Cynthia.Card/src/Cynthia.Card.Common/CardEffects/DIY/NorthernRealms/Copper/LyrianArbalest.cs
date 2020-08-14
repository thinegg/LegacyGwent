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
            if (@event.Target.PlayerIndex != Card.PlayerIndex || !Card.Status.CardRow.IsOnPlace())
            {
                return;
            }
            //如果左侧有单位且不是伏击卡
            var Ltaget = Card.GetRangeCard(1, GetRangeType.HollowLeft);
            var Rtaget = Card.GetRangeCard(1, GetRangeType.HollowRight);

            if (Ltaget.Count() != 0 && !Ltaget.Single().Status.Conceal && Ltaget.Single() == @event.Target)
            {
                await trigger();
            }
            //如果右侧有单位且不是伏击卡
            else if (Rtaget.Count() != 0 && !Rtaget.Single().Status.Conceal && Rtaget.Single() == @event.Target)
            {
                await trigger();
            }
            await Task.CompletedTask;
        }
        private async Task trigger()
        {
            var cards = Game.GetPlaceCards(AnotherPlayer);
            if (Card.Status.HealthStatus >= Card.Status.Strength / 2)
            {
                await cards.Mess(RNG).First().Effect.Damage(2, Card);
            }
            else
            {
                await cards.Mess(RNG).First().Effect.Damage(1, Card);
            }
            await Task.CompletedTask;
        }
    }
}