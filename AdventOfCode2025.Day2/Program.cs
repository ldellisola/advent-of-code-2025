var data =
    File.ReadAllText("input.txt").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(t => t.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Select(t => (Min: long.Parse(t[0]), Max: long.Parse(t[1])))
        .ToArray();

var result = data
    .SelectMany(t => Enumerable.Sequence(t.Min, t.Max, 1).Select(r => r.ToString()))
    .AsParallel()
    .Where(Day2)
    .Sum(long.Parse);




Console.WriteLine(result);
return;

static bool Day1(string t)
{
    if (t.Length % 2 == 0)
        return false;
    var span = t.AsSpan();
    return span[..(span.Length / 2)].SequenceEqual(span[(span.Length / 2)..]);
}

static bool Day2(string t)
{
    var span = t.AsSpan();
    for (int subLength = span.Length / 2; subLength > 0; subLength--)
    {
        if (span.Length % subLength != 0) continue;

        bool match = true;

        var firstPart = span[..subLength];
        var start = subLength;

        while (match && start < span.Length)
        {
            match &= firstPart.SequenceEqual(span.Slice(start, subLength));
            start += subLength;
        }

        if (match) return match;
    }

    return false;
}