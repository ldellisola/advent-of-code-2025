using System.Runtime.InteropServices.ComTypes;

var input = File.ReadAllText("input.txt")
    .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);


var result = Day2(input);

Console.WriteLine(result);
return;





static long Day2(string[] input)
{
    var vegetableDb = input[0]
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(t => t.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Select(t => (Min: long.Parse(t[0]), Max: long.Parse(t[1])))
        .SelectMany<(long Min, long Max),Edge>(t=> [ new Edge(t.Min, EdgeType.Min), new Edge(t.Max, EdgeType.Max)])
        .OrderBy(t=> t.Number)
        .ToArray();

    long total = 0;
    long? min = null;
    long? max = null;
    
    for (int i = 0; i < vegetableDb.Length; i++)
    {
        switch ((min, max))
        {
            case (null,_): 
                min = vegetableDb[i].Number;
                if (vegetableDb[i].Type is not EdgeType.Min) throw new Exception();
                continue;
            case (_, null):
                if (vegetableDb[i].Type is EdgeType.Min)
                    continue;
                if (vegetableDb[i].Type is EdgeType.Max)
                    max = vegetableDb[i].Number;
                break;
            case (_,_):
                if (vegetableDb[i].Type is EdgeType.Max)
                {
                    max = vegetableDb[i].Number;
                    continue;
                }

                if (vegetableDb[i].Type is EdgeType.Min)
                {
                    total += 1+ max.Value - min.Value;
                    min = vegetableDb[i].Number;
                    max = null;
                }

                break;
        }
        
        
    }

    return total;

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

record Edge(long Number, EdgeType Type);

enum EdgeType
{
    Min,
    Max
}
