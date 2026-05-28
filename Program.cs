using PalletSignalRHub.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configurar logging  
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Agregar servicios SignalR  
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Configurar CORS para permitir conexiones desde cualquier origen  
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configurar el pipeline de la aplicación  
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors();

// Mapear el hub en la ruta que espera la aplicación de escritorio  
app.MapHub<PalletHub>("/pallethub");

// Endpoints de salud  
app.MapGet("/", () => "Pallet SignalR Hub esta corriendo! 🚀");
app.MapGet("/health", () => new {
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0"
});

app.Run();