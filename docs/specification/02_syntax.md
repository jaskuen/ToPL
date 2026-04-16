# `W Language` — Синтаксическая спецификация

Документ описывает грамматику языка, приоритеты операций и структуру программ.

## 1. Выражения

Выражения в языке `W Language` могут содержать:

1. Переменные и константы
2. Литералы (числовые, строковые, булевы)
3. Вызовы функций (пользовательских и встроенных)
4. Операторы (арифметические, логические, сравнения, присваивания)
5. Группировку с помощью скобок `()`

## 2. Приоритеты операций

Операции перечислены в порядке убывания приоритета.

| Приоритет (по убыванию) | Оператор           | Описание               | Ассоциативность |
| ----------------------- | ------------------ | ---------------------- | --------------- |
| 10                      | `++` (префикс)     | Префиксный инкремент   | Правая          |
| 10                      | `--` (префикс)     | Префиксный декремент   | Правая          |
| 10                      | `!`, `not`         | Логическое НЕ          | Правая          |
| 10                      | `-` (унарный)      | Унарный минус          | Правая          |
| 9                       | `++` (постфикс)    | Постфиксный инкремент  | Левая           |
| 9                       | `--` (постфикс)    | Постфиксный декремент  | Левая           |
| 8                       | `*`                | Умножение              | Левая           |
| 8                       | `/`                | Деление                | Левая           |
| 8                       | `%`                | Остаток от деления     | Левая           |
| 7                       | `+`                | Сложение, конкатенация | Левая           |
| 7                       | `-` (бинарный)     | Вычитание              | Левая           |
| 6                       | `<`                | Меньше                 | Левая           |
| 6                       | `<=`               | Меньше или равно       | Левая           |
| 6                       | `>`                | Больше                 | Левая           |
| 6                       | `>=`               | Больше или равно       | Левая           |
| 5                       | `==`, `equals`     | Равенство              | Левая           |
| 5                       | `!=`               | Неравенство            | Левая           |
| 4                       | `&&`, `and`        | Логическое И           | Левая           |
| 3                       | `\|\|`, `or`       | Логическое ИЛИ         | Левая           |
| 2                       | `=`                | Присваивание           | Правая          |
| 1                       | `,`                | Последовательность     | Левая           |

>[!INFO]
> - Словесные и символьные формы операторов имеют одинаковый приоритет.
> - Скобки `()` могут использоваться для изменения приоритета.
> - Операторы с одинаковым приоритетом вычисляются согласно их ассоциативности.

## 3. Структура программы

### 3.1 Общие правила
>[!INFO]
> Программа состоит из одного файла.
> Обязательна функция `main` — точка входа.
> Глобальные объявления (переменные, константы, функции) разрешены.
> Функции могут быть объявлены только на глобальном уровне (вложенные функции не поддерживаются).
> Все инструкции завершаются точкой с запятой `;`.
> Блоки кода заключаются в фигурные скобки `{}`.
> Пробельные символы (пробел, табуляция, новая строка) игнорируются.

### 3.2 Точка входа

Точкой входа в программу является функция `main`. 

**Синтаксис:**
```cpp
void main() { ... }
// или
int main() { 
...
return 0;
 }
```

>[!INFO]
> Возвращаемое значение функции может быть `void` или `int`.
> Если возвращаемый тип `int`, он интерпретируется как код возврата (0 — успех).

>[!TIP]
> Функция `main` не поддерживает списка аргументов в текущей версии.

### 3.3 Инструкции (Statements)

Язык поддерживает следующие типы инструкций:
- Объявление переменных
- Объявление констант
- Присваивание
- Ветвления
- Циклы
- Управление циклами
- Возврат из функции
- Ввод-вывод
- Вызов функций

## 4. Объявление переменных и констант

**Переменные:**
```cpp
int x;
int y = 10;
float pi = 3.14;
string name = "W";
```

**Константы:**
```cpp
const int MAX = 100;
const string GREETING = "Hi";
```

## 5. Объявление функций

