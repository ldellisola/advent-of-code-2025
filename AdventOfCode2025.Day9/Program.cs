using System.Collections.Frozen;
using AdventOfCode2025.Day9;
using Aspose.Drawing;
using Aspose.Drawing.Imaging;

var input = File.ReadLines("input.txt")
    .Select(t=> t.Split(',',StringSplitOptions.TrimEntries| StringSplitOptions.RemoveEmptyEntries))
    .Select(t=> new Tile(Col: long.Parse(t[0]), Row: long.Parse(t[1])))
    .ToArray();

// 37401302 too low
// 4647828380
// 1476517912
// 1476517912
// 1476517912
// 1476550548

var result = Part2(input);

Console.WriteLine();
Console.WriteLine();
Console.WriteLine();
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

    Console.WriteLine("Generating map from red tiles");
    var map = GenerateMap(input);
    // map.Print();
    Console.WriteLine("adding green tiles");
    var greenTiles = GenerateGreenTiles(input);
    

    Dictionary<long, (long Min, long Max)> sparseMap = greenTiles
        .Concat(input)
        .GroupBy(t => t.Row, t => t.Col)
        .ToDictionary(t => t.Key, t => (t.Min(), t.Max()));
    
   //  var (_,_,totalCols, totalRows) = GetBoundingBox(input);

    // SaveAsPicture(sparseMap,(int)totalRows, (int) totalCols, $"C:\\Temp\\test\\{DateTime.Now:HH_m_s}.tiff");
    

    // long maxArea = -1;

    Console.WriteLine("Calculating area");
    // long total = Enumerable.Sequence(input.Length, 1, -1).Sum();
    // long count = 0;


    var rectangles = GetTilesWithDistance(input)
        .OrderByDescending(t => t.Area);

    foreach (var rectangle in rectangles)
    {
        // Progress(count++, 99999999);
        Console.WriteLine($"{ rectangle.Start} to  {rectangle.End}. Area: {rectangle.Area}");
        if (SquareHasOnlyValidTiles(sparseMap, rectangle.Start, rectangle.End))
        {
            return rectangle.Area;
        }
    }

    return -1;
    
    // for (int i = 0; i < input.Length; i++)
    // {
    //     var (startCol, startRow) = input[i];
    //     for (int j = i+1; j < input.Length; j++)
    //     {
    //         Progress(count++, total);
    //         var (endCol, endRow) = input[j];
    //     
    //         long area = Math.Abs(1 + startCol - endCol) * Math.Abs(1+startRow - endRow);
    //
    //
    //         if (area > maxArea && SquareHasOnlyValidTiles(sparseMap,input[i], input[j]))
    //         {
    //             Console.WriteLine($"{startCol},{startRow} and {endCol},{endRow} make {area}");
    //             maxArea = area;
    //         }
    //     }
    // }
    //
    // Console.WriteLine("finito");
    //
    // return maxArea;

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
    // if (sparseMatrix.TryGetValue(start.Row, out var startRowMap) && sparseMatrix.TryGetValue(stop.Row, out var stopRowMap))
    //     return startRowMap.Min <= stop.Col && stop.Col <= startRowMap.Max
    //                                        && stopRowMap.Min <= start.Col && start.Col <= stopRowMap.Max;
    // return false;


    var startCol = Math.Min(start.Col, stop.Col);
    var startRow = Math.Min(start.Row, stop.Row);
    var endCol = Math.Max(start.Col, stop.Col);
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


    //
    // for (long row = startRow; row <= endRow; row++)
    // {
    //     for (long col = startCol;  col <= endCol; col++)
    //     {
    //         if (!map[row][col])
    //             return false;
    //     }
    // }
    //
    // return true;
}
// static bool SquareHasOnlyValidTiles(bool[][] map, Tile start, Tile stop)
// {
//     
//     var startCol = Math.Min(start.Col, stop.Col);
//     var startRow = Math.Min(start.Row, stop.Row);
//     var endCol = Math.Max(start.Col, stop.Col);
//     var endRow = Math.Max(start.Row, stop.Row);
//
//     for (long row = startRow; row <= endRow; row++)
//     {
//         for (long col = startCol;  col <= endCol; col++)
//         {
//             if (!map[row][col])
//                 return false;
//         }
//     }
//
//     return true;
// }

static bool[][] GenerateMap(Tile[] redTiles)
{
    var (startCols, startRows,totalCols, totalRows) = GetBoundingBox(redTiles);
    var map = new bool[totalRows][];
    for (int i = 0; i < totalRows; i++) 
        map[i] = new bool[totalCols];

    foreach (var (col, row) in redTiles)
    {
        map[row][col] = true;
    }
    
    return map;
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

static Tile[]  GenerateGreenTiles_old(bool[][] map, Tile[] redTiles)
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
            map[i][col] = true;
        }
    }
    
    map.Print();
    
    Console.WriteLine("Defining horizontal edges");
    var rowGroup = redTiles.
        GroupBy(t => t.Row, t => t.Col)
        .ToFrozenDictionary(t => t.Key, t => t.Order().ToArray());

    foreach (var (row, cols) in rowGroup)
    {
        var startCol = cols[0];
        var endCol = cols[^1];

        for (var i = startCol; i <= endCol; i++)
        {
            greenTiles.Add(new(i,row));
            map[row][i] = true;
        }
    }
    map.Print();
    
    // Console.WriteLine("Filling space");
    // var (startCols, startRows,totalCols, totalRows) = GetBoundingBox(redTiles);
    // for (long row = startRows; row < totalRows; row++)
    // {
    //     Progress(row, totalRows);
    //     bool paintGreen = false;
    //     long paintGreenIndex = -1;
    //     for (long col = startCols; col < totalCols; col++)
    //     {
    //         if (map[row][col] && !map[row][col+1])
    //         {
    //             paintGreen= !paintGreen;
    //             paintGreenIndex = paintGreen ? col : -1;
    //             continue;
    //         }
    //
    //         if (paintGreen)
    //             map[row][col] = true;
    //     }
    //
    //     if (paintGreen && paintGreenIndex != -1)
    //     {
    //         for (long col = paintGreenIndex + 1; col < totalCols; col++)
    //             map[row][col] = false;
    //     }
    // }
    // map.Print();


    return [];
}

