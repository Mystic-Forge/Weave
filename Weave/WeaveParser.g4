parser grammar WeaveParser;

options {
    tokenVocab = WeaveLexer;
}

start : topLevel* EOF;

topLevel : exportStatement | importStatement | event | listener;

importStatement : IMPORT (identifier SLASH)* identifier;
exportStatement : EXPORT identifier;
listener : ON identifier (WITH (identifier COMMA)*? identifier)? DO block END;
event : EVENT identifier (WITH (type COMMA)*? type)*?;

block : statement*;

// statements
statement : if | print | temp | assignment;

print : PRINT expression;
temp : TEMP identifier ASSIGN expression;
assignment : identifier ASSIGN expression;

// Expressions
expression : 
      if
    | literal 
    | identifier
    | (NOT | MINUS) expression
    | expression (MULTIPLY | SLASH | MOD) expression
    | expression (PLUS | MINUS) expression
    | expression (IS) expression;

if : IF expression THEN block (ELSE IF expression THEN block)* (ELSE block)* END;
identifier : NAME;
type : INT_TYPE;
literal : INT | FLOAT | STRING | BOOL;
