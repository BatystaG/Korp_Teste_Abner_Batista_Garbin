using Microsoft.EntityFrameworkCore;
using EstoqueService.Data;

var builder = WebApplication.CreateBuilder(args);

// Registra o DbContext usando a connection string do appsettings.json
builder.Services.AddDbContext<EstoqueDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

// Permite que o Angular (rodando em outra porta) consuma esta API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Aplica migrations automaticamente ao subir o container
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();
    db.Database.Migrate();
}

app.MapOpenApi();
app.UseCors();
app.MapControllers();

app.Run();
