using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuanLyKho.Server.Hubs;
using QuanLyKho.Server.Models;
using QuanLyKho.Server.Services;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=103.28.36.36;Database=nhkimi2o_quanlykho_db;User Id=nhkimi2o_kimsakota;Password=Cute123_VN@;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ===== C?U H?NH JWT AUTHENTICATION =====
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-secret-key-at-least-32-characters-long-for-security";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "QuanLyKhoAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "QuanLyKhoClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // THÊM: Cấu hình để lấy Token từ Query String (cho SignalIR)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                // Nếu request là cho Hub (ví dụ đường dẫn bắt đầu bằng /hubs)
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ===== ĐĂNG K? SERVICES =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ConnectionManager>();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// THAY ĐỔI: Cấu hình CORS cho SignalIR (AllowAnyOrigin không hoạt động với AllowCredentials)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Cho phép mọi nguồn (thay cho AllowAnyOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Bắt buộc phải có để SignalIR client kết nối
    });
});

// THÊM: Đăng ký dịch vụ SignalIR
builder.Services.AddSignalR();



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "API with JWT Authentication"
    });

    // Add JWT Bearer authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT token (không còn ghi chữ 'Bearer')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",       // lưu ?: thư?ng vi?t lowercase
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

    // Optional: Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Tách biệt xử lý lỗi
    /*app.UseExceptionHandler(appError =>
    {
        appError.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\": \"Đã xảy ra lỗi không mong muốn.\"}");
        });
    });

    // Trả về View cho lỗi Web thông thường
    app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), appBuilder =>
    {
        appBuilder.UseExceptionHandler("/Home/Error");
    });*/
}
app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll"); 

app.UseAuthentication(); 

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notification");

app.Run();
