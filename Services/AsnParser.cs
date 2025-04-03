namespace AsnFileParser.Services;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AsnFileParser.Models;

public class AsnParser
{
    public async Task<List<Box>> ParseFileAsync(string filePath)
    {
        var boxes = new List<Box>();
        Box? currentBox = null;

        // parses line-by-line instead of using StreamReader to load whole file into memory till EOF is reached, prevents OutOfMemoryException 

        await foreach (string line in File.ReadLinesAsync(filePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("HDR"))
            {
                if (currentBox != null) boxes.Add(currentBox);

                currentBox = new Box
                {
                    SupplierIdentifier = line.Substring(4, 10).Trim(),
                    CartonBoxIdentifier = line.Substring(50).Trim(),
                    Contents = new List<BoxContent>()
                };
            }
            else if (line.StartsWith("LINE") && currentBox != null)
            {
                var content = new BoxContent
                {
                    PoNumber = line.Substring(5, 25).Trim(),
                    Isbn = line.Substring(30, 20).Trim(),
                    Quantity = int.Parse(line.Substring(55).Trim())
                };
                currentBox.Contents.Add(content);
            }
        }

        if (currentBox != null) boxes.Add(currentBox);
        return boxes;
    }
}