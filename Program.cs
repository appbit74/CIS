using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using CIS.Data;
using Microsoft.AspNetCore.Authentication.Cookies; // 1. *** เพิ่ม Using นี้ ***
using Microsoft.AspNetCore.Authorization; // 2. *** เพิ่ม Using นี้ ***
using Microsoft.AspNetCore.Mvc.Authorization; // 3. *** เพิ่ม Using นี้ ***

var builder = WebApplication.CreateBuilder(args);

// --- 1. ส่วน Database (เหมือนเดิม) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<CIS.Services.UserDisplayService>();

// --- 2. *** นี่คือการ "ผ่าตัด" ระบบ Authentication *** ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) // 4. "Cookie" คือผู้คุมประตูหลัก
    .AddCookie(options =>
    {
        // 5. ถ้ายังไม่ล็อกอิน, ให้วิ่งไปหน้านี้
        options.LoginPath = "/Account/Login";
        // 6. ถ้าสิทธิ์ไม่พอ, ให้วิ่งไปหน้านี้
        options.AccessDeniedPath = "/Account/AccessDenied";
    })
    .AddNegotiate(); // 7. เรายัง "เก็บ" Windows Auth (Negotiate) ไว้ใช้ (แต่ไม่ได้เป็น Default)

// --- 3. *** ระบบ Authorization (ปรับปรุงใหม่) *** ---
builder.Services.AddAuthorization(options =>
{
    // 8. นโยบาย "ต้องล็อกอิน" (เหมือนเดิม)
    // แต่คราวนี้ มันจะถูกจัดการโดย Cookie Auth
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// --- 4. ส่วน Controller (ปรับปรุงใหม่) ---
builder.Services.AddControllersWithViews(options =>
{
    // 9. เราใช้ FallbackPolicy ด้านบนแล้ว
    // (วิธีเก่า: options.FallbackPolicy = options.DefaultPolicy;)
});

// [เพิ่มส่วนนี้] ลงทะเบียน HttpClient และ Service
builder.Services.AddHttpClient<CIS.Services.ICrimsApiService, CIS.Services.CrimsApiService>(client =>
{
    // ตั้งค่า Timeout เผื่อ API ปลายทางช้า (เช่น รอ 10 วินาที)
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<CIS.Services.IAdService, CIS.Services.AdService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // ต้องมาก่อน Auth

// --- 5. ลำดับการ "เปิดสวิตช์" (เหมือนเดิม แต่ความหมายเปลี่ยน) ---
app.UseAuthentication(); // 10. เปิดใช้ "ผู้คุมประตู" (ซึ่งตอนนี้คือ Cookie)
app.UseAuthorization(); // 11. เปิดใช้ "ยาม" (ที่คอยเช็คสิทธิ์)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ================== เพิ่มส่วนนี้ลงไป (SEED DATA) ==================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CIS.Data.ApplicationDbContext>();

        // เรียกใช้ฟังก์ชั่น Initialize ที่เราเพิ่งสร้าง
        CIS.Data.DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "เกิดข้อผิดพลาดในการสร้างข้อมูลตั้งต้น (Seeding DB)");
    }
}
// ==============================================================

app.Run();