
namespace SD3_APIGateway
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);


      builder.Services.AddHttpClient("FileStoringService", client =>
      {
        client.BaseAddress = new Uri("http://file-storing:8080/");
      });

      builder.Services.AddHttpClient("FileAnalysisService", client =>
      {
        client.BaseAddress = new Uri("http://file-analysis:8080/");
      });


      builder.Services.AddControllers();
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();

      var app = builder.Build();

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
