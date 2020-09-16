using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("70055")]//疯狂的冲锋 MadCharge
    public class MadCharge : CardEffect
    {//使一个友军单位与一个敌军单位对决。
        public MadCharge(GameCard card) : base(card) { }
        public override async Task<int> CardUseEffect()
        {
            var cards = await Game.GetSelectPlaceCards(Card, 1, selectMode: SelectModeType.MyRow);
            if (!cards.TrySingle(out var friend))
            {
                return 0;
            }

            var list = await Game.GetSelectPlaceCards(Card, selectMode: SelectModeType.EnemyRow);
            if (!list.TrySingle(out var enemy))
            {
                return 0;
            }

            //对决，前一个先受到伤害
            await friend.Effect.Duel(enemy, Card);
            return 0;
        }
    }
}