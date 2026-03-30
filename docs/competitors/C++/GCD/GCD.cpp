#include <iostream>

int GCD(int a, int b) 
{
    while (b != 0) 
    {
        int temp = b;
        b = a % b;
        a = temp;
    }

    return a;
}

int main() 
{
    int num1, num2;
    std::cin >> num1 >> num2;

    std::cout << GCD(num1, num2) << std::endl;

    return EXIT_SUCCESS;
}