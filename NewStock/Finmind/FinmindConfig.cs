namespace NewStock.Finmind;

public sealed class FinmindConfig
{
    /// <summary>
    /// appsettings.json 區段鍵名（與類別名稱無須相同）。
    /// </summary>
    public const string SectionName = "Finmind";

    /// <summary>
    /// FinMind v4 API 基底網址（勿結尾斜線），例如 https://api.finmindtrade.com/api/v4。
    /// </summary>
    public required string Domain { get; set; }

    /// <summary>
    /// 選填。<c>Bearer</c> Token（例如呼叫需認證的 TaiwanStockPrice 全市場）。
    /// 建議設於環境變數 <c>Finmind__Token</c>（雙底線對應此區段），不必寫入 appsettings。
    /// </summary>
    public string? Token { get; set; }
}