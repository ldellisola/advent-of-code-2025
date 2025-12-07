
var input = File.ReadAllLines("input.txt");

var result = Day2(input);


Console.WriteLine(result);
return;

static long Day2(string[] lines)
{
    var charMap = lines.Select(t => t.ToArray()).ToArray();

    int rows = charMap.Length;
    int columns = charMap[0].Length;

    long result = 0;

    List<long> numberList = [];
    for (int col = columns-1; col >=0 ; col--)
    {
        
        long number = 0;
        for (int row = 0; row < rows-1; row++)
        {
            var character = charMap[row][col];
            if (!char.IsNumber(character))
                continue;

            number = number * 10 + character - '0';
        }
        
        numberList.Add(number);

        switch (charMap[^1][col])
        {
            case ' ': break;
            case '*':
                Console.WriteLine(string.Join(" * ", numberList));
                result += numberList.Aggregate(1L, (acc, t) => acc * t);
                numberList.Clear();
                col--;
                break;
            case '+':
                Console.WriteLine(string.Join(" + ", numberList));
                result += numberList.Sum();
                numberList.Clear();
                col--;
                break;
        }
        
    }

    return result;
}
/*
123 328  51 64 
 45 64  387 23 
  6 98  215 314
*   +   *   +  
 */
static long Day1(string[] lines)
{
    var input = lines
        .Select(t=> t.Split(' ', StringSplitOptions.RemoveEmptyEntries  | StringSplitOptions.TrimEntries))
        .ToArray();
    
    var columns = input.Length -1;

    MathProblem[] mathProblems = new MathProblem[input[0].Length];

    for (int i = 0; i < mathProblems.Length; i++)
    {
        var data = new long[columns];
        for (int j = 0; j < data.Length; j++)
        {
            data[j] = long.Parse(input[j][i]);
        }

        mathProblems[i] = new MathProblem(data, input[^1][i][0]);
    }


    return mathProblems.Sum(t => t.Calculate());
}


record MathProblem(long[] Data, char Operator)
{
    public long Calculate()
    {
        return Operator switch
        {
            '+' => Data.Sum(),
            '*' => Data.Aggregate(1L, static (acc, next) => acc * next),
            _ => throw new Exception()
        };
    }
};