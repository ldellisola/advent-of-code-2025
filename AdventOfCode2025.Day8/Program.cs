
var positions = File.ReadLines("input.txt")
    .Select(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Select(t => new Position(long.Parse(t[0]), long.Parse(t[1]), long.Parse(t[2])))
    .ToArray();

List<(double Distance, HashSet<Position> Boxes)> a = new(positions.Length * positions.Length);

for (int i = 0; i < positions.Length; i++)
{
    var currentPos = positions[i];
    for (int j = i + 1; j < positions.Length; j++)
    {
        var nextPos = positions[j];
        
        a.Add((currentPos.DistanceTo(nextPos), [currentPos,nextPos]));
    }
}

const int boxesToJoin = 1000;

var circuits = a
    .OrderBy(t => t.Distance)
    .Take(boxesToJoin)
    .Select(t=> t.Boxes)
    .ToList();


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

Console.WriteLine();
Console.WriteLine($"Score: {score}");
return;


static bool CanConnectCircuits(List<HashSet<Position>> circuits)
{
    foreach (var circuit in circuits)
    {
        if (circuits.Where(t => t != circuit).Any(t => circuit.Overlaps(t)))
            return true;
    }

    return false;
}

record Position(long X, long Y, long Z)
{
    public double DistanceTo(Position other)
    {
        return Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y) + (Z - other.Z) * (Z - other.Z));
    }
}