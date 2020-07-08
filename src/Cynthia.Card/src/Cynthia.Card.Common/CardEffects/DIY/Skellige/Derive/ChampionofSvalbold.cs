using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("70035")] //斯瓦勃洛勇士
    public class ChampionofSvalbold : CardEffect, IHandlesEvent<AfterTurnOver>
    {
        //回合结束时，摧毁对方半场1个随机最弱单位，一共可生效3次。
        public ChampionofSvalbold(GameCard card) : base(card){}
        public async Task HandleEvent(AfterTurnOver @event)
        {
            if (Card.Status.CardRow.IsOnPlace() && @event.PlayerIndex == PlayerIndex && Game.GetPlaceCards(AnotherPlayer).WhereAllLowest().Count() > 0 && Card.Status.Countdown > 0)
            {
                await Game.GetPlaceCards(AnotherPlayer).WhereAllLowest().Mess(RNG).First().Effect.ToCemetery(CardBreakEffectType.Scorch);
                await Card.Effect.SetCountdown(offset: -1);
            }
            return;
        }
       
    }
}