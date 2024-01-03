lexer grammar WeaveLexer;

// Keywords
ON : 'on';
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

// Types
INT_TYPE : 'int';

// Operators
ASSIGN : '=';

// Symbols
SLASH : '/';
COMMA : ',';
COMMENT : '#' .*? '\n' -> skip;
BLOCK_COMMENT : '#-' .*? '-#' -> skip;

BOOL : 'true' | 'false';
INT : Digit+;
FLOAT : Digit+ '.' Digit+;
STRING : '"' .*? '"'; // Why is '?' needed here?

NAME : [a-zA-Z_0-9]+;

WS : [ \t\r\n]+ -> skip;

fragment Digit: [0-9];