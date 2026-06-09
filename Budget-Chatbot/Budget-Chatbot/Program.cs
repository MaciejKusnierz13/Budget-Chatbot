using AI.Integration.Configuration;
using AI.Integration.Services;
using Budget_Chatbot.Services;
using BudgetChatbot.Core.DTOs;
using BudgetChatbot.Core.Entities;
using BudgetChatbot.Infrastructure.Data;
using BudgetChatbot.Services;
using Core.DTOs;
using Microsoft.AspNetCore.Http;
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

            // Rejestracja serwisu autoryzacji
            builder.Services.AddScoped<AuthService>();

            // W³¹czenie sesji
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(12);
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
            app.UseSession();
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
            app.MapPost("/api/chat", async (string message, HttpContext ctx, TransactionBotService botService, AppDbContext db) =>
            {
                var userId = ctx.Session.GetInt32("UserId") ?? 1;

                var words = message.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length <= 1 && !System.Text.RegularExpressions.Regex.IsMatch(message, @"\d"))
                    return Results.BadRequest("Wiadomoœæ wygl¹da jak test lub jest zbyt krótka. Napisz np. 'Kawa 12 z³'.");

                var transaction = await botService.ProcessMessageAndSaveAsync(userId, message);

                if (transaction == null)
                    return Results.BadRequest("Bot nie zrozumia³ transakcji lub wyst¹pi³ b³¹d.");

                // kwota
                if (transaction.Amount <= 0)
                    return Results.BadRequest("Nie wykryto kwoty. Napisz np. 'Kawa 12 z³'.");

                // proste przykladowe teksty
                if (string.IsNullOrWhiteSpace(transaction.Description) ||
                    transaction.Description.ToLower() is "test" or "unknown" or "brak" or "null")
                    return Results.BadRequest("Opis transakcji jest nieprawid³owy. Spróbuj jeszcze raz.");

                return Results.Ok(new
                {
                    Info = "Sukces! Zapisano w bazie.",
                    SavedTransaction = transaction
                });
            });

            // ZAPIS WIADOMOŒCI DO HISTORII CHATU
            app.MapPost("/api/chat/history", async (ChatMessageDto dto, HttpContext ctx, AppDbContext db) =>
            {
                var userId = ctx.Session.GetInt32("UserId") ?? 1;

                var entry = new ChatHistory
                {
                    UserId = userId,
                    SessionId = dto.SessionId,
                    ChatTitle = dto.ChatTitle,
                    Role = dto.Role,
                    Content = dto.Content,
                    Timestamp = DateTime.UtcNow
                };

                db.ChatHistories.Add(entry);
                await db.SaveChangesAsync();

                return Results.Ok();
            });

            // £ADOWANIE HISTORII CHATÓW DLA U¯YTKOWNIKA
            app.MapGet("/api/chat/history", async (HttpContext ctx, AppDbContext db) =>
            {
                var userId = ctx.Session.GetInt32("UserId") ?? 1;

                var messages = await db.ChatHistories
                    .Where(h => h.UserId == userId)
                    .OrderBy(h => h.Timestamp)
                    .Select(h => new
                    {
                        h.SessionId,
                        h.ChatTitle,
                        h.Role,
                        h.Content,
                        h.Timestamp
                    })
                    .ToListAsync();

                var chats = messages
                    .GroupBy(m => m.SessionId)
                    .Select(g => new
                    {
                        SessionId = g.Key,
                        Title = g.First().ChatTitle,
                        Messages = g.Select(m => new { m.Role, m.Content, m.Timestamp }).ToList()
                    })
                    .OrderByDescending(c => c.Messages.Last().Timestamp)
                    .ToList();

                return Results.Ok(chats);
            });

            // USUWANIE CHATU
            app.MapDelete("/api/chat/history/{sessionId}", async (string sessionId, HttpContext ctx, AppDbContext db) =>
            {
                var userId = ctx.Session.GetInt32("UserId") ?? 1;

                var entries = db.ChatHistories.Where(h => h.UserId == userId && h.SessionId == sessionId);
                db.ChatHistories.RemoveRange(entries);
                await db.SaveChangesAsync();

                return Results.Ok();
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

            // Endpoint Rejestracji
            app.MapPost("/api/register", async (RegisterDto dto, AuthService authService) =>
            {
                bool result = await authService.RegisterAsync(dto);

                if (!result)
                {
                    return Results.BadRequest("Taki u¿ytkownik ju¿ istnieje.");
                }

                return Results.Ok("Konto utworzone.");
            });

            // Endpoint Logowania
            app.MapPost("/api/login", async (LoginDto dto, AuthService authService, HttpContext context) =>
            {
                // specjalny admin
                if (dto.Username == "admin" && dto.Password == "admin123")
                {
                    context.Session.SetString("Role", "Admin");
                    context.Session.SetString("Username", "admin");
                    return Results.Ok("Zalogowano jako admin.");
                }

                var user = await authService.LoginAsync(dto);

                if (user == null)
                {
                    return Results.Unauthorized();
                }

                context.Session.SetString("Role", "User");
                context.Session.SetString("Username", user.Username);
                context.Session.SetInt32("UserId", user.Id);

                return Results.Ok("Zalogowano.");
            });

            // Endpoint wylogowania
            app.MapPost("/api/logout", (HttpContext context) =>
            {
                context.Session.Clear();
                return Results.Ok("Wylogowano.");
            });

            // CMS - panel administratora
            app.MapGet("/api/admin", (HttpContext context) =>
            {
                string? role = context.Session.GetString("Role");

                if (role != "Admin")
                {
                    return Results.Unauthorized();
                }

                return Results.Ok("Witaj w panelu administratora.");
            });

            // Lista u¿ytkowników dla admina
            app.MapGet("/api/admin/users", async (HttpContext context, AppDbContext db) =>
            {
                string? role = context.Session.GetString("Role");

                if (role != "Admin")
                {
                    return Results.Unauthorized();
                }

                var users = await db.Users
                    .Select(x => new { x.Id, x.Username, x.Email })
                    .ToListAsync();

                return Results.Ok(users);
            });

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

            // START APKI ===============================================
            app.Run();
        }
    }
}