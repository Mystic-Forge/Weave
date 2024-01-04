parser grammar WeaveParser;

options {
    tokenVocab = WeaveLexer;
}

start : topLevel* EOF;

topLevel : exportStatement | importStatement | event | listener;

importStatement : IMPORT (identifier SLASH)* identifier;
exportStatement : EXPORT identifier;
event : EVENT identifier (WITH (labeled_type COMMA)*? labeled_type)?;
listener : ON identifier (WITH (identifier COMMA)*? identifier)? DO block END;

block : statement*;

labeled_type : identifier BEING type;

// statements
statement : if | print | temp | assignment;

print : PRINT expression;
temp : TEMP identifier ASSIGN expression;
assignment : identifier ASSIGN expression;

// Expressions
expression : 
      if
    | while
    | for
    | literal 
    | identifier
    | LPAREN expression RPAREN
    | (NOT | MINUS) expression
    | expression (MULTIPLY | SLASH | MOD) expression
    | expression (PLUS | MINUS) expression
    | expression (IS | IS_NOT | LESS | LESS_EQUAL | GREATER | GREATER_EQUAL) expression;
    
if : IF expression THEN block (ELSE IF expression THEN block)* (ELSE block)* END;
while : WHILE expression DO block END;
for : FOR identifier IN expression DO block END;
identifier : NAME;
literal : INT | FLOAT | STRING | BOOL;
type : INT_TYPE | FLOAT_TYPE | STRING_TYPE | BOOL_TYPE;