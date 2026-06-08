using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    // ==========================================================
    // AppDbContext
    // ----------------------------------------------------------
    // 功能：
    // 1️⃣ 與資料庫溝通的核心類別
    // 2️⃣ 管理所有 Entity（資料表）
    // 3️⃣ 負責 LINQ 查詢 → 轉換成 SQL
    // 4️⃣ 管理 Migration / Schema
    //
    // DbContext 在 ASP.NET Core 中通常是：
    // - Scoped 生命週期（每個 Request 一個實例）
    //
    // ⚠️ 不要自己 new DbContext
    // 一定要透過 DI 注入
    // ==========================================================
    public class AppDbContext : DbContext
    {
        // ----------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------
        // options 來自 Program.cs 的：
        // builder.Services.AddDbContext<AppDbContext>(...)
        //
        // DbContextOptions 內包含：
        // - 連線字串
        // - 使用的資料庫類型（SQL Server / SQLite / PostgreSQL）
        // - Lazy Loading / Logging 設定
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ==========================================================
        // DbSet<T>
        // ----------------------------------------------------------
        // 每個 DbSet 代表一張資料表
        //
        // EF Core 會根據 Entity 類別：
        // - 建立資料表
        // - 建立欄位
        // - 建立關聯
        //
        // 命名慣例：
        // 類別 User → 資料表 Users
        // ==========================================================

        // 使用者資料表
        public DbSet<User> Users => Set<User>();

        // 聊天訊息資料表
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        // 記憶資料表（例如 RAG / 長期記憶）
        public DbSet<Memory> Memories => Set<Memory>();


        // ==========================================================
        // OnModelCreating
        // ----------------------------------------------------------
        // 用來客製化資料表設定
        // - Index
        // - 關聯（Foreign Key）
        // - 欄位長度
        // - 預設值
        // - Constraint
        //
        // ⚠️ 如果不寫，EF 會使用 Convention（預設規則）
        // ==========================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ==========================================================
            // User 設定
            // ==========================================================
            modelBuilder.Entity<User>(entity =>
            {
                // ----------------------------------------------------------
                // Email 設定為 citext（大小寫不敏感）
                // ----------------------------------------------------------
                // PostgreSQL 專用型別
                // 比較時會自動忽略大小寫
                entity.Property(u => u.Email)
                    .HasColumnType("citext")  // ⭐ 核心關鍵
                    .HasMaxLength(255)
                    .IsRequired();

                // ----------------------------------------------------------
                // 建立唯一索引
                // ----------------------------------------------------------
                // 為什麼要這樣做？
                //
                // 1️⃣ 確保 Email 不會重複（資料層保護）
                // 2️⃣ 加快登入查詢速度（WHERE Email = ?）
                //
                // ⚠️ 即使 Controller 有檢查 Email 是否存在
                // 還是必須在資料庫層做 Unique Constraint
                // 因為：
                // - 高併發情況下可能會同時註冊
                // - 只有 DB Constraint 才能 100% 保證唯一性
                // 因為是 citext
                // 所以 Unique 會自動大小寫不敏感
                entity.HasIndex(u => u.Email)
                      .IsUnique();

                entity.Property(u => u.PasswordHash)
                    .HasMaxLength(255)
                    .IsRequired();
            });

            // ==========================================================
            // ChatMessage 設定
            // ==========================================================

            modelBuilder.Entity<ChatMessage>()
                .Property(c => c.Content)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(c => c.UserId);

            // ==========================================================
            // Memory 設定
            // ==========================================================

            modelBuilder.Entity<Memory>()
                .Property(m => m.Summary)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<Memory>()
                .HasIndex(m => m.UserId);

            // ----------------------------------------------------------
            // 一定要呼叫 base
            // ----------------------------------------------------------
            // 如果未來繼承 IdentityDbContext 等
            // 不呼叫 base 可能會出問題
            base.OnModelCreating(modelBuilder);
        }
    }
}