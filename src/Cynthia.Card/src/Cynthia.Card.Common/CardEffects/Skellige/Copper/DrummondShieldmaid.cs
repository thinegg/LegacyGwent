using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("64026")]//德拉蒙家族持盾女卫
    public class DrummondShieldmaid : CardEffect
    {//对一个单位造成1点伤害，若其受伤，从卡组打出1张自身同名牌
        public DrummondShieldmaid(GameCard card) : base(card) { }
        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            var selectList = await Game.GetSelectPlaceCards(Card, selectMode: SelectModeType.AllRow);
            if (!selectList.TrySingle(out var target))
            {
                return 0;
            }
            await target.Effect.Damage(1, Card);
            //如果目标没受伤，结束
            if (target.Status.HealthStatus < 0 && Game.PlayersDeck[PlayerIndex].Any(t => t.Status.CardId == Card.Status.CardId))
            {
                if (!Game.PlayersDeck[PlayerIndex].Where(x => x.Status.CardId == Card.Status.CardId).TryMessOne(out var card, Game.RNG))
                {
                    return 0;
                }
                await card.MoveToCardStayFirst();
                return 1;
            }
            return 0;
        }
    }
}