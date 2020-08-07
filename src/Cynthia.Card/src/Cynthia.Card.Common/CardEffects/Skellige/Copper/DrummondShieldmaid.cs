using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("64026")]//德拉蒙家族持盾女卫
    public class DrummondShieldmaid : CardEffect
    {//对一个已受伤的敌军单位造成2点伤害，从卡组打出1张自身同名牌
        public DrummondShieldmaid(GameCard card) : base(card) { }
        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            var selectList = await Game.GetSelectPlaceCards(Card, filter: x => x.Status.HealthStatus < 0, selectMode: SelectModeType.AllRow);
            //若无目标
            if (!selectList.TrySingle(out var target))
            {
                return 0;
            }
            await target.Effect.Damage(2, Card);
            if (!Game.PlayersDeck[PlayerIndex].Where(x => x.Status.CardId == Card.Status.CardId).TryMessOne(out var card, Game.RNG))
            {
                return 0;
            }
            await card.MoveToCardStayFirst();
            return 1;
        }
    }
}