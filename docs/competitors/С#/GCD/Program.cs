using System;

namespace GDC;

public static class Program
{
    public static int GCD(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }

        return a;
    }

    public static void Main()
    {
        string[] input = Console.ReadLine()!.Split();
        int num1 = int.Parse(input[0]);
        int num2 = int.Parse(input[1]);

        Console.WriteLine(GCD(num1, num2));
    }
}