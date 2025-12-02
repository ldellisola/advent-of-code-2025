


var file = File.ReadLinesAsync("input.txt");


var result = await Part2(file);
Console.WriteLine(result);




async Task<int> Part2(IAsyncEnumerable<string> lines)
{
    int lockPosition = 50;
    int zeroes = 0;


    var rotationLock = new RotationLock(lockPosition, 100);
    
    Console.WriteLine($"The dial starts pointing at {lockPosition}");
    await foreach(var line in lines)
    {
        var movement = int.Parse(line[1..]);

        if (line[0] == 'R')
        {
            rotationLock.Right(movement);
        }
        else
        {
            rotationLock.Left(movement);
        }
    }

    return rotationLock.Score;
}


async Task<int> Part1(IAsyncEnumerable<string> lines)
{
    int lockPosition = 50;
    int zeroes = 0;

    await foreach(var line in lines)
    {
        var direction = line[0] switch
        {
            'R' => 1,
            'L' => -1,
            _ => throw new InvalidDataException()
        };

        var movement = int.Parse(line[1..]);
    
        lockPosition = (lockPosition + direction * movement) % 100;
        if (lockPosition == 0)
            zeroes++;
    }

    return zeroes;
}

class RotationLock(int start, int size)
{
    private int _position = start;
    public int Score { get; private set; }
    
    public void Left(int move)
    {
        Console.Write($"The dial is rotated L{move} to point at ");
        Spin(move,-1);
    }

    private void Spin(int move, int direction)
    {
        int scoreBefore = Score;
        while (move > 0)
        {
            _position = (( direction +_position) % size + size) % size;
            move--;
            if (_position == 0 && move != 0)
                Score++;
        }
        
        Console.WriteLine(scoreBefore != Score
            ? $"{_position}; during this rotation, it points at zero {Score - scoreBefore} times."
            : $"{_position}.");

        if (_position == 0)
            Score++;
    }



    public void Right(int move)
    {
        int scoreBefore = Score;
        Console.Write($"The dial is rotated R{move} to point at ");
        Spin(move,+1);
    }
}