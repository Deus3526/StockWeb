using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace NewStock.EFModels;

public partial class NewStockContext : DbContext
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
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
