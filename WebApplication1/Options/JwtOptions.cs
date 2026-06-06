namespace WebApplication1.Options;

/// <summary>
/// JWT 設定類別
/// 
/// 用來對應 appsettings.json 內的 Jwt 區段設定。
/// 通常會透過 IOptions<JwtOptions> 注入使用。
/// 
/// ⚠️ 這個類別本身不做任何驗證，只是設定容器。
/// 真正的安全性取決於：
/// - Key 是否足夠安全
/// - Issuer / Audience 是否正確
/// - Token 是否正確驗證
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// JWT 簽章金鑰（Secret Key）
    /// 
    /// 這是整個 JWT 最重要的安全核心。
    /// 伺服器會用這個 Key 對 Token 做簽章。
    /// 驗證時也會用這個 Key 驗證是否合法。
    /// 
    /// ⚠️ 重要注意事項：
    /// 1. 長度至少 32 bytes（建議 256-bit 以上）
    /// 2. 不要寫死在程式碼裡
    /// 3. 不要 commit 到 GitHub
    /// 4. 正式環境請放在：
    ///    - 環境變數
    ///    - Azure Key Vault
    ///    - Secret Manager
    /// 
    /// ❌ 常見錯誤：
    /// "123456" 這種超短字串
    /// 
    /// ✔ 建議：
    /// 使用隨機產生器產生高強度金鑰
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// JWT 發行者（Issuer）
    /// 
    /// 用來標示「這個 Token 是誰發的」。
    /// 
    /// 驗證 Token 時會檢查：
    /// Token 內的 iss 是否等於這裡設定的值。
    /// 
    /// ✔ 建議：
    /// - 使用你的 API 網域
    ///   例如：https://api.yourapp.com
    /// - 不要亂填
    /// 
    /// ⚠️ 如果驗證時設定 ValidateIssuer = true，
    /// 這個值必須完全一致。
    /// </summary>
    public string Issuer { get; set; } = null!;

    /// <summary>
    /// JWT 接收者（Audience）
    /// 
    /// 表示這個 Token 是發給誰使用的。
    /// 
    /// 驗證時會檢查：
    /// Token 內的 aud 是否符合。
    /// 
    /// 常見情境：
    /// - Web 前端
    /// - Mobile App
    /// - 不同系統之間 API 溝通
    /// 
    /// ✔ 單一系統可簡單填固定值
    /// ✔ 多系統時可設計不同 Audience
    /// 
    /// ⚠️ 若驗證時開啟 ValidateAudience，
    /// 這個值一定要對。
    /// </summary>
    public string Audience { get; set; } = null!;

    /// <summary>
    /// Token 有效期限（分鐘）
    /// 
    /// 產生 JWT 時會設定：
    /// expires = DateTime.UtcNow + ExpireMinutes
    /// 
    /// ⚠️ 設計考量：
    /// 
    /// ⏱ 太短：
    /// - 使用者會頻繁重新登入
    /// 
    /// ⏱ 太長：
    /// - Token 被盜風險變高
    /// - 無法即時強制登出
    /// 
    /// ✔ 常見建議：
    /// - 15 ~ 60 分鐘
    /// - 搭配 Refresh Token 使用
    /// 
    /// ⚠️ 目前你的專案如果沒有 Refresh Token，
    /// 建議不要設太長（例如不要設 7 天）
    /// </summary>
    public int ExpireMinutes { get; set; }
}