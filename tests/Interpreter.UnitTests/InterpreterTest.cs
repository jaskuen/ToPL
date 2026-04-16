using Parser;

using Runtime;

namespace Interpreter.Specs;

public class InterpreterTest
{
    [Theory]
    [MemberData(nameof(GetParseProgramTestData))]
    public void Can_parse_program(string sourceCode, List<RuntimeValue> inputValues, List<object> expectedOutputValues)
    {
        FakeEnvironment environment = new FakeEnvironment(inputValues);
        SaintInterpreter interpreter = new SaintInterpreter(environment);

        interpreter.Execute(sourceCode);

        // Проверяем вычисленный результат.
        IReadOnlyList<RuntimeValue> actual = environment.Results;
        for (int i = 0, iMax = Math.Min(expectedOutputValues.Count, actual.Count); i < iMax; ++i)
        {
            bool areEqual = expectedOutputValues[i] switch
            {
                int => (int)expectedOutputValues[i] == actual[i].ToInt(),
                double => Math.Abs((double)expectedOutputValues[i] - actual[i].ToFloat()) < 0.001,
                string => (string)expectedOutputValues[i] == actual[i].ToString(),
                bool => (bool)expectedOutputValues[i] == actual[i].ToBoolean(),
                _ => false,
            };
            Assert.True(areEqual);
        }
    }

    public static TheoryData<string, List<RuntimeValue>, List<object>> GetParseProgramTestData()
    {
        return new TheoryData<string, List<RuntimeValue>, List<object>>
        {
            {
                """
                void main()
                {
                    // объявление и инициализация чисел
                    int num1 = 0;
                    int num2 = 0;

                    // ввод чисел
                    write("Введите первое число: ");
                    read(num1);

                    write("Введите второе число: ");
                    read(num2);

                    // вычисление суммы
                    int summ = num1 + num2;
                    write("Сумма: ", summ);
                }
                """,
                [new RuntimeValue(5), new RuntimeValue(10)],
                ["Введите первое число: ", "Введите второе число: ", "Сумма: 15", "15"]
            },
            {
                """
                void main()
                {
                    const float PI = 3.141592;
                    float radius = 0.0;

                    write("Введи радиус круга: ");
                    read(radius);

                    // вычисление площади круга
                    float circleArea = PI * (radius * radius);
                    write("Площадь круга = ", circleArea);
                }
                """,
                [new RuntimeValue(5)], ["Введи радиус круга: ", "Площадь круга = 78.5398", "78.5398"]
            },
            {
                """
                void main()
                {
                    // объявление переменных
                    float km, miles, mileToKm;
                    mileToKm = 1.609344;

                    write("Введите расстояние в милях: ");
                    read(miles);

                    km = miles * mileToKm;
                    write("Расстояние в милях ", miles, " = ", km, " км");
                }
                """,
                [new RuntimeValue(4)], ["Введите расстояние в милях: ", "Расстояние в милях 4 = 6.4374 км", "6.4374"]
            },
            {
                """
                void main()
                {
                    int N = 0;
                    write("Введите N: ");
                    read(N);

                    // Цикл вывода таблицы умножения
                    int i = 1;
                    while (i <= N)
                    {
                        int j = 1;
                        while (j <= N)
                        {
                            write(i * j, "\t");
                            j = j + 1;
                        }
                        i++;
                        write("\n");
                    }
                }
                """,
                [new RuntimeValue(3)], [
                    "Введите N: ",
                    "1\t", "2\t", "3\t", "\n",
                    "2\t", "4\t", "6\t", "\n",
                    "3\t", "6\t", "9\t", "\n",
                    "0"
                ]
            },
            {
                """
                // Функция вычисления модуля числа с плавающей точкой
                float fabs(float n)
                {
                    if (n < 0)
                    {
                        return -n;
                    }

                    return n;
                }

                // Функция вычисления квадратного корня методом Ньютона
                // Возвращает -1, если корень нельзя вычислить
                // Возвращает корень числа n, с точностью epsilon
                float Sqrt(float n, float epsilon)
                {
                    if (n < 0)
                    {
                        write("Нельзя вычислить корень из отрицательного числа! \n");
                        return -1;
                    }

                    // sqrt(0) == 0
                    if (n == 0)
                    {
                        return 0;
                    }

                    float x = n;
                    float delta = epsilon + 1;

                    while(delta > epsilon)
                    {
                        float nextX = 0.5 * (x + (n / x));
                        delta = fabs(nextX - x);
                        x = nextX;
                    }

                    return x;
                }

                void main()
                {
                    float X;
                    write("Введите X: ");
                    read(X);

                    write("Sqrt(X) = ", Sqrt(X, 0.001));
                }
                """,
                [new RuntimeValue(169)], ["Введите X: ", "Sqrt(X) = 13", "0"]
            },
            {
                """
                // Пример рекурсии
                // Программа делит исходное число на 2 до тех пор,
                // пока не достигнет числа, < 10
                float LessThanTen(float n)
                {
                    float newN = n / 2;
                    if (newN > 10)
                    {
                        return LessThanTen(newN);
                    }
                    
                    return newN;
                }

                void main()
                {
                    float number;

                    write("Введите число: ");
                    read(number);
                    
                    return LessThanTen(number);
                }
                """,
                [new RuntimeValue(100)], ["Введите число: ", "6.25"]
            },
        };
    }
}