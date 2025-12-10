using System.Buffers;
using System.Collections;
using System.Net.Http.Headers;
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

        count += Enumerable.Sequence(0, machine.ButtonCombinations.Length, 1)
            .Sum(t => ((IntNum)opt.Model.Eval(buttonVars[t])).Int);

    }
    return count;

}



static long Part2_old(Machine[] machines)
    {
        long count = 0;
        foreach (var machine in machines)
        {
            Console.WriteLine(machine);


            // make this an iterator
            var rootNode = Part2_MakeGraph(machine);
            count += Part2_CalculateShortestPath(machine, rootNode);
        }

        return count;
    }

    static long Part2_CalculateShortestPath(Machine machine, IEnumerable<(int distance, Part2_Node node)> graph)
    {
        using var enumerator = graph.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var (distance, node) = enumerator.Current;
            Console.WriteLine($"Node {node} at distance {distance}");
            if (node.Meter.DistanceToZero() == 0)
            {
                Console.WriteLine(distance);
                return distance;
            }

            ArrayPool<int>.Shared.Return(node.Meter.Meter);
        }


        throw new Exception("fuck");
    }

    static IEnumerable<(int distance, Part2_Node node)> Part2_MakeGraph(Machine machine)
    {
        var buttonCombinations = machine.ButtonCombinations
            .Select(t => Enumerable.Sequence(0, machine.Lights.Length - 1, 1).Select(i => t.Contains(i) ? 1 : 0))
            .Select(t => t.ToArray())
            .ToArray();

        Console.WriteLine("Making graph...");
        // Dictionary<JoltageMeter, Part2_Node> nodeSet = [];
        HashSet<string> nodeSet = [];
        // HashSet<JoltageMeter> nodeSet = [];
        var rootNode = new Part2_Node(machine.Jolteage);
        // nodeSet.Add(rootNode.Meter, rootNode);
        nodeSet.Add(rootNode.Meter.ToString());

        var queue = new PriorityQueue<(int distance, Part2_Node node), int>();
        queue.Enqueue((0, rootNode), 0);
        // var queue = new  Queue<Part2_Node>([rootNode]);

        while (queue.TryDequeue(out var entry, out int _))
        {
            var (distanceToStart, node) = entry;

            for (var i = 0; i < buttonCombinations.Length; i++)
            {
                // var nextNode = node.ApplyButton(i,buttonCombinations[i], nodeSet);
                var nextNode = node.ApplyButton(i, buttonCombinations[i], null!);

                if (!nextNode.IsOutOfBounds(machine.Jolteage) && nodeSet.Add(nextNode.Meter.ToString()))
                {
                    queue.Enqueue((distanceToStart + 1, nextNode), nextNode.Meter.DistanceToZero());
                }
            }

            yield return entry;
        }

        // Console.WriteLine("Graph built!");
        // return rootNode;
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


record JoltageMeter(int[] Meter, int Length)
{
    public int this[int key] => Meter[key];

    public JoltageMeter Apply(int[] source)
    {
        var dest =  ArrayPool<int>.Shared.Rent(Meter.Length);
        for (int i = 0; i < Length; i++)
        {
            dest[i] = Meter[i] - source[i];
        }

        return this with { Meter = dest };
    }

    public override string ToString()
    {
        var bld = new StringBuilder();
        for (int i = 0; i < Length; i++)
        {
            bld.Append(this[i].ToString()).Append(' ');
        }

        return bld.ToString();
    }

    public virtual bool Equals(JoltageMeter? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;

        if (Meter.Length != other.Meter.Length)
            return false;

        for (int i = 0; i < Length; i++)
        {
            if (Meter[i] != other[i])
                return false;
        }

        return true;
    }
    
    public virtual bool Equals(int[] other)
    {
        if (Meter.Length != other.Length)
            return false;

        for (int i = 0; i < Length; i++)
        {
            if (Meter[i] != other[i])
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hashcode = new HashCode();
        for (int i = 0; i < this.Length; i++)
        {
            hashcode.Add(Meter[i]);
        }
        return hashcode.ToHashCode();
    }

    public int DistanceToZero()
    {
        int distance = 0;
        for (int i = 0; i < Length; i++)
        {
            distance += Meter[i];
        }

        return distance;
    }
}

record Part2_Node(JoltageMeter Meter)
{

    // public Dictionary<int, Part2_Node> Connections { get; } = [];

    public Part2_Node ApplyButton(int buttonIndex, int[] buttons, Dictionary<JoltageMeter, Part2_Node> nodeSet)
    {
        // if (Connections.TryGetValue(buttonIndex, out var nextNode))
        //    return  nextNode;

        var next = Meter.Apply(buttons);

        // if (!nodeSet.TryGetValue(next, out nextNode)) 
           var nextNode = new Part2_Node(next);
        
        // Connections.TryAdd(buttonIndex, nextNode);
        return nextNode;
    }

    public bool IsOutOfBounds(JoltageMeter desiredPowerLevel)
    {
        for (int i = 0; i < desiredPowerLevel.Length; i++)
        {
            if (Meter[i] < 0)
                return true;
        }

        return false;
    }
    
    public virtual bool Equals(Part2_Node? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;

        return Meter.Equals(other.Meter);
    }

    public override int GetHashCode()
    {
        return Meter.GetHashCode();
    }

    public override string ToString()
    {
        return Meter.ToString();
    }
}

record Part1_Node(BitArray Lights)
{

    public Dictionary<int, Part1_Node> Connections { get; } = [];

    public Part1_Node ApplyButton(int buttonIndex, BitArray buttons, Dictionary<string, Part1_Node> nodeSet)
    {
        if (Connections.TryGetValue(buttonIndex, out var nextNode))
            return  nextNode;
        
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



record Machine(BitArray Lights, HashSet<int>[] ButtonCombinations, JoltageMeter Jolteage){

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
            .Append(Jolteage)
            .Append('}');

        return bld.ToString();
    }

    
    public static Machine Parse(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lights = new BitArray(parts[0].Skip(1).SkipLast(1).Select(t=> t == '#').ToArray());

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
        
        var rentedJoltage = ArrayPool<int>.Shared.Rent(joltage.Length);
        joltage.CopyTo(rentedJoltage);


        return new Machine(lights, buttonCombinations, new(rentedJoltage, joltage.Length));
    }
}

