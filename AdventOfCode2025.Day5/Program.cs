
var input = File.ReadAllText("input.txt")
    .Split("\r\n\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);


var result = Day2(input);

Console.WriteLine(result);
return;





static long Day2(string[] input)
{
    var vegetableDb = input[0]
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(t => t.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Select(t => (Min: long.Parse(t[0]), Max: long.Parse(t[1])))
        .OrderBy(t=> t.Min)
        .ToArray();
    
    do
    {
        vegetableDb = CalculateNewRanges(vegetableDb);
        if (!HasIntersections(vegetableDb))
            break;
    }
    while(true);


    return vegetableDb.Select(t=> 1+ t.Max - t.Min).Sum();

}


static bool HasIntersections((long Min, long Max)[] ranges)
{
    return ranges
        .SelectMany(t => new[] { (Num: t.Min, type: "min"), (Num: t.Max, type: "max") })
        .OrderBy(t => t.Num)
        .Chunk(2)
        .Any(t => t[0].type == t[1].type);
}


static (long Min, long Max)[] CalculateNewRanges((long Min, long Max)[] ranges)
{
    var newRanges = new HashSet<(long Min, long Max)>();
    var alreadyIntersected = new HashSet<(long Min, long Max)>();
    
    for (int i = 0; i < ranges.Length; i++)
    {
        var currentRange = ranges[i];
        if (alreadyIntersected.Contains(currentRange))
            continue;
        bool hasIntersected = false;
        for (int j = 0 ; j < ranges.Length; j++)
        {
            bool intersects = (currentRange.Min <= ranges[j].Min && ranges[j].Min <= currentRange.Max) || (currentRange.Min <= ranges[j].Max && ranges[j].Max <= currentRange.Max);

            if (intersects && currentRange != ranges[j])
            {
                alreadyIntersected.Add(ranges[j]);
                hasIntersected = true;
                newRanges.Add((Math.Min(currentRange.Min, ranges[j].Min), Math.Max(currentRange.Max, ranges[j].Max)));
                break;
            }
        }
        if (!hasIntersected)
            newRanges.Add(currentRange);
    }

    return newRanges.OrderBy(t=> t.Min).ToArray();
}

static long Day1(string[] input)
{
    var freshVegetablesInput = input[0].Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(t => t.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Select(t => (Min: long.Parse(t[0]), Max: long.Parse(t[1])))
        .ToArray();

    var neededVegetablesInput = input[1]
        .Split("\n",StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(long.Parse);

    long result = 0;
    foreach (var neededVegetable in neededVegetablesInput)
    {
        if (IsVegetableFresh(freshVegetablesInput, neededVegetable))
            result++;
    }

    return result;
}


static bool IsVegetableFresh((long Min, long Max)[] freshVegetableDb, long vegetable)
{
    foreach ( var (min, max)  in freshVegetableDb)
    {
        if (min <= vegetable && vegetable <= max)
            return true;
    }

    return false;
}