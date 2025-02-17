namespace FourSPM_WebService.Config;

public class JwtConfig
{
    public required string Secret { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int ExpiryInMinutes { get; set; } = 60;
} 