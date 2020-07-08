using System.Linq;
using System.Threading.Tasks;
using Alsein.Extensions;
using System;

namespace Cynthia.Card
{
    [CardEffectId("70034")]//斯瓦勃洛图腾
    public class SvalblodTotem : CardEffect, IHandlesEvent<AfterTurnStart>, IHandlesEvent<AfterCardHurt>, IHandlesEvent<BeforeCardToCemetery>
    {//25回合后回合开始时，摧毁自身并生成1个“斯瓦勃洛勇士”。每当友军单位受到伤害时，减少1回合，每当友军单位被摧毁时，减少2回合。5点护甲。坚韧。
        public SvalblodTotem(GameCard card) : base(card) { }
        public override async Task<int> CardPlayEffect(bool isSpying, bool isReveal)
        {
            await Card.Effect.Armor(5, Card);
            await Card.Effect.Resilience(Card);
            //await Card.Effect.SetCountdown(value: 25);
            return 0;
        }

        public async Task HandleEvent(AfterTurnStart @event)
        {
            if (@event.PlayerIndex == Card.PlayerIndex && Card.Status.CardRow.IsOnPlace() && Card.Status.Countdown > 0)
            {
                await Card.Effect.SetCountdown(offset: -1);
            }
            if (Card.Effect.Countdown <= 0 && Card.Status.CardRow.IsOnPlace())
            {//触发效果
                var position = Card.GetLocation();
                await Card.Effect.ToCemetery();
                await Game.CreateCard(CardId.ChampionofSvalbold, PlayerIndex, position);
            }

        }
        public async Task HandleEvent(AfterCardHurt @event)
        {
            if (@event.Target.PlayerIndex != Card.PlayerIndex || !Card.Status.CardRow.IsOnPlace())
            {
                return;
            }
            await Card.Effect.SetCountdown(offset: -1);
            return;
        }
        public async Task HandleEvent(BeforeCardToCemetery @event)
        {
            if (!@event.isRoundEnd && @event.Target.PlayerIndex == Card.PlayerIndex && @event.Target.Status.Type == CardType.Unit
                && Card.Status.CardRow.IsOnPlace())
            {
                await Card.Effect.SetCountdown(offset: -2);
            }
            return;
        }


    }
}