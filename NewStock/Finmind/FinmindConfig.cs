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
}