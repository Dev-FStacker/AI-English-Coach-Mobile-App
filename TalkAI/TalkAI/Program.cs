
using Microsoft.EntityFrameworkCore;
using DAL.Data;
using BLL.Interfaces;
using BLL.Services;
using BLL.Interfaces;
using BLL.Interface;
using DAL.Entities;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;

namespace TalkAI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Thêm cấu hình CORS nếu cần
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

            // Cấu hình kích thước file tối đa cho upload
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
            });

         

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton<ICacheService, CacheService>();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSingleton<ICacheService,DistributedCacheService>();
         
            builder.Services.AddScoped<IAzureSpeechService, AzureSpeechService>();
            builder.Services.AddScoped<IAzureLanguageService, AzureLanguageService>();
            builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();
            builder.Services.AddScoped<IAudioRecorderService, AudioRecorderService>();
            builder.Services.AddScoped<ITranslationService, AzureTranslationService>();
            builder.Services.AddSignalR();
            builder.Services.AddDbContext<TalkAIContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.Configure<AzureLanguageSettings>(
    builder.Configuration.GetSection("AzureLanguage"));
            builder.Services.AddMemoryCache();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                            .SetIsOriginAllowed(_ => true) // Cho phép tất cả origin một cách linh hoạt
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
            });

  
            var app = builder.Build();
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
         

            // Sau app.UseRouting():
            app.UseCors("CorsPolicy");
            app.UseAuthorization();
            app.MapHub<ChatHub>("/chatHub");

            app.MapControllers();

            app.Run();
        }
    }
}
