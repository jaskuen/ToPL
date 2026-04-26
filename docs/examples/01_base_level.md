## CircleSquare
- читает радиус круга и печатает его площадь
```cpp
// void - один из вариантов main
// int можно сделать во 2 эпике, т.к. это работа с функциями
void main()
{
    // Объявление констнат и переменных
    const float PI = 3.141592;
    float r;

    // Функции ввода/вывода
    write("Введите радиус круга: ");
    read(r);

    float area = PI * r * r;
    write("Площадь круга: ", area);
}
```

##  ReverseString
- читает строку, переворачивает её символы и печатает результат
```cpp
void main()
{
    // Чтение строки
    string s;
    read(s);
    
    int len = length(s);
    string res = "";
    int i = 0;
    
    // Методом 2-х указателей не сделать, т.к. строки это не массивы
    // И массивы мы пока не поддерживаем
    while (i++ < len)
    {
        res = res + substring(s, len - i, 1);
    }
    write(res);
}
```