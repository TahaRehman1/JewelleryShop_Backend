using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
	public DbSet<ProductModel> Products { get; set; }

	public DbSet<OrderModel> Orders { get; set; }

	public DbSet<ProductImagesModel> ProductImages { get; set; }

	public DbSet<OrderProductsModel> OrderProducts { get; set; }

	public DbSet<DiscountModel> Discounts { get; set; }

	public DbSet<CustomerModel> Customers { get; set; }

	public DbSet<CategoryModel> Categories { get; set; }

	public DbSet<BannerModel> Banners { get; set; }

	public DbSet<NewsletterSubscriptionModel> NewsletterSubscriptions { get; set; }

	public DbSet<SpecificationsModel> Specifications { get; set; }

    public DbSet<InvoiceModel> Invoices { get; set; }

    public DbSet<ProductSpecificationModel> ProductSpecifications { get; set; }

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base((DbContextOptions)options)
	{
	}
}
