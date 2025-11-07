using System.ComponentModel.DataAnnotations;

namespace Adastral.Cockatoo.DataAccess.Models;

public class ServiceAccountTokenModel
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string Token { get; set; } = GenerateToken();
    public Guid ServiceAccountId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? CreatedBySessionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public static string GenerateToken()
    {
        var rand = new Random();
        var characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789><.#!~";
        return new string(Enumerable
            .Range(0, 32)
            .Select(num => characters[rand.Next() % characters.Length])
            .ToArray());
    }
}