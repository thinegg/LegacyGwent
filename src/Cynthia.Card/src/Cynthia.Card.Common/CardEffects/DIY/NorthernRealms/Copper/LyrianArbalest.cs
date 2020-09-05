using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
using System;


namespace Cynthia.Card
{
    [CardEffectId("70040")]//莱里亚强弩手
    public class LyrianArbalest : CardEffect
    {//对1个敌军单位造成等同于同排单位数量的伤害。
        public LyrianArbalest(GameCard card) : base(card) { }
        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            //计算己方同排的单位数量
            var count = Game.RowToList(Card.PlayerIndex, Card.Status.CardRow).IgnoreConcealAndDead().Count();
            var selectList = await Game.GetSelectPlaceCards(Card, selectMode: SelectModeType.AllRow);
            if (!selectList.TrySingle(out var target))
            {
                return 0;
            }
            var row = (target.Status.CardRow.MyRowToIndex() + 1).IndexToMyRow();
            await target.Effect.Damage(count, Card);
            if (!row.IsOnPlace())
            {
                return 0;
            }
            await target.Effect.Move(new CardLocation(row, int.MaxValue), Card);
            return 0;
        }

        /*
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
        */
    }
}