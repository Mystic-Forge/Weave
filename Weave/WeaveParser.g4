parser grammar WeaveParser;

options {
    tokenVocab = WeaveLexer;
}

start : topLevel* EOF;

topLevel : exportStatement | importStatement | event | listener | memory | function;

importStatement : IMPORT (import_identifier SLASH)* (identifier | MULTIPLY) (AS identifier)?;
exportStatement : EXPORT identifier;
event : EVENT identifier (WITH (labeled_type COMMA)*? labeled_type)?;
listener : ON identifier (WITH (identifier COMMA)*? identifier)? DO block END;
memory : MEMORY identifier BEING identifier;
function : identifier? FUNCTION identifier (WITH (labeled_type COMMA)*? labeled_type)? DO block END;

block : statement*;

labeled_type : identifier BEING identifier;

// statements
statement : if | print | temp | assignment | save | load | expression | while | for | exit | return;

print : PRINT expression;
temp : TEMP identifier ASSIGN expression;
assignment : expression ASSIGN expression;
save : SAVE (identifier | expression) (AS identifier)?;
load : LOAD identifier (AS identifier)?;
while : WHILE expression DO block END;
for : FOR identifier IN expression DO block END;
exit : EXIT;
return : RETURN expression?;

// Expressions
expression : 
      if
    | property_access
    | function_call
    | literal 
    | identifier
    | LPAREN expression RPAREN
    | (NOT | MINUS) expression
    | expression (MULTIPLY | SLASH | MOD) expression
    | expression (PLUS | MINUS) expression
    | expression (IS | IS_NOT | LESS | LESS_EQUAL | GREATER | GREATER_EQUAL) expression;
    
if : IF expression THEN block (ELSE IF expression THEN block)* (ELSE block)* END;
property_access : identifier OF expression;
function_call : identifier LPAREN ((expression COMMA)*? expression)? RPAREN;
identifier : NAME;
import_identifier : identifier | (DOT DOT);
literal : NIL | INT | FLOAT | STRING | BOOL | list;
list : LBRACKET (((expression COMMA)*? expression) | identifier) RBRACKET;