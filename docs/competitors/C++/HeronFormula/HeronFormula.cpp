#include <iostream>
#include <cmath>
#include <iomanip>

struct Point2D 
{
    double x;
    double y;
};

double Distance(const Point2D& a, const Point2D& b) 
{
    double dx = a.x - b.x;
    double dy = a.y - b.y;

    return std::sqrt(dx * dx + dy * dy);
}

int main() 
{
    Point2D a, b, c;
    std::cin >> a.x >> a.y >> b.x >> b.y >> c.x >> c.y;

    double ab = Distance(a, b);
    double bc = Distance(b, c);
    double ca = Distance(c, a);

    double p = (ab + bc + ca) / 2;
    double area = std::sqrt(p * (p - ab) * (p - bc) * (p - ca));

    std::cout << std::fixed << std::setprecision(6) << area << std::endl;

    return EXIT_SUCCESS;
}