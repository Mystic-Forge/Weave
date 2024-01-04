lexer grammar WeaveLexer;

// Keywords
ON : 'on';
IN : 'in';
WITH : 'with';
DO : 'do';
TEMP : 'temp';
PRINT : 'print';
IF : 'if';
THEN : 'then';
ELSE : 'else';
END : 'end';
IMPORT : 'import';
EXPORT : 'export';
EVENT : 'event';
BEING : 'being';
WHILE : 'while';
FOR : 'for';
STOP : 'stop';
NEXT : 'next';

// Operators
ASSIGN : '=';
PLUS : '+';
MINUS : '-';
MULTIPLY : '*';
MOD : 'mod';
IS_NOT : 'is not';
IS : 'is';
GREATER : '>';
LESS : '<';
GREATER_EQUAL : '>=';
LESS_EQUAL : '<=';
AND : 'and';
OR : 'or';
NOT : 'not';

// Symbols
SLASH : '/';
COMMA : ',';
COMMENT : '#' .*? '\n' -> skip;
BLOCK_COMMENT : '#-' .*? '-#' -> skip;
LPAREN : '(';
RPAREN : ')';

// Literals
BOOL : 'true' | 'false';
INT : Digit+;
FLOAT : Digit+ '.' Digit+;
STRING : '"' .*? '"'; // Why is '?' needed here?

// Types
INT_TYPE : 'Int';
FLOAT_TYPE : 'Float';
BOOL_TYPE : 'Bool';
STRING_TYPE : 'String';

NAME : [a-zA-Z_0-9]+;

WS : [ \t\r\n]+ -> skip;

fragment Digit: [0-9];