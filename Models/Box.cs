namespace AsnFileParser.Models;

public class Box
{
    public int Id { get; set; }
    public required string SupplierIdentifier { get; set; }
    public required string CartonBoxIdentifier { get; set; }
    public List<BoxContent> Contents { get; set; } = new();
}