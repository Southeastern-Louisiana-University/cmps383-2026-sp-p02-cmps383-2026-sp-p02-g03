namespace Selu383.SP26.Api.Features.Locations;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public int TableCount { get; set; }

    // Must match DTO type (int?) so it round-trips
    public int? ManagerId { get; set; }
}