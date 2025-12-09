using System.Collections.Frozen;
using System.Numerics;
using System.Text;

var input = File.ReadLines("input.txt")
    .Select(t=> t.Split(',',StringSplitOptions.TrimEntries| StringSplitOptions.RemoveEmptyEntries))
    .Select(t=> new Tile(Col: long.Parse(t[0]), Row: long.Parse(t[1])))
    .ToArray();

// 37401302 to low
// 40000000
// 50000000

var result = Part2(input);


Console.WriteLine(result);


static long Part2(Tile[] input)
{

    Console.WriteLine("Generating map from red tiles");
    var map = GenerateMap(input);
    Console.WriteLine("adding green tiles");
    GenerateGreenTiles(map,input);
    
    //map.SaveToFile($"./{DateTime.Now:HH_m_s}.map");
    //map.Print();



    long maxArea = -1;

    Console.WriteLine("Calculating area");
    long total = Enumerable.Sequence(input.Length, 1, -1).Sum();
    long count = 0;
    for (int i = 0; i < input.Length; i++)
    {
        var (startCol, startRow) = input[i];
        for (int j = i+1; j < input.Length; j++)
        {
            Progress(count++, total);
            var (endCol, endRow) = input[j];
        
            long area = Math.Abs(1 + startCol - endCol) * Math.Abs(1+startRow - endRow);


            if (area > maxArea && SquareHasOnlyValidTiles(map, input[i], input[j]))
            {
                Console.WriteLine($"{startCol},{startRow} and {endCol},{endRow} make {area}");
                maxArea = area;
            }
        }
    }
    
    Console.WriteLine("finito");

    return maxArea;

}

static bool SquareHasOnlyValidTiles(bool[][] map, Tile start, Tile stop)
{
    var startCol = Math.Min(start.Col, stop.Col);
    var startRow = Math.Min(start.Row, stop.Row);
    var endCol = Math.Max(start.Col, stop.Col);
    var endRow = Math.Max(start.Row, stop.Row);

    for (long row = startRow; row <= endRow; row++)
    {
        for (long col = startCol;  col <= endCol; col++)
        {
            if (!map[row][col])
                return false;
        }
    }

    return true;
}

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

static void GenerateGreenTiles(bool[][] map, Tile[] redTiles)
{
    Console.WriteLine("Defining vertical edges");
    var columnGroup = redTiles
        .GroupBy(t => t.Col, t => t.Row)
        .ToFrozenDictionary(t => t.Key, t => t.Order().ToArray());

    foreach (var (col, rows) in columnGroup)
    {
        var startRow = rows[0];
        var endRow = rows[^1];

        for (var i = startRow; i <= endRow; i++) 
            map[i][col] = true;
    }
    
    //map.Print();
    
    Console.WriteLine("Defining horizontal edges");
    var rowGroup = redTiles.
        GroupBy(t => t.Row, t => t.Col)
        .ToFrozenDictionary(t => t.Key, t => t.Order().ToArray());

    foreach (var (row, cols) in rowGroup)
    {
        var startCol = cols[0];
        var endCol = cols[^1];

        for (var i = startCol; i <= endCol; i++) 
            map[row][i] = true;
    }
    //map.Print();
    
    Console.WriteLine("Filling space");
    var (startCols, startRows,totalCols, totalRows) = GetBoundingBox(redTiles);
    for (long row = startRows; row < totalRows; row++)
    {
        Progress(row, totalRows);
        bool paintGreen = false;
        long paintGreenIndex = -1;
        for (long col = startCols; col < totalCols; col++)
        {
            if (map[row][col] && !map[row][col+1])
            {
                paintGreen= !paintGreen;
                paintGreenIndex = paintGreen ? col : -1;
                continue;
            }

            if (paintGreen)
                map[row][col] = true;
        }

        if (paintGreen && paintGreenIndex != -1)
        {
            for (long col = paintGreenIndex + 1; col < totalCols; col++)
                map[row][col] = false;
        }
    }
    //map.Print();


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


record Tile(long Col, long Row);

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