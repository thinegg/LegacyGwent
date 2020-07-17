using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;

namespace Cynthia.Card
{
    [CardEffectId("70039")]//奎特家族突袭者
    public class AnCraiteStriker : CardEffect, IHandlesEvent<AfterCardDiscard>
    {//对1个敌军随机单位造成3点伤害。被丢弃时对1个敌军随机单位造成3点伤害，并将自身2张同名牌加入牌组底部。
        public AnCraiteStriker(GameCard card) : base(card) { }

        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            await DamageRandomEnemy();
            return 0;
        }

        public async Task HandleEvent(AfterCardDiscard @event)
        {
            //进入墓地的不是本卡，什么都不发生
            if (@event.Target != Card)
            {
                return;
            }

            //await Card.Effect.Resurrect(Game.GetRandomCanPlayLocation(Card.PlayerIndex, true), Card);
            await DamageRandomEnemy();
            for (var i = 0; i < 2; i++)
            {
                await Game.CreateCardAtEnd(Card.Status.CardId, PlayerIndex, RowPosition.MyDeck);
            }
        }

        private async Task DamageRandomEnemy()
        {
            var cards = Game.GetPlaceCards(AnotherPlayer);
            if (cards.Count() == 0) return;
            await cards.Mess(Game.RNG).First().Effect.Damage(3, Card);
        }
    }
}