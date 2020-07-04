using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
using System;

namespace Cynthia.Card
{
    [CardEffectId("70031")]//沃米尔
    public class Voymir : CardEffect
    {//择一：选择1个单位将其增益转换为护甲；选择2个单位将其护甲转换为增益

        public Voymir(GameCard card) : base(card) { }

        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            //选择选项,设置每个选项的名字和效果
            var switchCard = await Card.GetMenuSwitch
            (
                ("增益转换为护甲", "选择1个单位将其增益转换为护甲"),
                ("护甲转换为增益", "选择2个单位将其护甲转换为增益")
            );

            if (switchCard == 0)
            {
                var selectList = await Game.GetSelectPlaceCards(Card, selectMode: SelectModeType.AllRow);
                if (!selectList.TrySingle(out var target))
                {
                    return 0;
                }

                int boostedPoint = target.Status.HealthStatus;
                if (boostedPoint > 0)
                {
                    await target.Effect.Reset(Card);
                    await target.Effect.Armor(boostedPoint, Card);
                }
                return 0;
            }
            else if (switchCard == 1)
            {
                var cards = await Game.GetSelectPlaceCards(Card, 2, selectMode: SelectModeType.AllRow);
                if (cards.Count <= 0) return 0;
                foreach (var target in cards)
                {
                    int damagenum = target.Status.Armor;
                    await target.Effect.Damage(damagenum, Card);
                    await target.Effect.Boost(damagenum, Card);
                }
            }

            return 0;
        }
    }
}
