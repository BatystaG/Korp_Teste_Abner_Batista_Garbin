using Microsoft.EntityFrameworkCore;
using FaturamentoService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FaturamentoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Evita erro de referência circular ao serializar NotaFiscal → Itens → NotaFiscal
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Registra o HttpClient nomeado que o NotasController usa para chamar o EstoqueService
builder.Services.AddHttpClient("EstoqueService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["EstoqueServiceUrl"]!);
});

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FaturamentoDbContext>();
    db.Database.Migrate();
}

app.MapOpenApi();
app.UseCors();
app.MapControllers();

app.Run();
