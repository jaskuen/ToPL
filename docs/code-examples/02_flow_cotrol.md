## Factorial
- читает целое число и печатает его факториал
```cpp
// Рекурсивная функция факториала
int Factorial(int n) 
{
    if (n <= 1) 
    {
        return 1;
    }
    return n * Factorial(n - 1);
}

// Используем int, 0 - EXIT_SUCCESS
int main() 
{
    int n;
    read(n);
    write(Factorial(n));

    return 0;
}
```

## FizzBuzz
- в цикле читает целые числа и печатает ответ, пока не встретит конец ввода
    - если число делится на 3, то печатает Fizz
    - если делится на 5, то печатает Buzz
    - может напечатать “FizzBuzz”, если число делится как на 3, так и на 5
    - если не делится ни на 3, ни на 5, то печатает само число

```cpp
int main() 
{
    int x;
    write("Введите число, 0 - для выхода: ");
    while (true) 
    {
        read(x);
        
        // Условие выхода - можно было сделать и через условие цикла, но есть break
        if (x == 0) 
        {
            break;
        }

        if (x % 3 == 0 && x % 5 == 0) 
        {
            write("FizzBuzz");
        }
        else 
        {
            if (x % 3 == 0) 
            {
                write("Fizz");
            }
            else 
            {
                if (x % 5 == 0) 
                {
                    write("Buzz");
                } 
                else 
                {
                    write(x);
                }
            }
        }
        
        write("\n");
    }

    return 0;
}
```