using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ChatMemoryApi.Extensions;
using ChatMemoryApi.Data;
using Microsoft.EntityFrameworkCore;
using ChatMemoryApi.DTOs;

namespace ChatMemoryApi.Controllers
{
    // =============================
    // UsersController
    // 功能：
    // - 提供與「目前登入使用者」相關的 API
    // - 透過 JWT 驗證身份
    // =============================
    [ApiController] // 啟用自動模型驗證與錯誤處理
    [Route("api/users")] // API 路由前綴
    public class UsersController : ControllerBase
    {
        // 先注入 DbContext
        private readonly AppDbContext _db;

        public UsersController(AppDbContext db)
        {
            _db = db;
        }
        // =============================
        // 取得目前登入使用者資訊
        // GET api/users/me
        // =============================
        // [Authorize] 代表：
        // 1️ 必須帶有效 JWT Token
        // 2️ Token 必須未過期
        // 3️ 簽章必須正確
        // 4️ issuer / audience 必須符合設定
        //
        // 如果驗證失敗：
        // → ASP.NET Core 會自動回傳 401 Unauthorized
        //
        // 前提：
        // Program.cs 必須有：
        // builder.Services.AddAuthentication(...)
        // app.UseAuthentication();
        // app.UseAuthorization();
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            // =============================
            // User 物件說明
            // =============================
            // User 是 ClaimsPrincipal
            // 由 JWT Middleware 解析 Token 後自動建立
            //
            // 裡面包含：
            // - Claims（Token 中的資料）
            // - Identity
            // - IsAuthenticated 狀態

            // =============================
            // 取得 JWT 內的 Subject (sub)
            // =============================
            // 在你之前的 AuthController 中：
            // new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
            //
            // 所以這裡會取得使用者 Id
            var userId = User.GetUserId();

            // =============================
            // 取得 Email Claim
            // =============================
            var user = await _db.Users
             .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserMeDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new ErrorResponse
                {
                    Success = false,
                    Message = "User not found"
                });

            }

            // =============================
            // ⚠️ 注意事項
            // =============================
            // 1️⃣ 如果 Claim 名稱拼錯，會回傳 null
            // 2️⃣ 如果 Token 裡沒有該 Claim，也會回傳 null
            // 3️⃣ FindFirstValue 比 FindFirst 更安全（自動取 Value）

            // =============================
            // 回傳目前登入者資訊
            // =============================
            return Ok(new ApiResponse<UserMeDto>
            {
                Success = true,
                Message = "User retrieved",
                Data = user
            });


            // ⚠️ 這裡只回傳必要資訊
            // 不要回傳：
            // - PasswordHash
            // - 內部欄位
            // - 敏感資料
        }
    }
}