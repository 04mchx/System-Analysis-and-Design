using dbFinal.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization(); // 啟用模型驗證

// 添加 Session
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});


builder.Services.AddDbContext<db_testContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("WebDatabase"),
        new MySqlServerVersion(new Version(8, 0, 40))));
//Program.cs中加入資料庫物件的DI注入連接資料庫

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true; // 僅通過 HTTP 存取 Cookie，防範腳本攻擊
    options.Cookie.IsEssential = true; // Session 必需
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session 過期時間
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080); // HTTP
    serverOptions.ListenAnyIP(8081, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseDefaultFiles(); // 啟用預設檔案（如 index.html）
app.UseStaticFiles();  // 啟用靜態檔案服務

app.UseRouting();

app.UseSession(); // 啟用 Session
app.UseAuthorization();

// 設定 MVC 路由 (在靜態檔案之後)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
