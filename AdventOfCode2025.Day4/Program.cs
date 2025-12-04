var input = File.ReadAllLines("input.txt");


var map = input
    .Select<string,bool[]>(t=> [false,..t.Select(r=> r == '@'), false])
    .Prepend(new bool[input[0].Length + 2])
    .Append(new bool[input[0].Length + 2])
    .ToArray();


var result = Day2(map);

Console.WriteLine(result);
return;

static int Day2(bool[][] map)
{
    int movedRolls;
    int totalRollsMoved = 0;

    do
    {
        map = ParseAndMakeNewMap(map, out movedRolls);
        totalRollsMoved += movedRolls;

    } while (movedRolls != 0);

    return totalRollsMoved;
}


static bool[][] ParseAndMakeNewMap(bool[][] map, out int count)
{
    count = 0;
    bool[][] newMap = Enumerable.Range(0, map.Length).Select(t => new bool[map[0].Length]).ToArray();
    
    for (int y = 1; y < map.Length-1; y++)
    {
        for (int x = 1; x < map[y].Length-1; x++)
        {
            if (!map[y][x])
                continue;

            bool[] blockedPositions = [ 
                map[y-1][x-1] , map[y-1][x] , map[y-1][x+1] ,
                map[y][x-1]   ,     false   , map[y][x+1]   ,
                map[y+1][x-1]   , map[y+1][x] , map[y+1][x+1]
            ];

            if (blockedPositions.Count(t => t) < 4)
                count++;
            else
                newMap[y][x] = true;
        }
    }

    return newMap;
}


static int Day1(bool[][] map)
{
    int count = 0;
    for (int y = 1; y < map.Length-1; y++)
    {
        for (int x = 1; x < map[y].Length-1; x++)
        {
            if (!map[y][x])
                continue;

            bool[] blockedPositions = [ 
                map[y-1][x-1] , map[y-1][x] , map[y-1][x+1] ,
                map[y][x-1]   ,     false   , map[y][x+1]   ,
                map[y+1][x-1]   , map[y+1][x] , map[y+1][x+1]
            ];

            if (blockedPositions.Count(t => t) < 4)
                count++;
        }
    }

    return count;
}