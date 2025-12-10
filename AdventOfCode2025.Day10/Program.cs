using System.Collections;
using System.Text;
using Microsoft.Z3;


var machines = File.ReadLines("input.txt")
    .Select(Machine.Parse)
    .ToArray();


long count = Part2(machines);
Console.WriteLine(count);

return;


static long Part2(Machine[] machines)
{
    long count = 0;

    foreach (var machine in machines)
    {
        Console.WriteLine(machine);

        var buttonCombinations = machine.ButtonCombinations
            .Select(t =>
                Enumerable.Sequence(0, machine.Lights.Length - 1, 1).Select(i => (double)(t.Contains(i) ? 1 : 0)))
            .SelectMany(t => t)
            .ToArray();


        var ctx = new Context();

        var opt = ctx.MkOptimize();

        var buttonVars = new IntExpr[machine.ButtonCombinations.Length];

        for (var b = 0; b < machine.ButtonCombinations.Length; b++)
        {
            // set all variables >= 0;
            buttonVars[b] = ctx.MkIntConst($"b_{b}");
            opt.Add(ctx.MkGe(buttonVars[b], ctx.MkInt(0)));
        }

        for (var j = 0; j < machine.Jolteage.Length; j++)
        {
            var terms = new List<ArithExpr>();
            for (var b = 0; b < machine.ButtonCombinations.Length; b++)
            {
                // convert button presses into terms. 
                if (machine.ButtonCombinations[b].Contains(j))
                    terms.Add(buttonVars[b]);
            }

            // convert terms into math formula b0 + b2
            var se = ctx.MkAdd([..terms]);

            // pick up expected joltage
            var te = ctx.MkInt(machine.Jolteage[j]);

            // b0 + b2 = 4
            opt.Add(ctx.MkEq(se, te));
        }

        opt.MkMinimize(ctx.MkAdd([..buttonVars]));

        var status = opt.Check();

        count += Enumerable.Sequence(0, machine.ButtonCombinations.Length - 1, 1)
            .Sum(t => ((IntNum)opt.Model.Eval(buttonVars[t])).Int);
    }

    return count;
}

static long Part1(Machine[] machines)
{
    long count = 0;
    foreach (var machine in machines)
    {
        Console.WriteLine(machine);

        var rootNode = Part1_MakeGraph(machine);
        count += Part1_CalculateShortestPath(machine, rootNode);
    }

    return count;
}

static long Part1_CalculateShortestPath(Machine machine, Part1_Node root)
{
    var queue = new Queue<(int distance, Part1_Node node)>([(0, root)]);

    while (queue.TryDequeue(out var entry))
    {
        var (distance, node) = entry;
        if (node.Lights.Equivalent(machine.Lights))
        {
            Console.WriteLine(distance);
            return distance;
        }

        foreach (var (_, nextNode) in node.Connections)
        {
            queue.Enqueue((distance + 1, nextNode));
        }
    }

    throw new Exception("fuck");
}

static Part1_Node Part1_MakeGraph(Machine machine)
{
    var buttonCombinations = machine.ButtonCombinations
        .Select(t => Enumerable.Sequence(0, machine.Lights.Length - 1, 1).Select(t.Contains))
        .Select(t => new BitArray(t.ToArray()))
        .ToArray();


    Console.WriteLine("Making graph...");
    Dictionary<string, Part1_Node> nodeSet = [];
    var rootNode = new Part1_Node(new BitArray(machine.Lights.Length));
    nodeSet.Add(rootNode.Lights.AsString(), rootNode);

    var queue = new Queue<Part1_Node>([rootNode]);

    while (queue.TryDequeue(out var node))
    {
        for (var i = 0; i < buttonCombinations.Length; i++)
        {
            var nextNode = node.ApplyButton(i, buttonCombinations[i], nodeSet);
            if (nodeSet.TryAdd(nextNode.Lights.AsString(), nextNode))
            {
                queue.Enqueue(nextNode);
            }
        }
    }

    Console.WriteLine("Graph built!");
    return rootNode;
}


static class Extensions
{
    extension(BitArray array)
    {
        public string AsString()
        {
            var bld = new StringBuilder(array.Length);

            for (int i = 0; i < array.Length; i++)
            {
                bld.Append(array[i] switch
                {
                    true => '#',
                    false => '.'
                });
            }


            return bld.ToString();
        }

        public bool Equivalent(BitArray other)
        {
            if (ReferenceEquals(array, other))
                return true;

            if (other.Length != array.Length)
                return false;

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != other[i])
                    return false;
            }

            return true;
        }
    }
}


record Part1_Node(BitArray Lights)
{
    public Dictionary<int, Part1_Node> Connections { get; } = [];

    public Part1_Node ApplyButton(int buttonIndex, BitArray buttons, Dictionary<string, Part1_Node> nodeSet)
    {
        if (Connections.TryGetValue(buttonIndex, out var nextNode))
            return nextNode;

        var next = new BitArray(Lights).Xor(buttons);

        if (!nodeSet.TryGetValue(next.AsString(), out nextNode))
            nextNode = new Part1_Node(next);

        Connections.TryAdd(buttonIndex, nextNode);
        return nextNode;
    }

    public virtual bool Equals(Part1_Node? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;

        return !Lights.Xor(other.Lights).HasAnySet();
    }

    public override int GetHashCode()
    {
        return Lights.GetHashCode();
    }

    public override string ToString()
    {
        return $"{Lights.AsString()} Connections: {Connections.Count}";
    }
}


record Machine(BitArray Lights, HashSet<int>[] ButtonCombinations, int[] Jolteage)
{
    public override string ToString()
    {
        var bld = new StringBuilder(Lights.AsString())
            .Append(' ');


        foreach (var buttonCombination in ButtonCombinations)
        {
            bld.Append('(')
                .AppendJoin(',', buttonCombination)
                .Append(") ");
        }

        bld.Append('{')
            .AppendJoin(' ',Jolteage)
            .Append('}');

        return bld.ToString();
    }


    public static Machine Parse(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lights = new BitArray(parts[0].Skip(1).SkipLast(1).Select(t => t == '#').ToArray());

        var buttonCombinations = parts
            .Skip(1)
            .SkipLast(1)
            .Select(t =>
                t.Trim(')', '(').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(t => t.Select(int.Parse).ToHashSet())
            .ToArray();

        var joltage = parts[^1]
            .Trim('{', '}')
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToArray();
        
        return new Machine(lights, buttonCombinations, joltage);
    }
}