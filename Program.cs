using Microsoft.EntityFrameworkCore;
using SistemaBancario.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Registrar AppDbContext y configurar la conexión a la base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // Se usa la cadena de conexión definida en appsettings.json

builder.Services.AddControllers();
// Configurar Swagger para la documentación de la API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{

    options.AddPolicy("AllowAll",

        policy =>
        {

            policy.AllowAnyOrigin()

                  .AllowAnyHeader()

                  .AllowAnyMethod();

        });

});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// Habilita CORS antes de Authorization
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.Run();
