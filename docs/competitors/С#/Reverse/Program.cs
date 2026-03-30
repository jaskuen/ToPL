using System;

namespace Reverse;

public static class Program
{
    public static void Main()
    {
        string input = Console.ReadLine()!;
        char[] charArray = input.ToCharArray();
        Array.Reverse(charArray);
        Console.WriteLine(new string(charArray));
    }
}