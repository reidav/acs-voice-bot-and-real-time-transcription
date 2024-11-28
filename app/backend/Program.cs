var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddBackendServices();

var app = builder.Build();

app.UseCors(option =>
{
    option.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});

app.MapControllers();
app.UseWebSockets();
app.Run();