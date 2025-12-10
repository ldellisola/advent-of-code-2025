
using System.Text;

var machines = File.ReadLines("input.txt")
    .Select(Machine.Parse)
    .ToArray();
var comparer = new SetEqualityComparer<int>();
long count = 0;
foreach (var machine in machines)
{
    Console.WriteLine(machine);

    HashSet<PressRecord> memory = [];
    
    while (!machine.AreAllLightsOn())
    {
        count++;
        
        var offLights = machine.GetLightsInState(false);

        // Do I have a perfect match?
        var perfectMatch = machine.ButtonCombinations.IndexOf(offLights,comparer);

        if (perfectMatch is not -1 && !memory.Contains(new(machine.LightsState(), perfectMatch)))
        {
            memory.Add(machine.PressButton(perfectMatch));
            continue;
        }
        
        // Hope I get a decent match
        int bestGuess = -1;
        for (int i = 0; i < machine.ButtonCombinations.Length; i++)
        {
            if ( !memory.Contains(new(machine.LightsState(), i) ) && machine.ButtonCombinations[i].IsProperSubsetOf(offLights))
            {
                bestGuess = i;
                break;
            }
        }
        
        if (bestGuess is not -1)
        {
            memory.Add(machine.PressButton(bestGuess));
            continue;
        }

        
        // Hope I get a decent match
        int secondBestGuess = -1;
        for (int i = 0; i < machine.ButtonCombinations.Length; i++)
        {
            if (!memory.Contains(new(machine.LightsState(), i) ) && machine.ButtonCombinations[i].IsProperSupersetOf(offLights))
            {
                secondBestGuess = i;
                break;
            }
        }
        
        if (secondBestGuess is not -1)
        {
            memory.Add(machine.PressButton(secondBestGuess));
            continue;
        }


    }
}

Console.WriteLine(count);


return;

class SetEqualityComparer<T> : IEqualityComparer<HashSet<T>>
{
    public bool Equals(HashSet<T>? x, HashSet<T>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.SetEquals(y);
    }

    public int GetHashCode(HashSet<T> obj)
    {
        return HashCode.Combine(obj.Comparer, obj.Count, obj.Capacity);
    }
}

record PressRecord(string LightsBefore, int ButtonsPressed);

record Machine(bool[] Lights, HashSet<int>[] ButtonCombinations, int[] Jolteage)
{
    public HashSet<int> GetLightsInState(bool state)
    {
        HashSet<int> set = [];
        for (var i = 0; i < Lights.Length; i++)
        {
            if (Lights[i] == state)
                set.Add(i);
        }
        return set;
    }
    

    public PressRecord PressButton(int index)
    {
        var combination = ButtonCombinations[index];
        
        Console.WriteLine($"Pressed: {string.Join(',', combination)}");

        var record = new PressRecord(LightsState(), index);
        
        foreach (var i in combination) 
            Lights[i] = !Lights[i];
        
        Console.WriteLine(LightsState());
        return record;
    }

    public bool AreAllLightsOn()
    {
        for (int i = 0; i < Lights.Length; i++)
        {
            if (!Lights[i])
                return false;
        }
        return true;
    }

    public string LightsState()
    {
        var bld = new StringBuilder();
        bld.Append('[')
            .Append(Lights.Select(t => t ? '#' : '.').ToArray())
            .Append(']');
        return bld.ToString();
    }

    public override string ToString()
    {
        var bld = new StringBuilder();
        bld.Append('[')
            .Append(Lights.Select(t => t ? '#' : '.').ToArray())
            .Append(']');
        bld.Append(' ');

        foreach (var buttonCombination in ButtonCombinations)
        {
            bld.Append('(')
                .AppendJoin(',', buttonCombination)
                .Append(") ");
        }

        bld.Append('{')
            .AppendJoin(',', Jolteage)
            .Append('}');

        return bld.ToString();
    }

    
    private string initialState = "";
    public static Machine Parse(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lights = parts[0].Skip(1).SkipLast(1).Select(t => t == '#').ToArray();

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


        return new Machine(lights, buttonCombinations, joltage)
        {
            initialState = line
        };

    }
}