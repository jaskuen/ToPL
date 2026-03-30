parser grammar LanguageParser;

options { tokenVocab=LanguageLexer; }

program: functionDeclaration* mainFunction EOF;

mainFunction: GLAVNAYA scope;

functionDeclaration
    : returnType IDENTIFIER LPAREN parameterList? RPAREN scope
    ;

returnType
    : type 
    | NICHTOZHE
    ;

parameterList
    : parameter (COMMA parameter)*
    ;

parameter
    : type IDENTIFIER
    ;

scope
    : VOISTINU statementItem* AMIN
    ;

statementItem
    : statement
    | caseStatement
    | defaultStatement
    ;

statement
    : variableDeclaration
    | assignmentStatement
    | inputStatement
    | outputStatement
    | returnStatement
    | functionCallStatement
    | emptyStatement
    | ifStatement
    | whileStatement
    | forStatement
    | switchStatement
    | breakStatement
    | continueStatement
    ;

variableDeclaration: type variableDeclList POKLON;
type: BLAGODAT | KADILO | VERYUSCHII | SLOVESA;

variableDeclList: variableDeclItem (COMMA variableDeclItem)*;
variableDeclItem: IDENTIFIER (DARUI expression)?;

assignmentStatement: IDENTIFIER DARUI expression POKLON;

inputStatement: VNEMLI LPAREN inputArguments RPAREN POKLON;
inputArguments: IDENTIFIER (COMMA IDENTIFIER)*;

outputStatement: VOZGLASI LPAREN outputArguments RPAREN POKLON;
outputArguments: outputItem (COMMA outputItem)*;
outputItem: STRING_LITERAL | expression;

ifStatement
    : ASCHE LPAREN expression RPAREN scope (ILIZHE scope)?
    ;

whileStatement
    : DOKOLE LPAREN expression RPAREN scope
    ;

forStatement
    : POVTORITI LPAREN assignmentExpression COMMA expression COMMA assignmentExpression RPAREN scope
    ;

assignmentExpression
    : IDENTIFIER DARUI expression
    ;

switchStatement
    : IZBERETSYA LPAREN expression RPAREN scope
    ;

caseStatement
    : EGDA literal scope
    ;

defaultStatement
    : POELIKUZHE scope
    ;

breakStatement: OTRESHITI POKLON;
continueStatement: UPOVAIU POKLON;
returnStatement: VOZVRATI expression POKLON;
emptyStatement: POKLON;
functionCallStatement: functionCall POKLON;

expression
    : expression DARUI expression                          # AssignmentExpr
    | logicalOrExpr                                        # LogicExpr
    ;

logicalOrExpr
    : logicalAndExpr (ILI logicalAndExpr)*
    ;

logicalAndExpr
    : equalityExpr (I equalityExpr)*
    ;

equalityExpr
    : comparisonExpr ((IAKO | NEGOZHE | ROVNO) comparisonExpr)*
    ;

comparisonExpr
    : additiveExpr ((PACHE | MENYSHE | VELII | MALYI) additiveExpr)*
    ;

additiveExpr
    : multiplicativeExpr ((PLUS | MINUS) multiplicativeExpr)*
    ;

multiplicativeExpr
    : unaryExpr ((MUL | DIV | MOD) unaryExpr)*
    ;

unaryExpr
    : (NE | PLUS | MINUS) postfixExpr  # PrefixUnary
    | postfixExpr                      # SimpleUnary
    ;

postfixExpr
    : primary (PRIMUMNOZHU | UMALYU)?
    ;

primary
    : literal
    | constant
    | functionCall
    | LPAREN expression RPAREN
    | IDENTIFIER
    | IDENTIFIER LBRACK expression RBRACK
    ;

literal
    : INTEGER_LITERAL
    | REAL_LITERAL
    | STRING_LITERAL
    | (ISTINNO | LUKAVO)
    ;

constant: PI | EULER;

functionCall
    : builtinFunction LPAREN expression RPAREN
    | IDENTIFIER LPAREN (expression (COMMA expression)*)? RPAREN
    ;

builtinFunction
    : PREISPODNIAIA
    | NEBESA
    | KOLOBOK
    | SINUS
    | KOSINUS
    | TANGENS
    ;