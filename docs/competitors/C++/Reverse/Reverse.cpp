#include <iostream>
#include <string>
#include <algorithm>

int main() 
{
    std::string input;
    std::getline(std::cin, input);

    std::reverse(input.begin(), input.end());
    std::cout << input << std::endl;

    return EXIT_SUCCESS;
}