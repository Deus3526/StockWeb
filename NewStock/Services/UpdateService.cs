using Microsoft.EntityFrameworkCore;
using NewStock.EFModels;
using NewStock.Finmind;

namespace NewStock.Services;

public class UpdateService
{
    private readonly FinmindApiClient _finmindApiClient;
    private readonly NewStockContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(
        FinmindApiClient finmindApiClient,
        NewStockContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UpdateService> logger)
    {
        _finmindApiClient = finmindApiClient;
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// 自 FinMind TaiwanStockInfo 更新至 StockInfo。
    /// <para>
    /// 字串欄位不加工、不補預設值；上游若缺值或無效，由 EF／資料庫約束反映錯誤。
    /// </para>
    /// </summary>
    public async Task<UpdateStockInfoResult> UpdateTaiwanStockInfoAsync()
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var rows = await _finmindApiClient.GetTaiwanStockInfoAsync(cancellationToken);
        var stockCount = rows.Count;

        var existing = await _db.StockInfos.ToDictionaryAsync(e => e.StockId, cancellationToken);

        var inserted = 0;
        var updated = 0;
        var unchanged = 0;

        foreach (var dto in rows)
        {
            if (existing.TryGetValue(dto.StockIdShort, out var entity))
            {
                if (entity.StockName == dto.StockName
                    && entity.IndustryCategory == dto.IndustryCategory
                    && entity.MarketType == dto.MarketTypeEnum)
                {
                    unchanged++;
                    continue;
                }

                entity.StockName = dto.StockName!;
                entity.IndustryCategory = dto.IndustryCategory!;
                entity.MarketType = dto.MarketTypeEnum;
                updated++;
            }
            else
            {
                _db.StockInfos.Add(new StockInfo
                {
                    StockId = dto.StockIdShort,
                    StockName = dto.StockName!,
                    IndustryCategory = dto.IndustryCategory!,
                    MarketType = dto.MarketTypeEnum,
                });
                inserted++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"TaiwanStockInfo 更新完成：StockCount={stockCount}, Inserted={inserted}, Updated={updated}, Unchanged={unchanged}");

        return new UpdateStockInfoResult(stockCount, inserted, updated, unchanged);
    }
}

public sealed record UpdateStockInfoResult(
    int StockCount,
    int Inserted,
    int Updated,
    int Unchanged);
