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
    public DbSet<ItemCodigo> ItemCodigos => Set<ItemCodigo>();
    public DbSet<CentroCosto> CentrosCosto => Set<CentroCosto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentoTipo>(entity => { entity.ToTable("Documentos"); entity.HasKey(x => x.IdDocumento); entity.Property(x => x.IdDocumento).HasColumnName("ID_Documento"); entity.Property(x => x.Nombre).HasMaxLength(200); });
        modelBuilder.Entity<DocVenta>(entity => { entity.ToTable("Doc_Venta"); entity.HasKey(x => x.Op); entity.Property(x => x.IdDocumento).HasColumnName("ID_Documento"); entity.Property(x => x.IdAgenda).HasColumnName("ID_Agenda"); entity.Property(x => x.NdOp).HasColumnName("ND_Op"); });
        modelBuilder.Entity<DocVentaLinea>(entity => { entity.ToTable("Doc_VentaLinea"); entity.HasKey(x => x.IdAmarre); entity.Property(x => x.IdAmarre).HasColumnName("ID_Amarre"); entity.Property(x => x.IdCentroCosto).HasColumnName("ID_Centro_Costo"); entity.Property(x => x.IdItem).HasColumnName("Id_item"); });
        modelBuilder.Entity<DocDevVenta>(entity => { entity.ToTable("Doc_DevVenta"); entity.HasKey(x => x.Op); entity.Property(x => x.IdDocumento).HasColumnName("ID_Documento"); entity.Property(x => x.IdAgenda).HasColumnName("ID_Agenda"); });
        modelBuilder.Entity<DocDevVentaLinea>(entity => { entity.ToTable("Doc_DevVentaLinea"); entity.HasKey(x => x.IdAmarre); entity.Property(x => x.IdAmarre).HasColumnName("ID_Amarre"); entity.Property(x => x.IdCentroCosto).HasColumnName("ID_Centro_Costo"); entity.Property(x => x.IdItem).HasColumnName("Id_item"); });
        modelBuilder.Entity<Agenda>(entity => { entity.ToTable("Agenda"); entity.HasKey(x => x.IdAgenda); entity.Property(x => x.IdAgenda).HasColumnName("ID_Agenda"); entity.Property(x => x.AgendaNombre).HasColumnName("AgendaNombre"); });
        modelBuilder.Entity<ItemCodigo>(entity => { entity.ToTable("ItemCodigo"); entity.HasKey(x => x.IdItem); entity.Property(x => x.IdItem).HasColumnName("ID_Item"); });
        modelBuilder.Entity<CentroCosto>(entity => { entity.ToTable("CCostos"); entity.HasKey(x => x.IdCentroCosto); entity.Property(x => x.IdCentroCosto).HasColumnName("ID_CCosto"); });
    }
}

public sealed class DocumentoTipo { public decimal IdDocumento { get; set; } public string Nombre { get; set; } = string.Empty; public string? DocSunat { get; set; } }
public sealed class DocVenta { public decimal Op { get; set; } public DateTime Fecha { get; set; } public decimal Estado { get; set; } public decimal IdDocumento { get; set; } public string? Serie { get; set; } public decimal? Numero { get; set; } public string? IdAgenda { get; set; } public decimal SubTotal { get; set; } public decimal Impuestos { get; set; } public decimal? Percepcion { get; set; } public decimal Total { get; set; } public string? Observaciones { get; set; } public decimal? NdOp { get; set; } }
public sealed class DocVentaLinea { public decimal IdAmarre { get; set; } public decimal Op { get; set; } public string? IdItem { get; set; } public decimal? Cantidad { get; set; } public decimal? Precio { get; set; } public decimal? Importe { get; set; } public decimal? IdCentroCosto { get; set; } }
public sealed class DocDevVenta { public decimal Op { get; set; } public DateTime Fecha { get; set; } public decimal Estado { get; set; } public decimal IdDocumento { get; set; } public string? Serie { get; set; } public decimal? Numero { get; set; } public string? IdAgenda { get; set; } public decimal SubTotal { get; set; } public decimal Impuestos { get; set; } public decimal Total { get; set; } public string? Observaciones { get; set; } public decimal? OpDocVta { get; set; } }
public sealed class DocDevVentaLinea { public decimal IdAmarre { get; set; } public decimal Op { get; set; } public string? IdItem { get; set; } public decimal? Cantidad { get; set; } public decimal? Precio { get; set; } public decimal? Importe { get; set; } public decimal? IdCentroCosto { get; set; } }
public sealed class Agenda { public string IdAgenda { get; set; } = string.Empty; public string? AgendaNombre { get; set; } }
public sealed class ItemCodigo { public string IdItem { get; set; } = string.Empty; public string? Descripcion { get; set; } }
public sealed class CentroCosto { public string IdCentroCosto { get; set; } = string.Empty; public string? Nombre { get; set; } }
