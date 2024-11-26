using Microsoft.EntityFrameworkCore;
using SistemaBancario.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Registrar AppDbContext y configurar la conexi�n a la base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // Se usa la cadena de conexi�n definida en appsettings.json

builder.Services.AddControllers();
// Configurar Swagger para la documentaci�n de la API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
