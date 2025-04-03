namespace AsnFileParser.Models;

public class BoxContent
{
    public int Id { get; set; }
    public int BoxId { get; set; }
    public required string PoNumber { get; set; }
    public required string Isbn { get; set; }
    public int Quantity { get; set; }
}