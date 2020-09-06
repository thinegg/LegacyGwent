using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("70046")]//帝国占卜师 ImperialDiviner

    public class ImperialDiviner : CardEffect
    {//对方每有一张被揭示的牌，获得2点增益。若被揭示，则双方每有一张被揭示的牌，获得2点增益。
        public ImperialDiviner(GameCard card) : base(card) { }
        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            //已被揭示
            if (isReveal)
            {
                var cards = Game.PlayersHandCard[PlayerIndex].Concat(Game.PlayersHandCard[AnotherPlayer]).Where(x => x.Status.IsReveal && x.Status.Type == CardType.Unit).ToList();
                await Card.Effect.Boost(cards.Count*2, Card);
            }
            //未被揭示
            else
            {
                var cards = Game.PlayersHandCard[AnotherPlayer].Where(x => x.Status.IsReveal && x.Status.Type == CardType.Unit).ToList();
                await Card.Effect.Boost(cards.Count*2, Card);
            }
            return 0;
        }
    }
}
