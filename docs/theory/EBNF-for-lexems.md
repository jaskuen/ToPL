___EBNF грамматика будет описана для языка C#.___

Рассматриваются следующие типы лексем -

- `идентификатор (identifier)` – Это имена переменных, методов, классов, пространств имён и т. п.
- `литералы целых чисел (integer number)` – Целые числа в десятичной, шестнадцатеричной или двоичной системе.
- `литералы чисел с плавающей точкой (real number)` – Числа с дробной частью и/или экспонентой.
- `литералы строк (string literal)` - Текстовые значения в кавычках: обычные или `verbatim` –строки (`@"..."`).

| Символ EBNF     | Запись  | Описание                                                                       |
|-----------------|---------|--------------------------------------------------------------------------------|
| definition      | =       | Правило, то есть определение нетерминального символа через ряд других символов |
| concatenation   | ,       | Конкатенация символов                                                          |
| termination     | ;       | Конец правила                                                                  |
| alternation     | \|      | Альтернативные варианты правила                                                |
| optional        | [ … ]   | Опциональная часть правила (0 или 1 раз)                                       |
| repetition      | { … }   | Повторяющаяся часть правила (0, 1 или много раз)                               |
| grouping        | ( … )   | Атомарная группа символов                                                      |
| terminal string | " … "   | Терминальный символ (строка или символ Unicode)                                |
| comment         | (* … *) | Комментарий, не входит в определение                                           |
| exception       | -       | Исключение ряда символов из числа допустимых                                   |

__Идентификатор__

```ebnf
identifier = verbatim_identifier | simple_identifier ;
verbatim_identifier = '@', simple_identifier ;
simple_identifier = identifier_start, { identifier_part } ;
identifier_start = letter | '_' ;
identifier_part = letter | digit | '_' ;

letter = "A" | "B" | "C" | "D" | "E" | "F" | "G" | "H" | "I" | "J"
       | "K" | "L" | "M" | "N" | "O" | "P" | "Q" | "R" | "S" | "T"
       | "U" | "V" | "W" | "X" | "Y" | "Z"
       | "a" | "b" | "c" | "d" | "e" | "f" | "g" | "h" | "i" | "j"
       | "k" | "l" | "m" | "n" | "o" | "p" | "q" | "r" | "s" | "t"
       | "u" | "v" | "w" | "x" | "y" | "z" ;

digit = "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" ;
```

__Целый литерал__

```ebnf
integer_literal = decimal_integer | hex_integer | binary_integer ;

decimal_integer = digit, { digit_or_separator }, [ integer_type_suffix ] ;
digit_or_separator = '_', digit | digit ;

hex_integer = "0x", hex_digit, { hex_digit_or_separator }, [ integer_type_suffix ]
            | "0X", hex_digit, { hex_digit_or_separator }, [ integer_type_suffix ] ;

hex_digit_or_separator = '_', hex_digit | hex_digit ;

binary_integer = "0b", binary_digit, { binary_digit_or_separator }, [ integer_type_suffix ]
               | "0B", binary_digit, { binary_digit_or_separator }, [ integer_type_suffix ] ;

binary_digit_or_separator = '_', binary_digit | binary_digit ;

binary_digit = "0" | "1" ;

hex_digit = digit
          | "A" | "B" | "C" | "D" | "E" | "F"
          | "a" | "b" | "c" | "d" | "e" | "f" ;

integer_type_suffix = "u" | "U"
                    | "l" | "L"
                    | "ul" | "uL" | "Ul" | "UL"
                    | "lu" | "lU" | "Lu" | "LU" ;
```

__Вещественное число__

```ebnf
real_literal = digit, { digit_or_separator }, '.', digit, { digit_or_separator }, [ exponent_part ], [ real_type_suffix ]
             | '.', digit, { digit_or_separator }, [ exponent_part ], [ real_type_suffix ]
             | digit, { digit_or_separator }, exponent_part, [ real_type_suffix ]
             | digit, { digit_or_separator }, real_type_suffix ;

exponent_part = "e", [ sign ], digit, { digit_or_separator }
              | "E", [ sign ], digit, { digit_or_separator } ;

sign = "+" | "-" ;

real_type_suffix = "f" | "F" | "d" | "D" | "m" | "M" ;

(* Строковые литералы *)
string_literal = regular_string_literal | verbatim_string_literal ;

regular_string_literal = '"', [ regular_string_characters ], '"' ;
regular_string_characters = { regular_string_character } ;
regular_string_character = regular_char | escape_sequence ;
```

__Строковый литерал__

```ebnf
string_literal = regular_string_literal | verbatim_string_literal ;

regular_string_literal = '"', [ regular_string_characters ], '"' ;
regular_string_characters = { regular_string_character } ;
regular_string_character = regular_char | escape_sequence ;

regular_char = " " | "!" | "#" | "$" | "%" | "&" | "'" | "(" | ")" | "*" | "+" | "," | "-"
             | "." | "/" | "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"
             | ":" | ";" | "<" | "=" | ">" | "?" | "@" | "A" | "B" | "C" | "D" | "E" | "F"
             | "G" | "H" | "I" | "J" | "K" | "L" | "M" | "N" | "O" | "P" | "Q" | "R"
             | "S" | "T" | "U" | "V" | "W" | "X" | "Y" | "Z" | "[" | "]" | "^" | "_" | "`"
             | "a" | "b" | "c" | "d" | "e" | "f" | "g" | "h" | "i" | "j" | "k" | "l"
             | "m" | "n" | "o" | "p" | "q" | "r" | "s" | "t" | "u" | "v" | "w" | "x"
             | "y" | "z" | "{" | "|" | "}" | "~" ;

escape_sequence = '\', '"' 
                | '\', '\' 
                | '\', 'n' 
                | '\', 'r' 
                | '\', 't' 
                | '\', 'b' 
                | '\', 'f' 
                | '\', '0' 
                | '\', 'a' 
                | '\', 'v'
                | '\', 'x', hex_digit, [ hex_digit ], [ hex_digit ], [ hex_digit ]
                | '\', 'u', hex_digit, hex_digit, hex_digit, hex_digit ;

verbatim_string_literal = '@', '"', { verbatim_string_character }, '"' ;
verbatim_string_character = verbatim_char | '"', '"' ;

verbatim_char = " " | "!" | "#" | "$" | "%" | "&" | "'" | "(" | ")" | "*" | "+" | "," | "-"
              | "-" | "." | "/" | "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"
              | ":" | ";" | "<" | "=" | ">" | "?" | "@" | "A" | "B" | "C" | "D" | "E" | "F"
              | "G" | "H" | "I" | "J" | "K" | "L" | "M" | "N" | "O" | "P" | "Q" | "R"
              | "S" | "T" | "U" | "V" | "W" | "X" | "Y" | "Z" | "[" | "]" | "^" | "_" | "`"
              | "a" | "b" | "c" | "d" | "e" | "f" | "g" | "h" | "i" | "j" | "k" | "l"
              | "m" | "n" | "o" | "p" | "q" | "r" | "s" | "t" | "u" | "v" | "w" | "x"
              | "y" | "z" | "{" | "|" | "}" | "~" ;
```

