namespace AdventOfCode2025.Day12;

public static class Extensions
{
    extension(string s)
    {
        public string[] SplitTrim(char c)
        {
            return s.Split(c, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
        public string[] SplitTrim(string c)
        {
            return s.Split(c, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
    }
}