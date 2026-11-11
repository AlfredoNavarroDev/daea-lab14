using Microsoft.EntityFrameworkCore;
using TattooStudio.Datos.Data;
using TattooStudio.Datos.Repositorios;
using TattooStudio.Negocio;
using TattooStudio.Negocio.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<TattooStudioContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TattooStudioConnection")));

builder.Services.AddScoped<ITatuadorRepository, TatuadorRepository>();
builder.Services.AddScoped<ICitaTatuajeRepository, CitaTatuajeRepository>();
builder.Services.AddScoped<ITatuadorService, TatuadorService>();
builder.Services.AddScoped<ICitaTatuajeService, CitaTatuajeService>();

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "TattooStudio API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
