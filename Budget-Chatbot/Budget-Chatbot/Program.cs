using AI.Integration.Configuration;
using AI.Integration.Services;
using BudgetChatbot.Core.Entities;
using BudgetChatbot.Infrastructure.Data;
using BudgetChatbot.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

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

            // Rejestracja serwisów dla Swaggera
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

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

            // SEGMENCIK TESTOWY, JAK NAJBARDZIEJ MO¯NA USUN¥Æ

            // --- 1. SEEDOWANIE BAZY DANYCH ---
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Dodanie testowego u¿ytkownika
                if (!db.Users.Any())
                {
                    db.Users.Add(new User { Username = "TestowyStudent", Email = "student@agh.edu.pl" });
                    db.SaveChanges();
                }

                // Podstawowe kategorie, *testowo* DANIEL ADAM mo¿ecie zmieniaæ
                if (!db.Categories.Any())
                {
                    db.Categories.AddRange(
                        new Category { Name = "Jedzenie", IsExpense = true },
                        new Category { Name = "Transport", IsExpense = true },
                        new Category { Name = "Wyp³ata", IsExpense = false }
                    );
                    db.SaveChanges();
                }
            }

            // --- 2. TESTOWY ENDPOINT API ---
            app.MapPost("/api/chat", async (string message, TransactionBotService botService) =>
            {
                // Na sztywno podajemy ID testowego u¿ytkownika (1), którego stworzyliœmy wy¿ej
                var transaction = await botService.ProcessMessageAndSaveAsync(1, message);

                if (transaction == null)
                {
                    return Results.BadRequest("Bot nie zrozumia³ transakcji lub wyst¹pi³ b³¹d.");
                }

                return Results.Ok(new
                {
                    Info = "Sukces! Zapisano w bazie.",
                    SavedTransaction = transaction
                });
            });

            app.Run();
        }
    }
}
