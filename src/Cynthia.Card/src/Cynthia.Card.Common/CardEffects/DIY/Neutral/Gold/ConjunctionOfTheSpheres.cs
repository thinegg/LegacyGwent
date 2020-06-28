using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
using System;
using System.Collections.Generic;

namespace Cynthia.Card
{
    [CardEffectId("70029")]//天球交汇
    public class ConjunctionOfTheSpheres : CardEffect
    {//复制你的当前牌库的所有牌的基础同名牌随机洗入到你的牌库中。

        public ConjunctionOfTheSpheres(GameCard card) : base(card) { }

        public override async Task<int> CardUseEffect()
        {
            var Deck = Game.PlayersDeck[PlayerIndex].ToList();

            // 随机洗入卡组
            await Shuffle(Deck);

            return 0;
        }

        public async Task Shuffle(List<GameCard> Deck)
        {
            foreach (var CardInDeck in Deck)
            {
                await Game.CreateCard(CardInDeck.Status.CardId, Card.PlayerIndex, new CardLocation(RowPosition.MyDeck, RNG.Next(0, Game.PlayersDeck[Card.PlayerIndex].Count)));
            }
        }
    }
}
