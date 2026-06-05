using AI.Integration.Configuration;
using AI.Integration.Services;
using Budget_Chatbot.Services;
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

            // Rejestracja ReportsService
            builder.Services.AddScoped<Budget_Chatbot.Services.ReportsService>();
            builder.Services.AddScoped<BudgetChatbot.Services.TransactionBotService>();
            builder.Services.AddScoped<Budget_Chatbot.Services.AdvancedChartsService>();


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

            // --- SEEDOWANIE BAZY DANYCH ---
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.Database.Migrate(); // upewnia siê, ¿e baza istnieje

                // =========================
                // USER
                // =========================
                var user = db.Users.FirstOrDefault();

                if (user == null)
                {
                    user = new User
                    {
                        Username = "TestUser",
                        Email = "test@test.com"
                    };

                    db.Users.Add(user);
                    db.SaveChanges();
                }

                // =========================
                // CATEGORIES
                // =========================
                if (!db.Categories.Any())
                {
                    var categories = new List<Category>
                    {
                        new Category { Name = "Jedzenie", IsExpense = true },
                        new Category { Name = "Transport", IsExpense = true },
                        new Category { Name = "Rozrywka", IsExpense = true },
                        new Category { Name = "Wyp³ata", IsExpense = false }
                    };

                    db.Categories.AddRange(categories);
                    db.SaveChanges();
                }

                // =========================
                // TRANSAKCJE TESTOWE
                // =========================
                if (!db.Transactions.Any())
                {
                    var rnd = new Random();

                    var categories = db.Categories.ToList();

                    var foodCategory = categories.First(c => c.Name == "Jedzenie");
                    var transportCategory = categories.First(c => c.Name == "Transport");
                    var entertainmentCategory = categories.First(c => c.Name == "Rozrywka");
                    var salaryCategory = categories.First(c => c.Name == "Wyp³ata");

                    var transactions = new List<Transaction>();

                    var startDate = DateTime.UtcNow.AddMonths(-4);

                    // tydzieñ bez transakcji
                    var emptyWeekStart = startDate.AddDays(45);
                    var emptyWeekEnd = emptyWeekStart.AddDays(7);

                    // miesiêczne wyp³aty
                    for (int i = 0; i < 4; i++)
                    {
                        transactions.Add(new Transaction
                        {
                            UserId = user.Id,
                            CategoryId = salaryCategory.Id,
                            Amount = 5000 + rnd.Next(-300, 500),
                            Description = "Wyp³ata",
                            Date = startDate.AddMonths(i).AddDays(1)
                        });
                    }

                    // losowe wydatki
                    for (int i = 0; i < 200; i++)
                    {
                        DateTime date;

                        do
                        {
                            date = startDate.AddDays(rnd.Next(0, 120));
                        }
                        while (date >= emptyWeekStart && date <= emptyWeekEnd);

                        var category = rnd.Next(3);

                        int categoryId;
                        string description;
                        decimal amount;

                        switch (category)
                        {
                            case 0:
                                categoryId = foodCategory.Id;
                                description = "Zakupy spo¿ywcze";
                                amount = rnd.Next(10, 200);
                                break;

                            case 1:
                                categoryId = transportCategory.Id;
                                description = "Transport";
                                amount = rnd.Next(5, 80);
                                break;

                            default:
                                categoryId = entertainmentCategory.Id;
                                description = "Rozrywka";
                                amount = rnd.Next(20, 300);
                                break;
                        }

                        transactions.Add(new Transaction
                        {
                            UserId = user.Id,
                            CategoryId = categoryId,
                            Amount = amount,
                            Description = description,
                            Date = date
                        });
                    }

                    db.Transactions.AddRange(transactions);
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

            // SALDO
            app.MapGet("/api/reports/balance/{userId}", (int userId, ReportsService service) =>
            {
                var result = service.GetBalance(userId);
                return Results.Ok(result);
            });

            // WYKRES SALDA
            app.MapGet("/api/reports/balance-chart/{userId}", (int userId, DateTime? startDate, DateTime? endDate, ReportsService service) =>
            {
                var result = service.GetBalanceChart(userId, startDate, endDate);
                return Results.Ok(result);
            });

            // UDZIA£ KATEGORII WYDATKÓW W CZASIE
            app.MapGet("/api/reports/weekly-category-share/{userId}", (int userId, DateTime? startDate, DateTime? endDate, ReportsService service) =>
            {
                var result = service.GetWeeklyExpenseCategoryShare(userId, startDate, endDate);
                return Results.Ok(result);
            });

            // TOP WYDATKÓW
            app.MapGet("/api/reports/top-expenses/{userId}", (int userId, DateTime? startDate, DateTime? endDate, ReportsService service) =>
            {
                var result = service.GetTopExpenses(userId, startDate, endDate);
                return Results.Ok(result);
            });

            app.Run();

            app.MapGet("/api/reports/advanced/category-bar/{userId}", (int userId, DateTime? startDate, DateTime? endDate, Budget_Chatbot.Services.AdvancedChartsService service) =>
            {
                return Results.Ok(service.GetCategoryBarChart(userId, startDate, endDate));
            });

            app.MapGet("/api/reports/advanced/category-line-time/{userId}", (int userId, DateTime? startDate, DateTime? endDate, Budget_Chatbot.Services.AdvancedChartsService service) =>
            {
                return Results.Ok(service.GetCategoryLineChartOverTime(userId, startDate, endDate));
            });

            app.MapGet("/api/reports/advanced/summary-bar-time/{userId}", (int userId, DateTime? startDate, DateTime? endDate, Budget_Chatbot.Services.AdvancedChartsService service) =>
            {
                return Results.Ok(service.GetSummaryBarChartOverTime(userId, startDate, endDate));
            });
        }
    }
}
