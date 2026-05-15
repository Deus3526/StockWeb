namespace NewStock.Finmind;

/// <summary>
/// FinMind 若干 dataset 共用的 <c>stock_id</c> 欄位契約。
/// </summary>
public interface IBaseStockResponse
{
    string? StockId { get; set; }
}