using Caro.Hubs;
using Caro.Interfaces;
using Caro.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR service
builder.Services.AddSignalR();

// Dependency Injection
builder.Services.AddSingleton<IRoomManager, RoomManager>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowVue",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Thay đổi URL này thành URL của ứng dụng Vue.js 
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Cho phep gửi cookie và thông tin xác thực
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

app.UseAuthorization();

// Sử dụng CORS
app.UseCors("AllowVue");

app.MapControllers();

//Map SignalR hub
app.MapHub<GameHub>("/gameHub");

app.Run();
