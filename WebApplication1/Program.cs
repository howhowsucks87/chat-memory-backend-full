// =======================
// 基本 using
// =======================

// JWT 驗證核心
using Microsoft.AspNetCore.Authentication.JwtBearer;

// EF Core
using Microsoft.EntityFrameworkCore;

// JWT 驗證用的 Token 驗證參數
using Microsoft.IdentityModel.Tokens;

using System.Text;

// 自己的 DbContext
using ChatMemoryApi.Data;

// 自己的 Jwt 設定類別
using ChatMemoryApi.Options;

using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ChatMemoryApi.Extensions;


// 建立 WebApplication Builder
// 這是 .NET 6+ Minimal Hosting Model
var builder = WebApplication.CreateBuilder(args);

// =======================
// Services 註冊區
// =======================

// 註冊 Controller
// 讓專案可以使用 [ApiController]
builder.Services.AddControllers();


// =======================
// 註冊 DbContext
// =======================

// 使用 PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

/*
⚠️ 注意事項：

1️⃣ 連線字串不要寫死在程式碼
2️⃣ 正式環境應使用環境變數
3️⃣ DbContext 預設是 Scoped（每個 Request 一個實例）
4️⃣ 不要自己 new DbContext
*/


// =======================
// JWT Authentication 設定
// =======================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 讀取 appsettings.json 的 Jwt 區段
        var jwt = builder.Configuration
            .GetSection("Jwt")
            .Get<JwtOptions>()!;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // 驗證發行者
            ValidateIssuer = true,

            // 驗證接收者
            ValidateAudience = true,

            // 驗證是否過期
            ValidateLifetime = true,

            // 驗證簽章
            ValidateIssuerSigningKey = true,

            // 必須符合設定的 Issuer
            ValidIssuer = jwt.Issuer,

            // 必須符合設定的 Audience
            ValidAudience = jwt.Audience,

            // 使用對稱金鑰驗證簽章
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.Key)
            ),

            // 指定 Name Claim 對應
            // 這樣 User.Identity.Name 會對應 Sub
            NameClaimType = JwtRegisteredClaimNames.Sub,

            // 角色 Claim 對應
            RoleClaimType = ClaimTypes.Role,

            // 關閉預設 5 分鐘誤差
            ClockSkew = TimeSpan.Zero
        };
    });

/*
🔥 重要安全細節：

ClockSkew 預設是 5 分鐘。
意思是即使 Token 過期 5 分鐘內仍然有效。

你設為 0 → 更嚴格。

⚠️ 但如果伺服器時間不同步會導致驗證失敗。
正式環境務必使用 NTP 同步時間。
*/


// =======================
// 驗證 JwtOptions 啟動時檢查
// =======================

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))

    // Key 不能為空
    .Validate(o => !string.IsNullOrWhiteSpace(o.Key),
        "Jwt Key is required")

    // Key 至少 32 bytes
    .Validate(
        o => Encoding.UTF8.GetByteCount(o.Key) >= 32,
        "Jwt Key must be at least 32 bytes (256 bits)"
    )

    .Validate(
        o => !string.IsNullOrWhiteSpace(o.Issuer),
        "Jwt Issuer is required"
    )

    .Validate(
        o => !string.IsNullOrWhiteSpace(o.Audience),
        "Jwt Audience is required"
    )

    // 過期時間必須 > 0
    .Validate(o => o.ExpireMinutes > 0,
        "ExpireMinutes must be greater than 0")

    // 啟動時就驗證
    .ValidateOnStart();

/*
🔥 ValidateOnStart 很專業

代表：
如果設定錯誤，API 啟動時就會爆炸。

而不是等第一個請求才發現。

這是 Production 等級寫法。
*/


// =======================
// Swagger + JWT 支援
// =======================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // 定義 Bearer 認證方式
    options.AddSecurityDefinition("Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "輸入格式：Bearer {你的 JWT Token}"
        });

    // 全域要求使用 Bearer
    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type =
                                Microsoft.OpenApi.Models.ReferenceType
                                .SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

/*
🔥 好處：

Swagger 可以直接貼 Token 測試 API。
不用 Postman。
*/


// 建立 App
var app = builder.Build();


// =======================
// Middleware 區
// =======================

// 開發環境才啟用 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/*
⚠️ 正式環境通常會關閉 Swagger。
避免 API 文件公開。
*/


// 強制 HTTPS
app.UseHttpsRedirection();

/*
⚠️ JWT 一定要搭配 HTTPS
否則 Token 可被攔截。
*/

// 例外處理統一格式
app.UseGlobalException();

// 🔥 這行非常重要
app.UseAuthentication();

/*
一定要在 UseAuthorization 之前。

流程：
1️⃣ Authentication 驗證 Token
2️⃣ Authorization 判斷是否有權限

順序錯會導致：
User 永遠是空的
*/


app.UseAuthorization();


// 對應 Controller Route
app.MapControllers();


// 啟動應用程式
app.Run();