using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DatingContracts;

namespace DatingAPI.Models;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long ChatId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; } 
    public int? Age { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DialogState? State { get; set; }
}