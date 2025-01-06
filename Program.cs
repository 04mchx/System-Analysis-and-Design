using dbFinal.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization(); // �ҥμҫ�����

// �K�[ Session
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});


builder.Services.AddDbContext<db_testContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("WebDatabase"),
        new MySqlServerVersion(new Version(8, 0, 40))));
//Program.cs���[�J��Ʈw����DI�`�J�s����Ʈw

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true; // �ȳq�L HTTP �s�� Cookie�A���d�}������
    options.Cookie.IsEssential = true; // Session ����
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session �L���ɶ�
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
app.UseDefaultFiles(); // �ҥιw�]�ɮס]�p index.html�^
app.UseStaticFiles();  // �ҥ��R�A�ɮתA��

app.UseRouting();

app.UseSession(); // �ҥ� Session
app.UseAuthorization();

// �]�w MVC ���� (�b�R�A�ɮפ���)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
