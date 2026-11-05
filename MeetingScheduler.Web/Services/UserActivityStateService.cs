using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace MeetingScheduler.Web.Services;

public interface IUserActivityStateService
{
    UserActivitySnapshot Read(HttpContext context);
    void RecordRequest(HttpContext context, string feature);
    void StartSession(HttpContext context, int userId, string roleName);
    void ClearSession(HttpContext context);
    void ResetUsageCookie(HttpContext context);
}

public sealed record UserActivitySnapshot(int VisitCount, DateTimeOffset? LastAccessUtc, string MostUsedFeature,
    DateTimeOffset? LoginAtUtc);

public sealed class UserActivityStateService(IDataProtectionProvider protectionProvider) : IUserActivityStateService
{
    public const string CookieName = ".MeetingScheduler.Usage";
    private const string UserIdKey = "Security.UserId";
    private const string RoleKey = "Security.RoleLabel";
    private const string LoginAtKey = "Security.LoginAtUtc";
    private const string RequestCountKey = "Security.RequestCount";
    private const string LastFeatureKey = "Security.LastFeature";
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(30);
    private readonly IDataProtector protector = protectionProvider.CreateProtector("MeetingScheduler.UsageCookie.v1");

    public UserActivitySnapshot Read(HttpContext context)
    {
        var usage = ReadCookie(context);
        var favorite = usage.FeatureHits.OrderByDescending(x => x.Value).Select(x => x.Key).FirstOrDefault() ?? "Chưa có dữ liệu";
        return new(usage.VisitCount, usage.LastAccessUtc, favorite, Parse(context.Session.GetString(LoginAtKey)));
    }

    public void RecordRequest(HttpContext context, string feature)
    {
        var usage = ReadCookie(context);
        usage.VisitCount++;
        usage.LastAccessUtc = DateTimeOffset.UtcNow;
        usage.ExpiresUtc = DateTimeOffset.UtcNow.Add(CookieLifetime);
        usage.FeatureHits[feature] = usage.FeatureHits.GetValueOrDefault(feature) + 1;
        usage.FeatureHits = usage.FeatureHits.OrderByDescending(x => x.Value).Take(8)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        context.Items[CookieName] = usage;

        context.Response.Cookies.Append(CookieName, protector.Protect(JsonSerializer.Serialize(usage)), new CookieOptions
        {
            Expires = usage.ExpiresUtc,
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true
        });
        context.Session.SetInt32(RequestCountKey, (context.Session.GetInt32(RequestCountKey) ?? 0) + 1);
        context.Session.SetString(LastFeatureKey, feature);
    }

    public void StartSession(HttpContext context, int userId, string roleName)
    {
        context.Session.Clear();
        context.Session.SetInt32(UserIdKey, userId);
        // Vai trò trong Session chỉ phục vụ trạng thái giao diện; quyền truy cập dùng Claims đã ký và kiểm tra lại với cơ sở dữ liệu.
        context.Session.SetString(RoleKey, roleName);
        context.Session.SetString(LoginAtKey, DateTimeOffset.UtcNow.ToString("O"));
        context.Session.SetInt32(RequestCountKey, 0);
        context.Session.SetString(LastFeatureKey, "Đăng nhập");
    }

    public void ClearSession(HttpContext context) => context.Session.Clear();
    public void ResetUsageCookie(HttpContext context) => context.Response.Cookies.Delete(CookieName);

    private UsageCookie ReadCookie(HttpContext context)
    {
        if (context.Items.TryGetValue(CookieName, out var current) && current is UsageCookie currentUsage)
            return currentUsage;
        if (!context.Request.Cookies.TryGetValue(CookieName, out var value)) return new UsageCookie();
        try { return JsonSerializer.Deserialize<UsageCookie>(protector.Unprotect(value)) ?? new UsageCookie(); }
        catch (Exception) { return new UsageCookie(); }
    }

    private static DateTimeOffset? Parse(string? value) => DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

    private sealed class UsageCookie
    {
        public int VisitCount { get; set; }
        public DateTimeOffset? LastAccessUtc { get; set; }
        public DateTimeOffset? ExpiresUtc { get; set; }
        public Dictionary<string, int> FeatureHits { get; set; } = new(StringComparer.Ordinal);
    }
}
