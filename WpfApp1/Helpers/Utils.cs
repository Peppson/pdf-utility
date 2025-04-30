using System.Diagnostics;

namespace WpfApp1.Helpers;

public static class U
{
    private static readonly Stopwatch _timer = new Stopwatch();
    private static bool _isTimerRunning = false;

    public static T GetUserInput<T>(string message = "")
    {
        string typeName = typeof(T).Name.ToLower();
        Console.Write($"> [{typeName}] {message} ");

        while (true)
        {
            string? input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
            {
                Console.Write($"> Anything please... ");
                continue;
            }

            // Hack for reading float/double/decimals correctly
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(decimal))
            {
                input = input.Replace('.', ',');
                Console.WriteLine("YEP");
            }

            try
            {
                return ParseUserInput<T>(input, typeName);
            }
            catch
            {
                Console.Write($"> Please enter valid {typeName}. \"{input}\" isn't a {typeName}... ");
            }
        }
    }

    private static T ParseUserInput<T>(string input, string typeName)
    {
        switch (typeName)
        {
            case "int16": return (T)(object)short.Parse(input);
            case "int32": return (T)(object)int.Parse(input);
            case "int64": return (T)(object)long.Parse(input);
            case "uint16": return (T)(object)ushort.Parse(input);
            case "uint32": return (T)(object)uint.Parse(input);
            case "uint64": return (T)(object)ulong.Parse(input);
            case "single": return (T)(object)float.Parse(input);
            case "double": return (T)(object)double.Parse(input);
            case "decimal": return (T)(object)decimal.Parse(input);
            case "char": return (T)(object)char.Parse(input);
            case "string": return (T)(object)input;
            default: throw new Exception();
        }
    }

    public static void BenchmarkStart()
    {
        Debug.Assert(!_isTimerRunning, "Timer is already running!");

        _isTimerRunning = true;
        Console.Write("---- Timer started ----\n");
        _timer.Restart();
    }

    public static void BenchmarkStop(string message = "")
    {
        Debug.Assert(_isTimerRunning, "Timer is not running. Please start the timer before stopping it!");

        _timer.Stop();
        _isTimerRunning = false;

        // Format result to string
        var elapsedTime = _timer.Elapsed;
        string result = BenchmarkFormatTime(elapsedTime);

        // Print the result
        Console.Write($"\n---- Timer stopped ----\n");
        if (message != "")
        {
            Console.Write($"> {message}\n");
        }
        Console.Write($"> {result}\n\n");
    }

    private static string BenchmarkFormatTime(TimeSpan elapsedTime)
    {
        // Microseconds
        if (elapsedTime.TotalMilliseconds < 1)
        {
            return $"{elapsedTime.TotalMicroseconds:0.#} Microseconds";
        }
        // Milliseconds
        else if (elapsedTime.TotalSeconds < 1)
        {
            return $"{elapsedTime.TotalMilliseconds:0.#} Milliseconds";
        }
        // Format as 00:00:00
        else
        {
            return $"{elapsedTime:hh\\:mm\\:ss},{elapsedTime.Milliseconds}";
        }
    }

    public static void Print<T>(T a)
    {
        Console.WriteLine(a);
    }

    public static void Print<T>(HashSet<T> set)
    {
        foreach (var a in set)
            Console.Write($"{a}, ");
        Console.WriteLine();
    }

    public static void Print<T>(T[] arr)
    {
        foreach (var a in arr)
            Console.Write($"{a}, ");
        Console.WriteLine();
    }

    public static void Print<T>(T[][] arr)
    {
        foreach (var a in arr)
        {
            foreach (var item in a)
                Console.Write(item + " ");
            Console.WriteLine();
        }
    }

    public static void Print<T>(List<T[]> arr)
    {
        foreach (var a in arr)
        {
            foreach (var item in a)
                Console.Write(item + " ");
            Console.WriteLine();
        }
    }
}
