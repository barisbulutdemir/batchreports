using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace takip.TempModels;

public partial class ProductionContext : DbContext
{
    public ProductionContext(DbContextOptions<ProductionContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
