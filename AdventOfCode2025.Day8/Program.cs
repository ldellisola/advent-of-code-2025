
using System.Collections.Immutable;

var positions = File.ReadLines("input.txt")
    .Select(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Select(t => new Position(long.Parse(t[0]), long.Parse(t[1]), long.Parse(t[2])))
    .ToArray();


var score =Part2(positions);

Console.WriteLine();
Console.WriteLine($"Score: {score}");
return;


static ImmutableArray<(Position From, Position To)> Part2_GetConnectionsSortedByDistance(Position[] positions)
{
    List<(double Distance, Position From, Position To)> connectedBoxes = new(positions.Length * positions.Length);

    for (int i = 0; i < positions.Length; i++)
    {
        var currentPos = positions[i];
        for (int j = i + 1; j < positions.Length; j++)
        {
            var nextPos = positions[j];
        
            connectedBoxes.Add((currentPos.DistanceTo(nextPos), currentPos,nextPos));
        }
    }
    
    return  [
        ..connectedBoxes
            .OrderBy(t => t.Distance)
            .Select(t=> (t.From, t.To))
    ];
}


static bool Part2_CircuitsAreDisconnected(Dictionary<Position, int> circuits)
{
    var circuitIndex = circuits.Values.ElementAt(0);
    for (int i = 1; i < circuits.Values.Count; i++)
    {
        if (circuitIndex != circuits.Values.ElementAt(i))
            return true;
    }

    return false;
}

static long Part2(Position[] positions)
{
    HashSet<Position>[] circuits = positions.Select<Position,HashSet<Position>>(t => [t]).ToArray();
    Dictionary<Position, int> circuitLookup = circuits.Index().ToDictionary(t => t.Item.First(), t => t.Index);
    
    var sortedConnections = Part2_GetConnectionsSortedByDistance(positions);
    
    int sortedConnectionIndex = -1;

    while (Part2_CircuitsAreDisconnected(circuitLookup))
    {
        sortedConnectionIndex++;
        var (from, to) = sortedConnections[sortedConnectionIndex];
        
        var fromCircuitIndex =  circuitLookup[from];
        var toCircuitIndex = circuitLookup[to];
        
        if (fromCircuitIndex == toCircuitIndex)
            continue;
        
        var fromCircuit = circuits[fromCircuitIndex];
        var toCircuit = circuits[toCircuitIndex];
        
        fromCircuit.UnionWith(toCircuit);
        foreach (var position in toCircuit)
            circuitLookup[position] = fromCircuitIndex;
        
        toCircuit.Clear();
    }

    
    var lastConnection = sortedConnections[sortedConnectionIndex];
    long score = lastConnection.From.X * lastConnection.To.X;
    return score;
}


static bool CanConnectCircuits(List<HashSet<Position>> circuits)
{
    foreach (var circuit in circuits)
    {
        if (circuits.Where(t => t != circuit).Any(t => circuit.Overlaps(t)))
            return true;
    }

    return false;
}



static List<HashSet<Position>> Day1_GetClosestNConnections(Position[] positions, int n)
{
    List<(double Distance, HashSet<Position> Boxes)> connectedBoxes = new(positions.Length * positions.Length);

    for (int i = 0; i < positions.Length; i++)
    {
        var currentPos = positions[i];
        for (int j = i + 1; j < positions.Length; j++)
        {
            var nextPos = positions[j];
        
            connectedBoxes.Add((currentPos.DistanceTo(nextPos), [currentPos,nextPos]));
        }
    }


    return  connectedBoxes
        .OrderBy(t => t.Distance)
        .Take(n)
        .Select(t=> t.Boxes)
        .ToList();

}

static long Day1(Position[] positions)
{
    
    var circuits = Day1_GetClosestNConnections(positions, 1000);
    
    while (CanConnectCircuits(circuits))
    {
        HashSet<int> mergedIndexes = [];
        var newCircuits = new List<HashSet<Position>>();
        for (int i = 0; i < circuits.Count; i++)
        {
            if (mergedIndexes.Contains(i))
                continue;
            var newCircuit = new HashSet<Position>(circuits[i]);
            for (int j = i+1; j < circuits.Count; j++)
            {
                if (circuits[i].Overlaps(circuits[j]))
                {
                    mergedIndexes.Add(j);
                    newCircuit.UnionWith(circuits[j]);
                }
            }
        
            newCircuits.Add(newCircuit);
        }

        circuits = newCircuits;
    }


    int totalBoxes = circuits.SelectMany(t => t).Distinct().Count();
    Console.WriteLine($"Total boxes: {totalBoxes}");
    foreach (var circuit in circuits)
    {
        Console.WriteLine($"{circuit.Count} boxes in circuit");
    }

    long score = circuits
        .Select(t => t.Count)
        .OrderDescending()
        .Take(3)
        .Aggregate(1L, (acc, i) => acc * i);

    return score;
}



record Position(long X, long Y, long Z)
{
    public double DistanceTo(Position other)
    {
        return Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y) + (Z - other.Z) * (Z - other.Z));
    }
}