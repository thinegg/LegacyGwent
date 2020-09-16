using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
using System;


namespace Cynthia.Card
{
    [CardEffectId("70052")]//滚油 BoilingOil
    public class BoilingOil2 : CardEffect
    {//对1个敌军单位造成7点伤害。若目标被摧毁，对其相邻单位造成溢出的伤害。
        public BoilingOil2(GameCard card) : base(card) { }
        public override async Task<int> CardUseEffect()
		{
			var result = await Game.GetSelectPlaceCards(Card);
			if(result.Count<=0) 
            {
                return 0;
            }
            var card = result.Single();
            var org_point = card.CardPoint();
            await card.Effect.Damage(9,Card);
            if(org_point < 9)
            {
                //如果左侧有单位且不是伏击卡
                var Ltaget = card.GetRangeCard(1, GetRangeType.HollowLeft);
                var Rtaget = card.GetRangeCard(1, GetRangeType.HollowRight);

                if (Ltaget.Count() != 0 && !Ltaget.Single().Status.Conceal)
                {
                    await Ltaget.Single().Effect.Damage(7-org_point,Card);
                }
                //如果右侧有单位且不是伏击卡
                if (Rtaget.Count() != 0 && !Rtaget.Single().Status.Conceal)
                {
                    await Rtaget.Single().Effect.Damage(7-org_point,Card);
                }
            }
			return 0;
		}
    }
}