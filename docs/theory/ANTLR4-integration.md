# Интеграция antlr4 в C# проект
1. Создайте проект -
```csharp
dotnet new console -n AntlrDemo
cd AntlrDemo
```

2. Добавьте необходимые `nuget` пакеты -
```csharp
dotnet add package ANTLR4.Runtime.Standard --version 4.13.1
dotnet add package Antlr4BuildTasks --version 8.30.0
```

3. Создайте грамматику -

- Создайте папку и файл грамматики
```csharp
mkdir Grammars
notepad Grammars\Expr.g4
```

- Вставьте в файл грамматики простую грамматику в формате `EBNF` -
```ebnf
grammar Expr;
prog: expr EOF;
expr: expr ('*' | '/') expr
    | expr ('+' | '-') expr
    | INT
    | '(' expr ')'
    ;
INT: [0-9]+;
WS: [ \t\r\n]+ -> skip;
```

4. Настройте `AntrlDemo.csproj` файл -
```csharp
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Antlr4 Include="Grammars\Expr.g4" />
  </ItemGroup>

  <PropertyGroup>
    <Antlr4DefaultListener>false</Antlr4DefaultListener>
    <Antlr4DefaultVisitor>true</Antlr4DefaultVisitor>
  </PropertyGroup>
</Project>
```

5. Соберите проект -
```csharp
dotnet build
```

6. Проверьте парсер в коде, заменив `Program.cs` -
```csharp
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

var input = "3 + 4 * 2";
var stream = new AntlrInputStream(input);
var lexer = new ExprLexer(stream);
var tokens = new CommonTokenStream(lexer);
var parser = new ExprParser(tokens);
IParseTree tree = parser.prog();

Console.WriteLine("Parse tree:");
Console.WriteLine(tree.ToStringTree(parser));
```

8. Запустите и проверьте результат -
```csharp
dotnet run
```

```csharp
Parse tree:
(prog (expr (expr 3) + (expr (expr 4) * (expr 2))) <EOF>)
```
