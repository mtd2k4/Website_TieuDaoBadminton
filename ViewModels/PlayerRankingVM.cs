using System.Collections.Generic;

namespace tieudaobadminton.ViewModels
{
    public class RacketHistoryItemVM
    {
        public string Period { get; set; } = "";
        public string RacketModel { get; set; } = "";
        public string Brand { get; set; } = "";
        public string HighlightNote { get; set; } = "";
        public int? ShopProductId { get; set; }
    }

    public class PlayerRankingVM
    {
        public int Rank { get; set; }
        public string RegionGroup { get; set; } = "Vietnam"; // "Vietnam" | "World" | "Legends"
        public string Category { get; set; } = "Đơn Nam (MS)";
        public string FullName { get; set; } = "";
        public string Nickname { get; set; } = "";
        public string Country { get; set; } = "";
        public string CountryBadge { get; set; } = "VN";
        public int BwfPoints { get; set; }
        public string BestWorldRank { get; set; } = "";
        public string Achievements { get; set; } = "";
        public string PlayStyle { get; set; } = "";
        public string CurrentRacket { get; set; } = "";
        public string MainBrand { get; set; } = "";
        public string StringAndTension { get; set; } = "";
        public string ShoeModel { get; set; } = "";
        public string ShopMatchKeyword { get; set; } = "";
        public int? ShopProductId { get; set; }
        public string? ShopProductName { get; set; }
        public decimal? ShopProductPrice { get; set; }
        public List<RacketHistoryItemVM> RacketHistory { get; set; } = new();
    }
}

