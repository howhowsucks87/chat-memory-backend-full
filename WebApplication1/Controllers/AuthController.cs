using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatMemoryApi.Data;
using ChatMemoryApi.DTOs;
using ChatMemoryApi.Models;
using ChatMemoryApi.Options;
using Npgsql;

namespace ChatMemoryApi.Controllers
{
    // =====================
    // AuthController
    // 負責：使用者註冊、登入、JWT 發放
    //
    // ⚠️ 安全性重點：
    // - 不回傳 PasswordHash
    // - 不暴露使用者是否存在
    // - 使用 UTC 時間
    // - 密碼必須使用安全雜湊（BCrypt）
    // =====================
    [ApiController] // 啟用自動 Model Binding / 驗證錯誤回傳
    [Route("api/auth")] // API 路由前綴
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;   // EF Core 資料庫操作
        private readonly JwtOptions _jwt;    // JWT 設定（Key / Issuer / Audience / 過期時間）

        // =====================
        // Constructor
        // 使用 DI 注入 DbContext 與 JwtOptions
        // =====================
        public AuthController(
             AppDbContext db,
             IOptions<JwtOptions> jwtOptions
         )
        {
            _db = db;

            // IOptions<T> 是 ASP.NET Core 讀取 appsettings.json 的標準方式
            // .Value 才是真正的設定內容
            _jwt = jwtOptions.Value;    
        }

        // =====================
        // 註冊（Register）
        // =====================
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            // ----------------------------------------------------------
            // 1️ 正規化 Email（移除前後空白）
            // ----------------------------------------------------------
            var email = dto.Email.Trim().ToLowerInvariant();

            // ----------------------------------------------------------
            // 2️ 檢查是否已存在
            // ----------------------------------------------------------
            // 使用 AnyAsync 效能較好，只回傳 true / false
            if (await _db.Users.AnyAsync(u => u.Email == email))
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Email already exists"
                });


            if (dto.Password.Length < 8)
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Password must be at least 8 characters"
                });


            // ----------------------------------------------------------
            // 3️ 建立 User Entity
            // ----------------------------------------------------------

            var user = new User
            {
                Email = email,

                // 絕對不可儲存明碼密碼
                // BCrypt 會：
                // - 自動產生 Salt
                // - 自動處理加密成本
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),

                // 使用 UTC 時間，避免伺服器時區問題
                CreatedAt = DateTime.UtcNow
            };

            // ----------------------------------------------------------
            // 4️ 新增使用者並存入資料庫
            // ----------------------------------------------------------
            try
            {
                _db.Users.Add(user);

                await _db.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Register success",
                    Data = null
                });

            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException pgEx
                    && pgEx.SqlState == "23505")
                {
                    return BadRequest(new ErrorResponse
                    {
                        Success = false,
                        Message = "Email already exists"
                    });

                }

                throw;
            }
        }

        // =====================
        // 登入（Login）
        // =====================
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            // 正規化 Email
            var email = dto.Email.Trim();

            // 1️ 先用 Email 找使用者
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // 不要提示是「Email 錯」還是「密碼錯」
            // 這樣可以避免帳號被暴力嘗試
            if (user == null)
                return Unauthorized(new ErrorResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                });


            // 2️ 驗證密碼
            bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isValid)
                return Unauthorized(new ErrorResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                });


            // 3️ 產生 JWT Token
            var token = GenerateJwtToken(user);

            // 4️ 回傳 Token
            // 前端通常會存到：
            // - Memory
            // - LocalStorage
            // - HttpOnly Cookie（較安全）
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Login success",
                Data = new
                {
                    Token = token
                }
            });

        }

        // =====================
        // JWT Token 產生邏輯
        // =====================
        private string GenerateJwtToken(User user)
        {
            // =====================
            // Claims（Token 內的使用者資訊）
            // =====================
            var claims = new List<Claim>
            {
                // ASP.NET Core 標準使用者 Id
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()
    ),
                // JWT 標準欄位：Subject
                // 通常放 UserId
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

                // Email（可讓後端快速取得使用者資訊）
                new Claim(JwtRegisteredClaimNames.Email, user.Email),

                // JWT ID
                // 每個 Token 都不同，可用來防重放攻擊
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // =====================
            // 加密金鑰
            // =====================
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwt.Key)
            );

            // =====================
            // 簽章演算法
            // =====================
            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            // =====================
            // 建立 JWT Token
            // =====================
            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,     // 發行者（通常是你的 API）
                audience: _jwt.Audience, // 使用者（通常是前端 App）
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.ExpireMinutes), // 過期時間
                signingCredentials: creds
            );

            // =====================
            // 將 Token 轉成字串回傳
            // =====================
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}