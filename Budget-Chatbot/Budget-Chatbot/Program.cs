using AI.Integration.Configuration;
using AI.Integration.Services;
using BudgetChatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Budget_Chatbot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Rejestracja SQL Server

            builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.Configure<LmStudioOptions>(builder.Configuration.GetSection("LMStudio"));

            // 2. Rejestrujemy LlmService jako Singleton (klient wewn¹trz jest przystosowany do dzia³ania w tle dla ca³ej aplikacji)
            builder.Services.AddSingleton<LlmService>();

            builder.Services.AddScoped<BudgetChatbot.Services.TransactionBotService>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapGet("/api/test-ai", async (AI.Integration.Services.LlmService llmService) =>
            {
                var response = await llmService.TestConnectionAsync("Czeœæ! Jesteœ moim nowym asystentem finansowym. Powiedz mi jedno zdanie na powitanie.");
                return Results.Ok(response);
            });

            app.Run();
        }
    }
}
