using Microsoft.EntityFrameworkCore;
using NewStock.Models.Enum;

namespace NewStock.EFModels;

public class NewStockContext : DbContext
{
    public NewStockContext(DbContextOptions<NewStockContext> options)
        : base(options)
    {
    }

    public virtual DbSet<StockInfo> StockInfos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockInfo>(entity =>
        {
            entity.Property(e => e.StockId).ValueGeneratedNever();

            entity.Property(e => e.MarketType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<MarketTypeEnum>(v));
        });
    }
}
