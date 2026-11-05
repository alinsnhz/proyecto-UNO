var builder = WebApplication.CreateBuilder(args);

// La API escucha en todas las interfaces de red
// API escucha en el puerto 5198
builder.WebHost.UseUrls("http://0.0.0.0:5198");
// 0.0.0.0 es la dirección que escucha la API, no la del servidor
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // permite probar los endopints durante el desarrollo
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
// publica los endpoints de los controladores
app.MapControllers();

app.Run();