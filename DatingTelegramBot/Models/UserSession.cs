using DatingContracts;

namespace DatingTelegramBot.Models;

public class UserSession
{
    public long ChatId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; } 
    public int? Age { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<Guid>? Pictures { get; set; }
    
    public DialogState State { get; set; }
}