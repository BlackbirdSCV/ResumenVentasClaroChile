using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace VentasClaro;

public static class DriveSync
{
    const string ClientId = "258805615162-dp4c6f1si8270shsio4f6plrng1ae484.apps.googleusercontent.com";
    const string Archivo = "ventas_claro.db3";
    static readonly HttpClient Http = new();
    public static string Esquema => "com.googleusercontent.apps." + ClientId.Replace(".apps.googleusercontent.com", "");
    static string Redirect => Esquema + ":/oauth2redirect";
    static string? _token; static DateTime _vence;

    public static async Task<bool> HaySesion() => !string.IsNullOrEmpty(await SecureStorage.GetAsync("g_refresh"));
    public static void CerrarSesion() { SecureStorage.Remove("g_refresh"); _token = null; }

    static string B64(byte[] b) => Convert.ToBase64String(b).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static async Task Conectar()
    {
        var verifier = B64(RandomNumberGenerator.GetBytes(48));
        var challenge = B64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var url = "https://accounts.google.com/o/oauth2/v2/auth?response_type=code&access_type=offline&prompt=consent"
            + $"&client_id={Uri.EscapeDataString(ClientId)}&redirect_uri={Uri.EscapeDataString(Redirect)}"
            + "&scope=" + Uri.EscapeDataString("https://www.googleapis.com/auth/drive.appdata")
            + $"&code_challenge={challenge}&code_challenge_method=S256";
        var r = await WebAuthenticator.Default.AuthenticateAsync(new WebAuthenticatorOptions { Url = new Uri(url), CallbackUrl = new Uri(Redirect) });
        if (!r.Properties.TryGetValue("code", out var code)) throw new Exception("Google no devolvió el código de acceso.");
        var tok = await Token(new() { ["code"] = code, ["code_verifier"] = verifier, ["redirect_uri"] = Redirect, ["grant_type"] = "authorization_code" });
        if (tok.TryGetProperty("refresh_token", out var rt)) await SecureStorage.SetAsync("g_refresh", rt.GetString()!);
    }

    static async Task<JsonElement> Token(Dictionary<string, string> p)
    {
        p["client_id"] = ClientId;
        var res = await Http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(p));
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new Exception("Google rechazó el acceso: " + json);
        var doc = JsonDocument.Parse(json).RootElement.Clone();
        _token = doc.GetProperty("access_token").GetString();
        _vence = DateTime.UtcNow.AddSeconds(doc.GetProperty("expires_in").GetInt32() - 60);
        return doc;
    }

    static async Task<string> Acceso()
    {
        if (_token != null && DateTime.UtcNow < _vence) return _token;
        var rt = await SecureStorage.GetAsync("g_refresh") ?? throw new Exception("Conecta tu cuenta de Google primero.");
        await Token(new() { ["refresh_token"] = rt, ["grant_type"] = "refresh_token" });
        return _token!;
    }

    static async Task<HttpRequestMessage> Req(HttpMethod m, string url, HttpContent? c = null)
    {
        var q = new HttpRequestMessage(m, url) { Content = c };
        q.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Acceso());
        return q;
    }

    static async Task<(string Id, DateTime Mod)?> Buscar()
    {
        var q = Uri.EscapeDataString($"name='{Archivo}'");
        var res = await Http.SendAsync(await Req(HttpMethod.Get, $"https://www.googleapis.com/drive/v3/files?spaces=appDataFolder&q={q}&fields=files(id,modifiedTime)"));
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new Exception(json);
        var files = JsonDocument.Parse(json).RootElement.GetProperty("files");
        if (files.GetArrayLength() == 0) return null;
        return (files[0].GetProperty("id").GetString()!, files[0].GetProperty("modifiedTime").GetDateTime().ToLocalTime());
    }

    public static async Task<DateTime?> UltimoRespaldo() => (await Buscar())?.Mod;

    public static async Task Subir()
    {
        var bytes = await Db.LeerArchivo();
        HttpRequestMessage q;
        if (await Buscar() is { } e)
            q = await Req(new HttpMethod("PATCH"), $"https://www.googleapis.com/upload/drive/v3/files/{e.Id}?uploadType=media", new ByteArrayContent(bytes));
        else
        {
            var mp = new MultipartContent("related");
            mp.Add(new StringContent(JsonSerializer.Serialize(new { name = Archivo, parents = new[] { "appDataFolder" } }), Encoding.UTF8, "application/json"));
            mp.Add(new ByteArrayContent(bytes) { Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") } });
            q = await Req(HttpMethod.Post, "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart", mp);
        }
        var res = await Http.SendAsync(q);
        if (!res.IsSuccessStatusCode) throw new Exception(await res.Content.ReadAsStringAsync());
    }

    public static async Task Restaurar()
    {
        var f = await Buscar() ?? throw new Exception("No hay ningún respaldo en tu Drive.");
        var res = await Http.SendAsync(await Req(HttpMethod.Get, $"https://www.googleapis.com/drive/v3/files/{f.Id}?alt=media"));
        if (!res.IsSuccessStatusCode) throw new Exception(await res.Content.ReadAsStringAsync());
        await Db.ReemplazarArchivo(await res.Content.ReadAsByteArrayAsync());
    }
}
