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
    
    var nodes = input;
    Dictionary<string, long> visitedNodes = new(nodes.Count);

    FindPathsV2(start, end, false,false,visitedNodes, nodes);

    return visitedNodes[start];
}


static (bool visitedFft, bool visitedDac) FindPathsV2(string node, string destination,bool visitedFft, bool visitedDac, Dictionary<string, long> visitedNodes, Dictionary<string, string[]> nodeMap)
{
    if (node == destination)
    {
        if (visitedDac && visitedFft)
            visitedNodes[node] = 1;
        
        return (visitedFft, visitedDac);
    } 

    if (visitedNodes.ContainsKey(node))
        return (visitedFft, visitedDac);

    visitedDac |= node == "dac";
    visitedFft |= node == "fft";
    
    long paths = 0;
    foreach (var nextNode in nodeMap[node])
    {
        var (nextVisitedFft, nextVisitedDac) =FindPathsV2(nextNode, destination, visitedFft, visitedDac, visitedNodes, nodeMap);
        if (nextVisitedFft && nextVisitedDac)
            paths += visitedNodes.GetValueOrDefault(nextNode,0);
    }
    if (paths != 0)
        visitedNodes[node] = paths;
    
    return (visitedFft, visitedDac);

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






























    
    