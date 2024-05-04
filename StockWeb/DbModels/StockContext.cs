using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace StockWeb.DbModels;

public partial class StockContext : DbContext
{
    public StockContext()
    {
    }

    public StockContext(DbContextOptions<StockContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MarketDayInfo> MarketDayInfos { get; set; }

    public virtual DbSet<StockBaseInfo> StockBaseInfos { get; set; }

    public virtual DbSet<StockDayInfo> StockDayInfos { get; set; }

    public virtual DbSet<User> Users { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Data Source=.,1434;User ID=sa;Password=deus.ko3526;Initial Catalog=Stock;TrustServerCertificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketDayInfo>(entity =>
        {
            entity.HasKey(e => e.Date);

            entity.ToTable("MarketDayInfo");
        });

        modelBuilder.Entity<StockBaseInfo>(entity =>
        {
            entity.HasKey(e => e.StockId);

            entity.ToTable("StockBaseInfo");

            entity.Property(e => e.StockId)
                .ValueGeneratedNever()
                .HasColumnName("StockID");
            entity.Property(e => e.Category).HasMaxLength(20);
            entity.Property(e => e.StockName).HasMaxLength(15);
            entity.Property(e => e.StockType)
                .HasMaxLength(3)
                .IsUnicode(false);
        });

        modelBuilder.Entity<StockDayInfo>(entity =>
        {
            entity.HasKey(e => new { e.StockId, e.Date });

            entity.ToTable("StockDayInfo");

            entity.HasOne(d => d.Stock).WithMany(p => p.StockDayInfos)
                .HasForeignKey(d => d.StockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockDayInfo_StockBaseInfo");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Account)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.UserName).HasMaxLength(15);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
