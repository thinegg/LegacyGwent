using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
namespace Cynthia.Card
{
    [CardEffectId("70005")]//科沃的维索戈塔
    public class VysogotaOfCorvo : CardEffect, IHandlesEvent<AfterCardDeath>
    {//部署：治愈一个银/铜友军单位并使其获得坚韧。遗愿:治愈我方所有单位。
        public VysogotaOfCorvo(GameCard card) : base(card) { }

        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            var selectList = await Game.GetSelectPlaceCards(Card, selectMode: SelectModeType.MyRow, filter: card => card.IsAnyGroup(Group.Copper, Group.Silver));
            if (!selectList.TrySingle(out var target)) { return 0; }
            await target.Effect.Heal(Card);
            await target.Effect.Resilience(Card);
            // target.Status.IsImmue = true;
            return 0;
        }
        public async Task HandleEvent(AfterCardDeath @event)
        {
            if (@event.Target == Card)
            {
                var cards = Game.GetPlaceCards(Card.PlayerIndex)
                        .Where(x => x.Status.CardRow.IsOnPlace() && x.PlayerIndex == Card.PlayerIndex).ToList();
                foreach (var card in cards)
                {
                    // card.Status.IsImmue = false;
                    await card.Effect.Heal(Card);
                }
            }

            await Task.CompletedTask;
            return;
        }
    }
}