**Синтаксис:**
```cpp
type identifier(parameter_list) {
}
```

**Примеры:**
```cpp
int GetNumber() {
    return 42;
}

int Add(int a, int b) {
    return a + b;
}

void PrintMessage(string msg) {
    write(msg);
}
```

## 6. Условные операторы

**Синтаксис:**
```cpp
if (условие) {
} else {
}
```

**Примеры:**
```cpp
if (x > 0) {
    write("Положительное");
} else {
    write("Неположительное");
}

if (age >= 18) {
    write("Совершеннолетний");
}
```

## 7. Циклы

**Синтаксис:**
```cpp
while (условие) {
}

for (инициализация; условие завершения; шаг) {
}
```

**Примеры:**
```cpp
int i = 0;
while (i < 10) {
    write(i);
    i++;
}

while (true) {
    int x;
    read(x);
    if (x == 0) {
        break;
    }
    if (x < 0) {
        continue;
    }
    write(x);
}

for (int i = 0; i < 10; i++) {
    write(i);
}

for (;;) {
    // бесконечный цикл
    break;
}
```

## 8. EBNF-грамматика

```ebnf
program = { global_declaration } , main_function ;

global_declaration = variable_definition 
                   | const_definition 
                   | function_definition ;

main_function = ( "void" | "int" ) , "main" , "(" , ")" , block ;

function_definition = type , identifier , "(" , [ parameter_list ] , ")" , block ;

parameter_list = parameter , { "," , parameter } ;
parameter = type , identifier ;

block = "{" , { statement } , "}" ;

statement = variable_definition
          | const_definition
          | assignment , ";"
          | if_statement
          | while_statement
          | for_statement
          | break_statement , ";"
          | continue_statement , ";"
          | return_statement , ";"
          | io_statement , ";"
          | expression_statement , ";"
          | empty_statement ;
          
empty_statement = ";" ;

variable_definition = type , identifier [ "=" , expression ] , 
                      { "," , identifier [ "=" , expression ] } , ";" ;

const_definition = "const" , type , identifier , "=" , expression , ";" ;

assignment = identifier , "=" , expression ;

if_statement = "if" , "(" , expression , ")" , block , [ "else" , block ] ;

while_statement = "while" , "(" , expression , ")" , block ;

for_statement = "for" , "(" , [ for_init ] , ";" , [ expression ] , ";" , [ for_post ] , ")" , block ;
for_init = variable_definition | expression ;
for_post = expression ;

return_statement = "return" , [ expression ] ;

break_statement = "break" ;

continue_statement = "continue" ;

io_statement = "read" , "(" , [ identifier ] , ")"
             | "write" , "(" , expression , ")" ;

expression_statement = expression ;

expression = assignment_expr ;

assignment_expr = logical_or , [ "=" , assignment_expr ] ;

logical_or = logical_and , { ( "||" | "or" ) , logical_and } ;

logical_and = equality , { ( "&&" | "and" ) , equality } ;

equality = comparison , { ( "==" | "equals" | "!=" ) , comparison } ;

comparison = additive , { ( "<" | "<=" | ">" | ">=" ) , additive } ;

additive = multiplicative , { ( "+" | "-" ) , multiplicative } ;

multiplicative = unary , { ( "*" | "/" | "%" ) , unary } ;

unary = ( "!" | "not" | "-" | "++" | "--" ) , unary
      | postfix ;

postfix = primary , [ "++" | "--" ] ;

primary = literal
        | identifier , [ "(" , [ argument_list ] , ")" ]
        | builtin_function
        | "(" , expression , ")" ;

builtin_function = ( "abs" | "round" | "ceil" | "floor" ) , "(" , expression , ")"
                 | ( "min" | "max" ) , "(" , expression , "," , expression , ")"
                 | "length" , "(" , expression , ")"
                 | "substring" , "(" , expression , "," , expression , "," , expression , ")" ;

argument_list = expression , { "," , expression } ;

type = "int" | "float" | "string" | "bool" | "void" ;