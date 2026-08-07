using SD3_FileStoringService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace SD3_FileStoringService
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      builder.Services.AddDbContext<FileStorageDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


      builder.Services.AddControllers();
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();

      var app = builder.Build();


      using (var scope = app.Services.CreateScope())
      {
        var dbContext = scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();
        dbContext.Database.Migrate();
      }

      if (app.Environment.IsDevelopment())
      {
        app.UseSwagger();
        app.UseSwaggerUI();
      }

      app.UseHttpsRedirection();

      app.UseAuthorization();


      app.MapControllers();

      app.Run();
    }
  }
}
