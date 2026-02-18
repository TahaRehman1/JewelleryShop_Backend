using JeweleryAppBackend.Models;
using JeweleryAppBackend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
 
internal class Program
{
	private static async Task Main(string[] args)
	{
        Environment.SetEnvironmentVariable(
    "PLAYWRIGHT_BROWSERS_PATH",
    Path.Combine(Directory.GetCurrentDirectory(), "playwright")
);
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
		Logger logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
		builder.Services.AddLogging(delegate(ILoggingBuilder options)
		{
			options.AddConfiguration(builder.Configuration.GetSection("Logging")).AddNLog();
		});
		try
		{
            builder.Services.AddMemoryCache();
            builder.Services.AddControllers();
			builder.Services.AddTransient<UserService>();
			builder.Services.AddTransient<EmailService>();
			builder.Services.AddTransient<ProductService>();
            builder.Services.AddTransient<OrderService>();
            builder.Services.AddTransient<ImageService>();
			builder.Services.AddTransient<TaxService>();
            builder.Services.AddTransient<PlaywrightService>();
            builder.Services.AddTransient<InvoiceService>();
            builder.Services.AddDbContext<ApplicationDbContext>(delegate(DbContextOptionsBuilder options)
			{
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
			});
			builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
			byte[] key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
			builder.Services.AddAuthentication(delegate(AuthenticationOptions options)
			{
				options.DefaultAuthenticateScheme = "Bearer";
				options.DefaultChallengeScheme = "Bearer";
			}).AddJwtBearer(delegate(JwtBearerOptions options)
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false,
					ValidIssuer = builder.Configuration["Jwt:Issuer"],
					ValidAudience = builder.Configuration["Jwt:Audience"]
				};
			});
			builder.Services.AddCors(delegate(CorsOptions options)
			{
				options.AddPolicy("AllowAllOrigins", delegate(CorsPolicyBuilder corsPolicyBuilder)
				{
					corsPolicyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
				});
			});
			builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
			builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
			builder.Services.Configure<ShippingFeeSettings>(builder.Configuration.GetSection("ShippingFeeSettings"));
            builder.Services.Configure<InvoiceSettings>(builder.Configuration.GetSection("InvoiceSettings"));
            builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			WebApplication app = builder.Build(); 
            app.UseSwagger();
			app.UseSwaggerUI();
			app.UseCors("AllowAllOrigins");
			app.UseHttpsRedirection();
			app.UseAuthentication();
			app.UseAuthorization();
			app.MapControllers(); 
			app.UseStaticFiles();
            app.Run();
		}
		catch (Exception exception)
		{
			logger.Error(exception, "Stopped program because of exception");
			throw;
		}
		finally
		{
			LogManager.Shutdown();
		}
	}
}
