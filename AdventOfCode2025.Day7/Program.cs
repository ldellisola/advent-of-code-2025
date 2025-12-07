var map = File.ReadLines("input.txt").Select(t=> t.ToCharArray()).ToArray();

var result = Day2(map);

Console.WriteLine(result);
return;


static long Day2(char[][] map)
{
    var sourceNode = GenerateDirectedGraph(map);
    FindAllPathsToDestinationBest(sourceNode);
    return sourceNode.Score;
}

static void FindAllPathsToDestinationBest(Node node)
{
    if (node.Score != 0)
        return;
    
    if (node.IsADestination())
    {
        node.Score = 1;
        return;
    }

    foreach (var child in node.Nodes)
    {
        FindAllPathsToDestinationBest(child);
    }

    node.Score = node.Nodes.Sum(t => t.Score);
}


static Node GenerateDirectedGraph(char[][] map)
{
    int rows = map.Length;
    int columns = map[0].Length;
    
    var source = new Node($"0:{map[0].IndexOf('S')}");

    Dictionary<string, Node> nodeLookup = [];
    nodeLookup.Add(source.Id,source);
    
    for (int row = 1; row < rows; row++)
    {
        for (int col = 0; col < columns; col++)
        {
            if (map[row - 1][col] is not ('S' or 's' or '|')) 
                continue;
            
            var parentNode = nodeLookup.GetValueOrDefault($"{row - 1}:{col}") ?? throw new Exception("Wrong node");
            
            switch ( map[row][col])
            {
                case '.':
                case '|':
                {
                    var childId = $"{row}:{col}";
                    var childNode = nodeLookup.GetValueOrDefault(childId, new Node(childId));
                    nodeLookup.TryAdd(childId, childNode);
                    parentNode.AddNextNode(childNode);
                    
                    map[row][col] = '|';
                    continue;
                }
                case '^':
                {
                    var childLeftId = $"{row}:{col - 1}";
                    var childLeftNode = nodeLookup.GetValueOrDefault(childLeftId, new Node(childLeftId));
                    nodeLookup.TryAdd(childLeftId, childLeftNode);
                    parentNode.AddNextNode(childLeftNode);

                    
                    var childRightId = $"{row}:{col + 1}";
                    var childRightNode = nodeLookup.GetValueOrDefault(childRightId, new Node(childRightId));
                    nodeLookup.TryAdd(childRightId, childRightNode);
                    parentNode.AddNextNode(childRightNode);
                    
                    map[row][col - 1] = map[row][col + 1] = '|';
                    continue;
                }
            }
        }
    }

    return source;
}



static long Day1(char[][] map)
{
    int rows = map.Length;
    int columns = map[0].Length;

    int splitCounters = 0;

    PrintMap(map);
    
    for (var row = 1; row < rows; row++)
    {
        for (var col = 0; col < columns; col++)
        {
            if (map[row - 1][col] is not ('S' or 's' or '|')) 
                continue;
                
            switch (map[row][col])
            {
                case '.':
                    map[row][col] = '|';
                    continue;
                case '^':
                    splitCounters++;
                    map[row][col - 1] = map[row][col + 1] = '|';
                    continue;
            }
        }
    }
    
    PrintMap(map);


    return splitCounters;
}

static void PrintMap(char[][] map)
{
    for (int i = 0; i < map.Length; i++)
    {
        for (int j = 0; j < map[i].Length; j++)
        {
            Console.Write(map[i][j]);
        }
        Console.WriteLine(); 
    }
    
    Console.WriteLine(); 
    Console.WriteLine(); 
    Console.WriteLine(); 
}

record Node(string Id) : IComparable<Node>
{
    public HashSet<Node> Nodes { get; } = [];

    public long Score { get; set; }

    public void AddNextNode(Node next) => Nodes.Add(next);

    public bool IsADestination() => Nodes.Count == 0;
    

    public virtual bool Equals(Node? other) => CompareTo(other) == 0;

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString() => $"{Id} -> {string.Join('-', Nodes.Select(t => t.Id))}";

    public int CompareTo(Node? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return string.Compare(Id, other.Id, StringComparison.Ordinal);
    }
}