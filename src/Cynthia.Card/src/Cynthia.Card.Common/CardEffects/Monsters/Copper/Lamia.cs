using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("24024")]//女蛇妖
    public class Lamia : CardEffect
    {//对1个敌军单位造成4点伤害，场上每有1个“血月”灾厄效果，伤害提高2点。
        public Lamia(GameCard card) : base(card) { }
        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            if (!(await Game.GetSelectPlaceCards(Card, selectMode: SelectModeType.EnemyRow)).TrySingle(out var target))
            {
                return 0;
            }
            //int point = Game.GameRowEffect[AnotherPlayer][target.Status.CardRow.MyRowToIndex()].RowStatus == RowStatus.BloodMoon ? 7 : 4;
            var count = Game.GameRowEffect.SelectMany(x => x.Select(x => x.RowStatus)).Where(x => x == RowStatus.BloodMoon).Count();
            await target.Effect.Damage(4 + 2 * count, Card);
            return 0;
        }
    }
}