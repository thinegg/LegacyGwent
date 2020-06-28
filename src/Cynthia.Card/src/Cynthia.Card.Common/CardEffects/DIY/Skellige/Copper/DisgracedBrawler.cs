using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
using System;

namespace Cynthia.Card
{
    [CardEffectId("70028")]//不光彩的争斗者
    public class DisgracedBrawler : CardEffect
    {//部署：如果己方墓场中的卡牌数量大于牌组中的数量，则获得己方牌组数量的强化。

        public DisgracedBrawler(GameCard card) : base(card) { }

        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            int deckCount = Game.PlayersDeck[Card.PlayerIndex].Count();
            int cemeteryCount = Game.PlayersCemetery[PlayerIndex].Count();
            if (cemeteryCount > deckCount)
            {
                await Card.Effect.Strengthen(deckCount, Card);
            }
            return 0;
        }
    }
}
