
using AdventOfCode2025.Day12;

Dictionary<int, bool[][]> shapeMap = [];
List<Region> regions = [];

var lines = File.ReadAllLines("input.txt");
var i = 0;
while (i < lines.Length)
{
    if (i < 30)
    {
        var shapeIndex = lines[i++][0] - '0';
        var map = new bool[3][];
        for (int j = 0; j < 3; j++)
        {
            map[j] = lines[i++].Select(t => t == '#').ToArray();
        }

        shapeMap[shapeIndex] = map;
        i++;
        continue;
    }

    var regionPresentsArray = lines[i++].SplitTrim(':');
    var regionSize = regionPresentsArray[0].SplitTrim('x');
    var requiredPresentsInRegion = regionPresentsArray[1]
        .SplitTrim(' ').Select(int.Parse).ToArray();

    regions.Add(new(int.Parse(regionSize[0]), int.Parse(regionSize[1]), requiredPresentsInRegion));
}

Console.WriteLine();



int count = 0;
foreach (var region in regions)
{
    if ((region.Heigh * region.Width) - (9 * region.Presents.Sum() )>= 0)
        count++;
}

Console.WriteLine(count);


// 1000 too high
// 536 too low


































record Region(int Width, int Heigh, int[] Presents);