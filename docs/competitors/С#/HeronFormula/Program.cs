using System;

namespace HeronFormula;

public readonly struct Point2D
{
    public readonly double X;
    public readonly double Y;

    public Point2D(double x, double y)
    {
        X = x;
        Y = y;
    }
}

public static class Program
{
    public static double Distance(Point2D a, Point2D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static void Main()
    {
        string[] input = Console.ReadLine()!.Split();
        Point2D a = new Point2D(double.Parse(input[0]), double.Parse(input[1]));
        Point2D b = new Point2D(double.Parse(input[2]), double.Parse(input[3]));
        Point2D c = new Point2D(double.Parse(input[4]), double.Parse(input[5]));

        double ab = Distance(a, b);
        double bc = Distance(b, c);
        double ca = Distance(c, a);

        double p = (ab + bc + ca) / 2;
        double area = Math.Sqrt(p * (p - ab) * (p - bc) * (p - ca));

        Console.WriteLine(area.ToString("F6"));
    }
}