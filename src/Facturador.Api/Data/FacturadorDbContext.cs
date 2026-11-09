using Microsoft.EntityFrameworkCore;

namespace Facturador.Api.Data;

public sealed class FacturadorDbContext(DbContextOptions<FacturadorDbContext> options) : DbContext(options)
{
    public DbSet<DocumentoTipo> Documentos => Set<DocumentoTipo>();
    public DbSet<DocVenta> DocVentas => Set<DocVenta>();
    public DbSet<DocVentaLinea> DocVentaLineas => Set<DocVentaLinea>();
    public DbSet<DocDevVenta> DocDevVentas => Set<DocDevVenta>();
    public DbSet<DocDevVentaLinea> DocDevVentaLineas => Set<DocDevVentaLinea>();
    public DbSet<Agenda> Agendas => Set<Agenda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentoTipo>(entity => { entity.ToTable("Documentos"); entity.HasKey(x => x.IdDocumento); entity.Property(x => x.IdDocumento).HasColumnName("ID_Documento"); entity.Property(x => x.Nombre).HasMaxLength(200); });
        modelBuilder.Entity<DocVenta>(entity => { entity.ToTable("Doc_Venta"); entity.HasKey(x => x.Op); entity.Property(x => x.IdDocumento).HasColumnName("ID_Documento"); entity.Property(x => x.IdAgenda).HasColumnName("ID_Agenda"); entity.Property(x => x.NdOp).HasColumnName("ND_Op"); });
        modelBuilder.Entity<DocVentaLinea>(entity => { entity.ToTable("Doc_VentaLinea"); entity.HasKey(x => x.IdAmarre); entity.Property(x => x.IdAmarre).HasColumnName("ID_Amarre"); entity.Property(x => x.IdCentroCosto).HasColumnName("ID_Centro_Costo"); });
        modelBuilder.Entity<DocDevVenta>(entity => { entity.ToTable("Doc_DevVenta"); entity.HasKey(x => x.Op); entity.Property(x => x.IdDocumento).HasColumnName("ID_Documento"); entity.Property(x => x.IdAgenda).HasColumnName("ID_Agenda"); });
        modelBuilder.Entity<DocDevVentaLinea>(entity => { entity.ToTable("Doc_DevVentaLinea"); entity.HasKey(x => x.IdAmarre); entity.Property(x => x.IdAmarre).HasColumnName("ID_Amarre"); entity.Property(x => x.IdCentroCosto).HasColumnName("ID_Centro_Costo"); });
        modelBuilder.Entity<Agenda>(entity => { entity.ToTable("Agenda"); entity.HasKey(x => x.IdAgenda); entity.Property(x => x.IdAgenda).HasColumnName("ID_Agenda"); entity.Property(x => x.AgendaNombre).HasColumnName("AgendaNombre"); });
    }
}

public sealed class DocumentoTipo { public decimal IdDocumento { get; set; } public string Nombre { get; set; } = string.Empty; }
public sealed class DocVenta { public decimal Op { get; set; } public DateTime Fecha { get; set; } public decimal Estado { get; set; } public decimal IdDocumento { get; set; } public string? Serie { get; set; } public decimal? Numero { get; set; } public string? IdAgenda { get; set; } public decimal SubTotal { get; set; } public decimal Impuestos { get; set; } public decimal? Percepcion { get; set; } public decimal Total { get; set; } public string? Observaciones { get; set; } public decimal? NdOp { get; set; } }
public sealed class DocVentaLinea { public decimal IdAmarre { get; set; } public decimal Op { get; set; } public decimal? IdCentroCosto { get; set; } }
public sealed class DocDevVenta { public decimal Op { get; set; } public DateTime Fecha { get; set; } public decimal Estado { get; set; } public decimal IdDocumento { get; set; } public string? Serie { get; set; } public decimal? Numero { get; set; } public string? IdAgenda { get; set; } public decimal SubTotal { get; set; } public decimal Impuestos { get; set; } public decimal Total { get; set; } public string? Observaciones { get; set; } public decimal? OpDocVta { get; set; } }
public sealed class DocDevVentaLinea { public decimal IdAmarre { get; set; } public decimal Op { get; set; } public decimal? IdCentroCosto { get; set; } }
public sealed class Agenda { public string IdAgenda { get; set; } = string.Empty; public string? AgendaNombre { get; set; } }
