using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("44016")]//亚甸槌击者
    public class AedirnianMauler : CardEffect
    {//对一个敌军单位造成 3点伤害，若目标未被摧毁，则与其对决。
        public AedirnianMauler(GameCard card) : base(card) { }
        private const int damagePoint = 3;
        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            var selectList = await Game.GetSelectPlaceCards(Card, selectMode: SelectModeType.EnemyRow);

            if (!selectList.TrySingle(out var target))
            {
                return 0;
            }
            await target.Effect.Damage(damagePoint, Card);

            if (target.IsAliveOnPlance())
            {
                //对决，target先受到伤害
                await Duel(target, Card);
            }
            return 0;
        }
    }
}