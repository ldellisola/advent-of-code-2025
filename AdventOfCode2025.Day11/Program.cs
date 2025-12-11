using AdventOfCode2025.Day11;

var input = File.ReadLines("input.txt")
    .Select(t=> t.SplitTrim(':'))
    .ToDictionary(t=> t[0], t=> t[1].SplitTrim(' '));


long result = Part2(input);

Console.WriteLine(result);
return;


static long Part2(Dictionary<string, string[]> input)
{
    const string start = "svr";
    const string end = "out";
    Dictionary<(string, bool, bool), long> visitedNodes = [];

    return FindPathsV2Naive(start, end, false, false, visitedNodes, input);
}

static long FindPathsV2Naive(string node, string destination, bool visitedDac, bool visitedFft, Dictionary<(string, bool, bool), long> visitedNodes, Dictionary<string, string[]> nodeMap)
{
    if (node == destination)
        return visitedDac && visitedFft ? 1 : 0;

    if (visitedNodes.TryGetValue((node, visitedDac, visitedFft), out var value))
        return value;

    visitedDac |= node == "dac";
    visitedFft |= node == "fft";
    
    long paths = 0;
    foreach (var nextNode in nodeMap[node])
    {
        paths += FindPathsV2Naive(nextNode,destination,visitedDac, visitedFft, visitedNodes, nodeMap);
    }
    
    visitedNodes.TryAdd((node, visitedDac, visitedFft), paths);

    return paths;
}



static long Part1(Dictionary<string, string[]> input)
{
    const string start = "you";
    const string end = "out";

    var nodes = input;
    Dictionary<string, long> visitedNodes = new(nodes.Count);

    FindPathsV1(start, end, visitedNodes, nodes);

    return visitedNodes[start];
}

static void FindPathsV1(string node, string destination, Dictionary<string, long> visitedNodes, Dictionary<string, string[]> nodeMap)
{
    if (node == destination) 
        visitedNodes[node] = 1;

    if (visitedNodes.ContainsKey(node))
        return;
    
    long paths = 0;
    foreach (var nextNode in nodeMap[node])
    {
        FindPathsV1(nextNode,destination, visitedNodes, nodeMap);
        paths += visitedNodes[nextNode];
    }
    visitedNodes[node] = paths;
}

























    
    