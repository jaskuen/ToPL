lexer grammar LanguageLexer;

WS: [ \t\r\n]+ -> skip;
COMMENT: '#' ~[\r\n]* -> skip;

STRING_LITERAL: '"' ( ~["\\\r\n] | '\\' [trn"\\0] )* '"';
REAL_LITERAL: DIGIT+ '.' DIGIT+;
INTEGER_LITERAL: DIGIT+;

PI: 'пи';
EULER: 'эйлер';

PREISPODNIAIA: 'преисподняя';
NEBESA: 'небеса';
KOLOBOK: 'колобок';
SINUS: 'синус';
KOSINUS: 'косинус';
TANGENS: 'тангенс';

GLAVNAYA: 'главная';
VOISTINU: 'воистину';
AMIN: 'аминь';
POKLON: 'поклон';

ASCHE: 'аще';
ILIZHE: 'илиже';

DOKOLE: 'доколе';
POVTORITI: 'повторити';
OTRESHITI: 'отрешити'; // break
UPOVAIU: 'уповаю';     // continue

IZBERETSYA: 'изберется';
EGDA: 'егда';
POELIKUZHE: 'поеликуже';

VOZGLASI: 'возгласи';
VNEMLI: 'внемли';

VOZVRATI: 'возврати';

DARUI: 'даруй';

ISTINNO: 'истинно';
LUKAVO: 'лукаво';
I: 'и';
ILI: 'или';
NE: 'не';

PACHE: 'паче';     // >=
MENYSHE: 'меньше'; // <=
VELII: 'велий';    // >
MALYI: 'малый';    // <
IAKO: 'яко';       // ==
NEGOZHE: 'негоже'; // !=
ROVNO: 'ровно';    // === 

PRIMUMNOZHU: 'приумножу';
UMALYU: 'умалю';
PLUS: '+';
MINUS: '-';
MUL: '*';
DIV: '/';
MOD: '%';

NICHTOZHE: 'ничтоже';
BLAGODAT: 'благодать';
KADILO: 'кадило';
VERYUSCHII: 'верующий';
SLOVESA: 'словеса';

LPAREN: '(';
RPAREN: ')';
LBRACK: '[';
RBRACK: ']';
COMMA: ',';
COLON: ':';

IDENTIFIER: (LETTER | '_') (LETTER | DIGIT | '_')*;

fragment LETTER: CYRILLIC_LETTER | LATIN_LETTER;
fragment CYRILLIC_LETTER: [а-яА-ЯёЁ];
fragment LATIN_LETTER: [a-zA-Z];
fragment DIGIT: [0-9];