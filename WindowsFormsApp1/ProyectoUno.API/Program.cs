var builder = WebApplication.CreateBuilder(args);

// 1. Agregar servicios de Controllers y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. Habilitar Swagger en el pipeline HTTP (asegúrate de que esté fuera de 'if (app.Environment.IsDevelopment())' o dentro si estás en Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();