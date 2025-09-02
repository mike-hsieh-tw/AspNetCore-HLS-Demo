using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

// 添加 CORS 服務
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles(); // 將 index.html 設為預設檔案
app.UseStaticFiles(); // 啟用 wwwroot 中的靜態檔案服務

// 啟用 streams 目錄的靜態檔案服務
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "streams")),
    RequestPath = "/streams",
    ServeUnknownFileTypes = true
});

// 使用 CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// 確保必要的目錄存在
var streamsDir = Path.Combine(Directory.GetCurrentDirectory(), "streams");
if (!Directory.Exists(streamsDir))
{
    Directory.CreateDirectory(streamsDir);
}

var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}

app.Run();