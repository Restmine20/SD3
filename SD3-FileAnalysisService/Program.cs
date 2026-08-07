using SD3_FileAnalysisService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace SD3_FileAnalysisService
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      builder.Services.AddHttpClient("GatewayClient", client =>
      {
        client.BaseAddress = new Uri("http://api-gateway:8080/");
      });
      builder.Services.AddHttpClient("WordCloudClient", client =>
      {
        client.BaseAddress = new Uri("https://quickchart.io/wordcloud");
      });

      builder.Services.AddDbContext<FileAnalysisDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

      builder.Services.AddControllers();
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();

      var app = builder.Build();


      using (var scope = app.Services.CreateScope())
      {
        var dbContext = scope.ServiceProvider.GetRequiredService<FileAnalysisDbContext>();
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
