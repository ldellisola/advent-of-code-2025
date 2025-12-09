using System.Collections.Frozen;
using System.Diagnostics;
using AdventOfCode2025.Day9;

var input = File.ReadLines("input.txt")
    .Select(t=> t.Split(',',StringSplitOptions.TrimEntries| StringSplitOptions.RemoveEmptyEntries))
    .Select(t=> new Tile(Col: long.Parse(t[0]), Row: long.Parse(t[1])))
    .ToArray();

var result = Part2(input);

Console.WriteLine(result);



static void SaveAsPicture(Dictionary<long, (long Min, long Max)> sparseMap, int totalRows, int totalCols, string path)
{
    BigTiffWriter.Create(
        path,
        width: totalCols*10,
        height: totalRows*10,
        paint:  pos =>
        {
            var hasSparseRow = sparseMap.TryGetValue(pos.row/10, out var sparseRow);
            return hasSparseRow && sparseRow.Min <= pos.col/10 && pos.col/10 <= sparseRow.Max;
        });
    
}

static long Part2(Tile[] input)
{
    var greenTiles = GenerateGreenTiles(input);
    
    Dictionary<long, (long Min, long Max)> sparseMap = greenTiles
        .Concat(input)
        .GroupBy(t => t.Row, t => t.Col)
        .ToDictionary(t => t.Key, t => (t.Min(), t.Max()));
    
    // SaveAsPicture(sparseMap,(int)totalRows, (int) totalCols, $"C:\\Temp\\test\\{DateTime.Now:HH_m_s}.tiff");

    var rectangles = GetTilesWithDistance(input)
        .OrderByDescending(t => t.Area);

    foreach (var rectangle in rectangles)
    {
        // Console.WriteLine($"{ rectangle.Start} to  {rectangle.End}. Area: {rectangle.Area}");
        if (SquareHasOnlyValidTiles(sparseMap, rectangle.Start, rectangle.End))
        {
            return rectangle.Area;
        }
    }

    return -1;
}

static IEnumerable<(long Area, Tile Start, Tile End)> GetTilesWithDistance(Tile[] redTiles)
{
    for (int i = 0; i < redTiles.Length; i++)
    {
        for (int j = i + 1; j < redTiles.Length; j++)
        {
            yield return (redTiles[i].Area(redTiles[j]), redTiles[i], redTiles[j]);
        }
    }
}

static bool SquareHasOnlyValidTiles(Dictionary<long, (long Min, long Max)> sparseMatrix, Tile start, Tile stop)
{
    var startRow = Math.Min(start.Row, stop.Row);
    var endRow = Math.Max(start.Row, stop.Row);

    for (long row = startRow; row <= endRow; row++)
    {
        if (!sparseMatrix.TryGetValue(row, out var startRowMap))
            return false;

        if (!(startRowMap.Min <= stop.Col && stop.Col <= startRowMap.Max))
            return false;
        if (!(startRowMap.Min <= start.Col && start.Col <= startRowMap.Max))
            return false;
    }

    return true;
}

static List<Tile> GenerateGreenTiles(Tile[] redTiles)
{
    List<Tile> greenTiles = new(redTiles.Length * 2);
    Console.WriteLine("Defining vertical edges");
    var columnGroup = redTiles
        .GroupBy(t => t.Col, t => t.Row)
        .ToFrozenDictionary(t => t.Key, t => t.Order().ToArray());

    foreach (var (col, rows) in columnGroup)
    {
        var startRow = rows[0];
        var endRow = rows[^1];

        for (var i = startRow; i <= endRow; i++)
        {
            greenTiles.Add(new Tile(col, i));
        }
    }
    
    Console.WriteLine("Defining horizontal edges");
    var rowGroup = redTiles
        .GroupBy(t => t.Row, t => t.Col)
        .ToFrozenDictionary(t => t.Key, t => t.Order().ToArray());

    foreach (var  (row, cols) in rowGroup)
    {
        var startCol = cols[0];
        var endCol = cols[^1];

        for (var i = startCol; i <= endCol; i++) 
            greenTiles.Add(new(i,row));
    }

    return greenTiles;
}


static long Part1(Tile[] input)
{
    long maxArea = -1;
    for (int i = 0; i < input.Length; i++)
    {
        var (startCol, startRow) = input[i];
        for (int j = i+1; j < input.Length; j++)
        {
            var (endCol, endRow) = input[j];
        
            long area = Math.Abs(1 + startCol - endCol) * Math.Abs(1+startRow - endRow);

            Console.WriteLine($"{startCol},{startRow} and {endCol},{endRow} make {area}");

            if (area > maxArea)
                maxArea = area;
        }
    }

    return maxArea;
}


public record Tile(long Col, long Row)
{
    public long Area(Tile other) => (Math.Abs(Col - other.Col) +1) * (1+ Math.Abs(Row - other.Row));
}
