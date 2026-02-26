namespace Selu383.SP26.Api.Features.Locations;

public class LocationDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public int TableCount { get; set; }

    // Tests send an int (e.g., 46). Must be int?
    public int? ManagerId { get; set; }
}