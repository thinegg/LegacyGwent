namespace Cynthia.Card
{
    public class CardStatus
    {
        public CardStatus(string cardIndex)
        {
            CardId = cardIndex;
            Strength = GwentMap.CardMap[cardIndex].Strength;
            Type = GwentMap.CardMap[cardIndex].CardType;
            IsDoomed = GwentMap.CardMap[cardIndex].IsDoomed;
            IsCountdown = GwentMap.CardMap[cardIndex].IsCountdown;
            Countdown = GwentMap.CardMap[cardIndex].Countdown;
            CardArtsId = GwentMap.CardMap[cardIndex].CardArtsId;
            Group = GwentMap.CardMap[cardIndex].Group;
            Faction = GwentMap.CardMap[cardIndex].Faction;
        }
        public GwentCard CardInfo{get=>GwentMap.CardMap[CardId];}
        public CardStatus()
        {
            IsCardBack = true;
            Conceal = true;
        }
        //卡牌基本信息索引
        public string CardId { get; set; }
        public string CardArtsId { get; set; }
        public Group Group { get; set; }
        //public GwentCard CardInfo { get; set; }
        public RowPosition CardRow { get; set; }
        public bool IsDoomed { get; set; } = false;//是否佚亡
        public CardType Type { get; set; }//法术还是单位
        //---------------------------------------------------------------
        //卡牌状态
        public bool IsReveal { get; set; } = false;//是否被揭示 | 手牌
        public bool IsShield { get; set; } = false;//是否昆恩 | 手牌,场地
        public bool IsSpying { get; set; } = false;//是否间谍 | 场地
        public bool IsResilience { get; set; } = false;//是否坚韧 | 场地
        public int Armor { get; set; } = 0;//护甲 | 场地
        public int Strength { get; set; }//战力 | 手牌,场地,墓地
        public int HealthStatus { get; set; } = 0;//增益减益 | 手牌,场地
        public bool IsLock { get; set; } = false;//是否锁定 | 场地,墓地
        public bool Conceal { get; set; } = false;//是否盖牌 | 场地
        public bool IsImmue { get; set; } = false;//是否免疫 | 场地
        //-----------------------------------------------------------------
        //显示相关
        //public bool IsGray { get; set; } = false;
        public bool IsCardBack { get; set; } = false;
        public Faction DeckFaction { get; set; } = Faction.Monsters;
        public Faction Faction{get;set;}
        //-----------------------------------------------------------------
        public int Countdown = 0;
        public bool IsCountdown = false;
    }
}