namespace WebAPI;

public static class ExternalAuthentication
{
    public const string CookieScheme = "ExternalLogin";
    public const string ProviderItem = "provider";
    public const string ReturnUrlItem = "returnUrl";
    public const string CompletedClaim = "external_login_completed";

    public static readonly string[] Providers = ["Yandex", "Vkontakte"];
}