static void GenerateGreenTiles2(bool[][] map, Tile[] redTiles)
{
    var (startCols, startRows,totalCols, totalRows) = GetBoundingBox(redTiles);
    bool paintGreen;

    List<long> indexToPaint = new List<long>((int)Math.Max(totalRows-startRows, totalCols - startCols));

    Console.WriteLine("Paint Horizontal border");
    for (long row = startRows; row < totalRows; row++)
    {
        Progress(row, totalRows);
        paintGreen = false;
        indexToPaint.Clear();
        for (long col = startCols; col < totalCols; col++)
        {
            if (map[row][col])
            {
                if (paintGreen)
                {
                    indexToPaint.ForEach(t=> map[row][t] = true);
                    paintGreen = false;
                    indexToPaint.Clear();
                }
                else
                    paintGreen = true;
            }
            else if (paintGreen)
                indexToPaint.Add(col);
        }
    }
    

    Console.WriteLine("Paint vertical border");
    for (var col = startCols; col < totalCols; col++)
    {
        Progress(col, totalCols);

        paintGreen = false;
        indexToPaint.Clear();
        for (var row = startRows; row < totalRows; row++)
        {
            if (map[row][col])
            {
                if (paintGreen)
                {
                    indexToPaint.ForEach(t=> map[t][col] = true);
                    paintGreen = false;
                    indexToPaint.Clear();
                }
                else
                    paintGreen = true;
            }
            else if (paintGreen)
                indexToPaint.Add(row);
        }
    }
    
    
    Console.WriteLine("Paint Inside border 1");
    for (var row = startRows; row < totalRows; row++)
    {
        Progress(row, totalRows);
        paintGreen = false;
        indexToPaint.Clear();
        for (var col = startCols; col < totalCols; col++)
        {
            if (map[row][col])
            {
                if (paintGreen)
                {
                    indexToPaint.ForEach(t=> map[row][t] = true);
                    paintGreen = false;
                    indexToPaint.Clear();
                }
                else
                    paintGreen = true;
            }
            else if (paintGreen)
                indexToPaint.Add(col);
        }
    }
    
    Console.WriteLine("Paint Inside border 2");
    for (var col = startCols; col < totalCols; col++)
    {
        Progress(col, totalCols);
        paintGreen = false;
        indexToPaint.Clear();
        for (var row = startRows; row < totalRows; row++)
        {
            if (map[row][col])
            {
                if (paintGreen)
                {
                    indexToPaint.ForEach(t=> map[t][col] = true);
                    paintGreen = false;
                    indexToPaint.Clear();
                }
                else
                    paintGreen = true;
                
            }
            else if (paintGreen)
                indexToPaint.Add(row);
        }
    }
    
}



static (long startCols, long startRows,long TotalCols, long TotalRows) GetBoundingBox(Tile[] redTiles)
{
    return (
        Math.Min(0,redTiles.Select(t => t.Col).Min() - 1),
        Math.Min(0,redTiles.Select(t => t.Row).Min() - 1),
        redTiles.Select(t => t.Col).Max() + 1 + 2,
        redTiles.Select(t => t.Row).Max() + 1 + 2
    );
}


static void Progress(long current, long max) 
{
    if (current % 100 != 0)
        return;
        
    var(left, top) = Console.GetCursorPosition();
    Console.Write($"{current}/{max}                                                                 ");
    Console.SetCursorPosition(left, top);
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

            //Console.WriteLine($"{startCol},{startRow} and {endCol},{endRow} make {area}");

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

public static class MapExtensions {
    extension(bool[][] map)
    {
        public void Print()
        {
            Console.WriteLine();
            Console.WriteLine();
            
            int rows = map.Length;
            int columns = map[0].Length;
            
            // Paint Horizontal border
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Console.Write(map[row][col] switch
                    {
                        true => 'X',
                        false => '.'
                    });
                }
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine();
        }



        
        public void SaveToFile(string file)
        {
            int rows = map.Length;
            int columns = map[0].Length;

            using var writer = File.OpenWrite(file);
            
            // Paint Horizontal border
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    writer.WriteByte(map[row][col] switch
                    {
                        true => (byte)'X',
                        false => (byte)'.'
                    });
                }
                
                writer.WriteByte((byte)'\n');
                writer.Flush();
            }
        }
    }
}