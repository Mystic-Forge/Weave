lexer grammar WeaveLexer;

// Comments
COMMENT : '#' .*? ('\n' | EOF) -> skip;
BLOCK_COMMENT : '#-' .*? '-#' -> skip;

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
EXIT : 'exit';
NEXT : 'next';
MEMORY : 'memory';
SAVE : 'save';
LOAD : 'load';
AS : 'as';
FUNCTION : 'function';
RETURN : 'return';
OF : 'of';

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
LPAREN : '(';
RPAREN : ')';
LBRACKET : '[';
RBRACKET : ']';
DOT : '.';

// Literals
NIL : 'nil';
BOOL : 'true' | 'false';
INT : Digit+;
FLOAT : Digit+ '.' Digit+;
STRING : '"' .*? '"';
NAME : [a-zA-Z_0-9]+;

WS : [ \t\r\n]+ -> skip;

fragment Digit: [0-9];