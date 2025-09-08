using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using StockWeb.DbModels;
using StockWeb.Enums;
using StockWeb.Extensions;
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
        public string[][]? Data { get; set; }

        public void 轉換為上櫃股票基本資訊_流通股數(ConcurrentDictionary<int, StockBaseInfo> concurrentBaseInfos)
        {
            ArgumentNullException.ThrowIfNull(Data);
            foreach (string[] s in Data)
            {
                if (int.TryParse(s[1], out int stockId) == false) continue;
                StockBaseInfo? baseInfo = concurrentBaseInfos.GetValueOrDefault(stockId);
                if (baseInfo == null)
                {
                    baseInfo = new StockBaseInfo();
                    baseInfo.StockId = stockId;
                    baseInfo.StockType = StockTypeEnum.otc;
                    concurrentBaseInfos[stockId] = baseInfo;
                }
                baseInfo.StockName = s[2].Trim();
                decimal 發行股數 = decimal.Parse(s[3]);
                int 發行張數 = (int)(發行股數 / 1000);
                baseInfo.StockAmount = 發行張數;
            }


            return;
        }
    }

    public class 上市股票盤後基本資訊回傳結果
    {
        public required Table[] tables { get; set; }
        public Params? _params { get; set; }
        public string? stat { get; set; }
        public string? date { get; set; }

        public class Params
        {
            public string? date { get; set; }
            public string? type { get; set; }
            public string? controller { get; set; }
            public string? action { get; set; }
            public string? lang { get; set; }
        }

        public class Table
        {
            public string? title { get; set; }
            public string[]? fields { get; set; }
            public string[][]? data { get; set; }
        }
    }


    public class 上市股票盤後當沖資訊回傳結果
    {
        public string? stat { get; set; }
        public string? date { get; set; }
        public required Table[] tables { get; set; }
        public string? selectType { get; set; }
        public class Table
        {
            public string? title { get; set; }
            public string[]? fields { get; set; }
            public required string[][] data { get; set; }
            public string[]? notes { get; set; }
            public string? hints { get; set; }
            public int? total { get; set; }
        }
    }


    public class 上市股票盤後融資融券資訊回傳結果
    {
        public string? stat { get; set; }
        public string? date { get; set; }
        public required Table[] tables { get; set; }
        public class Table
        {
            public string? title { get; set; }
            public string[]? fields { get; set; }
            public required string[][] data { get; set; }
            public string[]? notes { get; set; }
        }
    }

    public class 上市股票盤後借券資訊回傳結果
    {
        public string? stat { get; set; }
        public string? date { get; set; }
        public string? title { get; set; }
        public string? hints { get; set; }
        public string[]? fields { get; set; }
        public required string[][] data { get; set; }
        public string[]? notes { get; set; }
        public int? total { get; set; }
    }


    public class 上市股票盤後外資資訊回傳結果
    {
        public string? stat { get; set; }
        public string? date { get; set; }
        public string? title { get; set; }
        public string[]? fields { get; set; }
        public required string[][] data { get; set; }
        public string[]? notes { get; set; }
        public string? hints { get; set; }
        public int total { get; set; }
    }


    public class 上市股票盤後投信資訊回傳結果
    {
        public string? stat { get; set; }
        public string? date { get; set; }
        public string? title { get; set; }
        public string[]? fields { get; set; }
        public required string[][] data { get; set; }
        public string[]? notes { get; set; }
        public string? hints { get; set; }
        public int? total { get; set; }
    }

    public class 上櫃股票回傳結果Base
    {
        public string? date { get; set; }
        public string? stat { get; set; }
        public required TablesModel[] tables { get; set; }  // 改為陣列

        public class TablesModel
        {
            public string? title { get; set; }
            public string? subtitle { get; set; }
            public string? date { get; set; }
            public int totalCount { get; set; }
            public string[]? fields { get; set; }
            public required string[][] data { get; set; }
            public string[][]? summary { get; set; }
            public string[]? notes { get; set; }
        }
    }

    public class 上櫃股票盤後基本資訊回傳結果 : 上櫃股票回傳結果Base
    {

    }


    public class 上櫃股票盤後當沖資訊回傳結果 : 上櫃股票回傳結果Base
    {
    }


    public class 上櫃股票盤後融資融券資訊回傳結果 : 上櫃股票回傳結果Base
    {

    }


    public class 上櫃股票盤後借券資訊回傳結果 : 上櫃股票回傳結果Base
    {

    }

    public class 上櫃股票盤後外資淨買超資訊回傳結果 : 上櫃股票回傳結果Base
    {

    }

    public class 上櫃股票盤後外資淨賣超資訊回傳結果 : 上櫃股票回傳結果Base
    {

    }


    public class 上櫃股票盤後投信淨買超資訊回傳結果 : 上櫃股票回傳結果Base
    {

    }

    public class 上櫃股票盤後投信淨賣超資訊回傳結果 : 上櫃股票回傳結果Base
    {

    }



    public class 上市大盤成交資訊回傳結果
    {
        public string? stat { get; set; }
        public string? date { get; set; }
        public string? title { get; set; }
        public string? hints { get; set; }
        public string[]? fields { get; set; }
        public required string[][] data { get; set; }
        public string[]? notes { get; set; }

        public List<MarketDayInfo> ToMarketDayInfo()
        {
            List<MarketDayInfo> result = new List<MarketDayInfo>();
            foreach (var s in data)
            {
                MarketDayInfo marketDayInfo = new MarketDayInfo
                {
                    Date = s[0].ToDateOnly(),
                    成交張數 = Convert.ToInt32(decimal.Parse(s[1]) / 1000),
                    成交金額 = Convert.ToInt64(decimal.Parse(s[2])),
                    成交筆數 = Convert.ToInt32(decimal.Parse(s[3])),
                    大盤指數 = Convert.ToDouble(decimal.Parse(s[4])),
                    漲跌 = Convert.ToDouble(decimal.Parse(s[5])),
                };
                result.Add(marketDayInfo);
            }
            return result;
        }
    }

    public class 上市股票殖利率回傳結果
    {
        public string? stat { get; set; }
        public string? title { get; set; }
        public string[]? fields { get; set; }
        public required string[][] data { get; set; }
        public object[]? extraNotes { get; set; }
        public string[]? notes { get; set; }
        public string[]? formula { get; set; }
        public string? strDate { get; set; }
        public string? endDate { get; set; }
    }

    // MIS 即時資料 (https://mis.twse.com.tw/stock/api/getStockInfo.jsp?json=1&delay=0&ex_ch=tse_1101.tw|otc_6488.tw)
    public class Mis即時股價回傳結果
    {
        public List<Mis即時股價項目>? msgArray { get; set; }
        public int? userDelay { get; set; }
        public string? rtcode { get; set; }
        public string? rtmessage { get; set; }
        public MisQueryTime? queryTime { get; set; }
        public string? referer { get; set; }
        public int? cachedAlive { get; set; }
    }

    public class MisQueryTime
    {
        public string? sysDate { get; set; }
        public string? sysTime { get; set; }
        public string? sessionFrom { get; set; }
        public string? sessionLatest { get; set; }
        public string? sessionTo { get; set; }
        public string? sessionStr { get; set; }
    }

    public class Mis即時股價項目
    {
        // 常見欄位: c(代碼) n(名稱) z(當盤成交價) o(開) h(高) l(低) y(昨收) v(累積成交量), tv(當盤成交量)
        public string? c { get; set; }
        public string? n { get; set; }
        public string? z { get; set; }
        public string? o { get; set; }
        public string? h { get; set; }
        public string? l { get; set; }
        public string? y { get; set; }
        public string? v { get; set; }
        public string? tv { get; set; }
        public string? ex { get; set; }
        public string? tlong { get; set; }
        public string? d { get; set; }
        public string? b { get; set; }
        public string? pz { get; set; }
    }
    public class 月營收資訊
    {
        [Name("資料年月")]
        public string MonthString { get; set; }
        [Name("公司代號")]
        public string StockId { get; set; }
        [Name("營業收入-上月比較增減(%)")]
        [TypeConverter(typeof(Double0IfEmptyConverter))]
        public double MOM月增率 { get; set; }
        [Name("營業收入-去年同月增減(%)")]
        [TypeConverter(typeof(Double0IfEmptyConverter))]
        public double YoY年增率 { get; set; }
        [Name("累計營業收入-前期比較增減(%)")]
        [TypeConverter(typeof(Double0IfEmptyConverter))]
        public double 累計Yoy { get; set; }
        public class Double0IfEmptyConverter : DoubleConverter
        {
            public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return 0.0;

                text = text.Trim();
                if (double.TryParse(text, out double result))
                    return result;

                return 0.0;
            }
        }
    }

}
