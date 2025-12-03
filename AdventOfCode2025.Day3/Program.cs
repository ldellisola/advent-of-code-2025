

var input = File.ReadAllLines("input.txt");

var result = 
input
    .AsParallel()
    .Select(Day2)
    .Sum();

Console.WriteLine("-------------");
Console.WriteLine(result);
return;

static long Day2(string batteryBank)
{
    const int batteriesNeeded = 12;
    
    int[] indexes = new int[batteriesNeeded];
    
    for (int i = 0; i < batteriesNeeded; i++)
    {
        int previousIndex = i is 0 ? 0 : indexes[i - 1] + 1;
        indexes[i] = IndexOfMax(batteryBank, batteryBank.Length - batteriesNeeded + i+1, previousIndex);
    }

    var selectedBatteries = string.Join("", indexes.Select(t=> batteryBank[t]));
    // Console.WriteLine(selectedBatteries);
    return long.Parse(selectedBatteries);
}
static int Day1(string batteryBank)
{
    var firstBatteryIndex = IndexOfMax(batteryBank, batteryBank.Length - 1, 0);
    var secondBatteryIndex = IndexOfMax(batteryBank, batteryBank.Length, firstBatteryIndex + 1);

    return int.Parse($"{batteryBank[firstBatteryIndex]}{batteryBank[secondBatteryIndex]}");
}

static int IndexOfMax(ReadOnlySpan<char> array, int stopPosition, int startPosition = 0)
{
    int? highestIndex = null;

    for (int i = startPosition; i < stopPosition; i++)
    {
        highestIndex ??= i;
        
        if (array[i] > array[highestIndex.Value])
            highestIndex = i;

        if (array[i] == '9')
            return i;

    }

    return highestIndex!.Value;
}