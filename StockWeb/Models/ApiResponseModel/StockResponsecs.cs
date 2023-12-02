using StockWeb.DbModels;
using StockWeb.Enums;
using System.Collections.Concurrent;

namespace StockWeb.Models.ApiResponseModel
{
    public class 上市股票基本資訊
    {
        public string? 出表日期 { get; set; }
        public int? 公司代號 { get; set; }
        public string? 公司名稱 { get; set; }
        public string? 公司簡稱 { get; set; }
        public string? 外國企業註冊地國 { get; set; }
        public string? 產業別 { get; set; }
        public string? 住址 { get; set; }
        public string? 營利事業統一編號 { get; set; }
        public string? 董事長 { get; set; }
        public string? 總經理 { get; set; }
        public string? 發言人 { get; set; }
        public string? 發言人職稱 { get; set; }
        public string? 代理發言人 { get; set; }
        public string? 總機電話 { get; set; }
        public string? 成立日期 { get; set; }
        public string? 上市日期 { get; set; }
        public string? 普通股每股面額 { get; set; }
        public string? 實收資本額 { get; set; }
        public string? 私募股數 { get; set; }
        public string? 特別股 { get; set; }
        public string? 編制財務報表類型 { get; set; }
        public string? 股票過戶機構 { get; set; }
        public string? 過戶電話 { get; set; }
        public string? 過戶地址 { get; set; }
        public string? 簽證會計師事務所 { get; set; }
        public string? 簽證會計師1 { get; set; }
        public string? 簽證會計師2 { get; set; }
        public string? 英文簡稱 { get; set; }
        public string? 英文通訊地址 { get; set; }
        public string? 傳真機號碼 { get; set; }
        public string? 電子郵件信箱 { get; set; }
        public string? 網址 { get; set; }
        public decimal? 已發行普通股數或TDR原股發行股數 { get; set; }
    }

    public class 上櫃股票基本資訊_發行股數回傳結果
    {
        public string? reportDate { get; set; }
        public int iTotalRecords { get; set; }
        public object[]? note { get; set; }
        public string[][]? aaData { get; set; }

        public void 讀入上櫃股票基本資訊_流通股數(ConcurrentBag<StockBaseInfo> bags)
        {
            ArgumentNullException.ThrowIfNull(aaData);
            foreach (string[] s in aaData)
            {
                if (int.TryParse(s[1], out int stockId) == false) continue;
                StockBaseInfo? baseInfo = bags.FirstOrDefault(b => b.StockId == stockId);
                if (baseInfo == null)
                {
                    baseInfo = new StockBaseInfo();
                    baseInfo.StockId = stockId;
                    baseInfo.StockType = StockTypeEnum.otc;
                    bags.Add(baseInfo);
                }
                baseInfo.StockName = s[2].Trim();
                decimal 發行股數 = decimal.Parse(s[3]);
                int 發行張數 = (int)(發行股數 / 1000);
                baseInfo.StockAmount = 發行張數;
            }


            return;
        }
    }


}
