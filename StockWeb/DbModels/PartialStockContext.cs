using Microsoft.EntityFrameworkCore;
using StockWeb.Enums;

namespace StockWeb.DbModels
{
    public partial class StockContext
    {
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockBaseInfo>(entity =>
            {
                entity.Property(e => e.StockType)
                    .HasConversion(e=>e.ToString(),e=>Enum.Parse<StockTypeEnum>(e))
                    .HasMaxLength(3)
                    .IsUnicode(false);
            });
            modelBuilder.Entity<StockDayInfo>(entity =>
            {
                entity.Property(e => e.DataType)
                    .HasConversion(e => e.ToString(), e => Enum.Parse<StockDayInfoDataTypeEnum>(e))
                    .HasMaxLength(20)
                    .IsUnicode(true);
            });
        }
    }
}
