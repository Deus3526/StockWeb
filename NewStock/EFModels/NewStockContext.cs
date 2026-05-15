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

    public virtual DbSet<StockDayInfo> StockDayInfos { get; set; }

    public virtual DbSet<TaiwanTradingDay> TaiwanTradingDays { get; set; }

    public virtual DbSet<周月K資料表> 周月K資料表s { get; set; }

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

        modelBuilder.Entity<StockDayInfo>(entity =>
        {
            entity.HasKey(e => new { e.StockId, e.Date });

            entity.Property(e => e.DataType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<StockDayInfoDataTypeEnum>(v));

            entity.HasOne(d => d.Stock)
                .WithMany(p => p.StockDayInfos)
                .HasForeignKey(d => d.StockId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaiwanTradingDay>(entity =>
        {
            entity.HasKey(e => e.Date);
            entity.Property(e => e.Date).ValueGeneratedNever();
        });

        modelBuilder.Entity<周月K資料表>(entity =>
        {
            entity.HasKey(e => new { e.StockId, e.Date, e.TimeType });

            entity.Property(e => e.TimeType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<StockKBarTimeTypeEnum>(v));

            entity.Property(e => e.DataType)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<StockDayInfoDataTypeEnum>(v));

            entity.HasOne(d => d.Stock)
                .WithMany()
                .HasForeignKey(d => d.StockId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
