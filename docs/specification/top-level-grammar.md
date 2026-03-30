# Ключевые особенности языка

## Семантические правила
- Запрещено объявлять переменные с тем же именем в одном блоке видимости
- Можно использовать пустой оператор

# EBNF

```ebnf
(* Основная структура программы *)
program = {function_declaration}, main_function, EOF_statement ;

EOF_statement = (? конец файла ?) ;

main_function = "главная", scope;

(* Объявление функции *)
function_declaration = return_type, identifier, function_parameters, scope;

(* Тип возвращаемого значения *)
return_type = type_name | "ничтоже";

(* Параметры функции *)
function_parameters = "(", [parameter_list], ")";

parameter_list = parameter_declaration, {",", parameter_declaration};

parameter_declaration = type_name, identifier;

(* Обновление инструкций *)
statement = variable_declaration
          | assignment_statement
          | input_statement
          | output_statement
          | return_statement
          | function_call_statement
          | empty_statement
          | if_statement
          | while_statement
          | switch_statement
          | for_statement
          | break_statement
          | continue_statement
          ;
          
(* Объявления переменных *)
variable_declaration = type, variable_decl_list, "поклон";

variable_decl_list = variable_decl_item, {",", variable_decl_item};

variable_decl_item = identifier, [variable_initializer];

variable_initializer = "даруй", expression;

(* Присваивание *)
assignment_statement = identifier, assignment_tail;

assignment_tail = "даруй", expression, "поклон";

(* Операторы ввода-вывода *)
input_statement = "внемли", "(", input_arguments, ")", "поклон";

input_arguments = identifier, {",", identifier};

output_statement = "возгласи", "(", output_arguments, ")", "поклон";

output_arguments = output_item, {",", output_item};

output_item = string | expression;

(* Возврат из функции *)
return_statement = "возврати", expression, "поклон";

(* Пустой оператор *)
empty_statement = "поклон";

(* Вызов функции как отдельной инструкции *)
function_call_statement = identifier, function_arguments, "поклон";

(* Условные конструкции *)
if_statement = "аще", "(", logical_or_expression, ")", scope, ["илиже", scope];

(* Цикл while *)
while_statement = "доколе", "(", logical_or_expression, ")", scope;

(* Цикл for *)
for_statement = "повторити", "(", assignment_expression, ",", logical_or_expression, ",", assignment_expression, ")", scope;

(* Выражение присваивания для цикла for (без терминатора "поклон") *)
assignment_expression = identifier, "даруй", expression;

(* Конструкция switch-case *)
switch_statement = "изберется", "(", expression, ")", scope;

case_clause = "егда", constant, scope;
default_clause = "поеликуже", scope;

(* Обновление определения scope для поддержки case-блоков *)
scope = "воистину", {statement | case_clause | default_clause}, "аминь";

(* Оператор break *)
break_statement = "отрешити", "поклон";

(* Оператор continue *)
continue_statement = "уповаю", "поклон";

(* Типы данных *)
type = type_name;
```