using Microsoft.EntityFrameworkCore;
using EventScheduler.Data;
using EventScheduler.Services;
using EventScheduler.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Enable CORS for Grafana frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


// Register database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<ITimingEventService, TimingEventService>();
builder.Services.AddSingleton<ISignalProcessingEngine, SignalProcessingEngine>();
builder.Services.AddHostedService<OpcUaClientService>();
builder.Services.AddSingleton<IInfluxDbService, InfluxDbService>();

var app = builder.Build();

// Auto-create database tables and seed everything
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // 1. Ensure Classifications exist
    var mechDelay = db.Classifications.FirstOrDefault(c => c.Name == "Mechanical Delay");
    if (mechDelay == null)
    {
        mechDelay = new EventScheduler.Models.Classification { Name = "Mechanical Delay" };
        db.Classifications.Add(mechDelay);
    }

    var setupDelay = db.Classifications.FirstOrDefault(c => c.Name == "Setup Delay");
    if (setupDelay == null)
    {
        setupDelay = new EventScheduler.Models.Classification { Name = "Setup Delay" };
        db.Classifications.Add(setupDelay);
    }
    db.SaveChanges(); // save to get IDs

    // 2. Ensure Signal Configs exist and are mapped perfectly
    if (!db.SignalConfigs.Any(c => c.SignalId == "Signal.A"))
    {
        db.SignalConfigs.Add(new EventScheduler.Models.SignalConfig { SignalId = "Signal.A", SignalType = "StartTrigger", Description = "Pump Sensor", ClassificationId = mechDelay.Id });
    }
    if (!db.SignalConfigs.Any(c => c.SignalId == "Signal.B"))
    {
        db.SignalConfigs.Add(new EventScheduler.Models.SignalConfig { SignalId = "Signal.B", SignalType = "StartTrigger", Description = "Setup Sensor", ClassificationId = setupDelay.Id });
    }
    if (!db.SignalConfigs.Any(c => c.SignalId == "Signal.C"))
    {
        db.SignalConfigs.Add(new EventScheduler.Models.SignalConfig { SignalId = "Signal.C", SignalType = "EndTrigger", Description = "Operation Complete", ClassificationId = null });
    }
    db.SaveChanges();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();
