    parser grammar WeaveParser;

options {
    tokenVocab = WeaveLexer;
}

start : topLevel* EOF;

topLevel : self_assertion | exportStatement | importStatement | event | listener | memory | function;

importStatement : IMPORT (import_identifier SLASH)* (identifier | MULTIPLY) (AS identifier)?;
exportStatement : EXPORT identifier;
event : type? EVENT identifier (WITH (labeled_type COMMA)*? labeled_type)?;
listener : ON identifier (WITH (identifier COMMA)*? identifier)? DO block END;
memory : MEMORY identifier BEING identifier;
function : identifier? FUNCTION identifier (WITH (labeled_type COMMA)*? labeled_type)? DO block END;
self_assertion : SELF BEING identifier;

block : statement*;

type : identifier;
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
    | (list_prefix_function)+? FROM expression
    | expression (list_suffix_function)+?
    | list_initialization
    | property_access
    | function_call
    | literal 
    | identifier
    | LPAREN expression RPAREN
    | (NOT | MINUS) expression
    | expression (MULTIPLY | SLASH | MOD) expression
    | expression (PLUS | MINUS) expression
    | expression (IS | IS_NOT | LESS | LESS_EQUAL | GREATER | GREATER_EQUAL) expression
    | expression (AND | OR) expression;
    
if : IF expression THEN block (ELSE IF expression THEN block)* (ELSE block)* END;
property_access : identifier OF expression;
function_call : identifier LPAREN ((expression COMMA)*? expression)? RPAREN;
identifier : NAME | SELF;
import_identifier : identifier | (DOT DOT);
enum : identifier DOT identifier;
literal : NIL | INT | FLOAT | STRING | BOOL | list | enum;
list : LBRACKET (((expression COMMA)*? expression) | identifier) RBRACKET;

// List functions
list_initialization : list_range | list_append | list_prepend | list_insert;

list_prefix_function : list_index | list_skip | list_take | list_where | list_select | list_flattened | list_all | list_any | list_split;
list_index : INDEX expression;
list_skip : SKIP_ITEMS expression;
list_take : TAKE_ITEMS expression;
list_where : WHERE expression;
list_select : SELECT expression;
list_flattened : FLATTENED;
list_all : ALL;
list_any : ANY;
list_split : SPLIT expression;

list_suffix_function : list_sorted | list_reversed | list_unique;
list_sorted : SORTED (ASCENDING | DESCENDING) (BY expression)?;
list_reversed : REVERSED;
list_unique : UNIQUE;

list_range : RANGE expression TO expression (EXCLUSIVE | INCLUSIVE);
list_append : APPEND expression TO expression;
list_prepend : PREPEND expression TO expression;
list_insert : INSERT expression AT expression IN expression;