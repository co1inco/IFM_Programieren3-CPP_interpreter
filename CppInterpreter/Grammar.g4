grammar Grammar;

//program : topLevelStatement* EOF;
program : topLevelStatement*;

// REPL (Read Eval Print Loop)
// No usage in parser rules just for the repl (handling input)
replStatement : statement | expression | topLevelStatement;

// toplevel (statements of the highest scope) 
topLevelStatement : functionDefinition
		          | variableDefinition ';'
		          | class;

// classes (with inheritance) / structs
class : defaultVis=(CLASS | STRUCT) typeIdentifier classInheritance? classBlock ';'; 

// class/struct body 
classBlock: '{' classBlockStatement* '}';

// Content within classes
classBlockStatement : classMemberMod functionDefinition 
					| classMemberMod variableDefinition ';'
					| classConstructor
					| classDestructor
					| pub=PUBLIC ':' 
					| prv=PRIVATE ':';

// virtual modifier
classMemberMod : virtual=VIRTUAL?;

// Multiple inheritance 
classInheritance : ':' classInheitanceIdent (',' classInheitanceIdent)*;  

// Modifier for inhereted classes 
classInheitanceIdent : vis=(PRIVATE|PUBLIC)? typeIdentifier;

classConstructor : ident=IDENTIFIER '(' parameterList ')' block;

classDestructor : '~' ident=IDENTIFIER '(' ')' block;

// Statements
statement : returnStmt ';'
		  | breakStmt ';'
		  | continueStmt ';'
// 		  | functionDefinition
		  | variableDefinition ';'
		  | ifStmt
		  | whileStmt
		  | forStmt
		  | doWhileStmt
		  | block
		  | expression ';'
		  ;

returnStmt : 'return' expression?;

functionDefinition : (typeIdentifierUsage | void=TYPE_VOID) ident=IDENTIFIER '(' parameterList ')' block;

parameterList : (typeIdentifierUsage varIdentifier)? (',' typeIdentifierUsage varIdentifier)* ; 

variableDefinition : typeIdentifierUsage varIdentifier ('=' expression)?;

ifStmt : 'if' '(' cond=expression ')' innerBlock elseStmt?;

elseStmt : 'else' (ifStmt | innerBlock);

whileStmt : 'while' '(' cond=expression ')' innerBlock;

forStmt : 'for' '(' setup=forStmtNestedStmt? ';' cond=expression? ';' incr=forStmtNestedStmt? ')' innerBlock;

// for loop statements nested 
forStmtNestedStmt : variableDefinition | expression;

doWhileStmt : 'do' block 'while' '(' cond=expression ')' ';';

breakStmt : 'break';

continueStmt : 'continue';

// Syntactic sugar (handling if() ...;)
innerBlock : block | statement | ';';

// New scopes 
block : '{' statement* '}';

// Expressions
expression : '(' brace=expression ')'
		   // Suffix (right side of variable for e.g.)
		   | expression suffix=('++' | '--')
		   // correct function resolving (y = x + foo())
 		   | func=expression '(' funcParameters? ')' 
 		   // arrays but not used 
		   | subscript=expression '[' param=expression ']'
		   | memberExpr=expression memberAccess='.' memberAtom=atom
		   // Pointer but not used 
		   | memberExpr=expression memberAccess='->' memberAtom=atom
		   // Prefix (left side of variables for e.g.)
		   | unary=('++' | '--') expression 
           | unary=('+' | '-' | '!' | '~' ) expression
           // - Case, derefference, address of, sizeof, new
           // Infix (between ...)
		   | left=expression binop=('*' | '/' | '%') right=expression
		   | left=expression binop=('+' | '-') right=expression
		   | left=expression comp=('<' | '<=' | '>' | '>=') right=expression 
		   | left=expression comp=('==' | '!=') right=expression 
		   // not relevant 
		   | left=expression bit='&' right=expression 
		   | left=expression bit='^' right=expression 
		   | left=expression bit='|' right=expression 
		   | left=expression logic=('&&' | '||') right=expression
		   // Assignment 
		   | left=expression assign='=' right=expression
		   // - Compond assignments
		   // Comma 
		   // Utility
		   | atom
		   | literal;


atom : IDENTIFIER;

literal : intLiteral
		| str=STRING 
		| char=CHAR 
		| bool=BOOL;

intLiteral :  int=INTEGER 
           |  hex=INTEGER_HEX 
           |  bin=INTEGER_BIN; 

// Utility
varIdentifier : ident=IDENTIFIER;

funcParameters : expression (',' expression)*;

typeIdentifierUsage : typeIdentifier ref='&'?; // & should actually be part of name?
typeIdentifier : int=TYPE_INT
			   | str=TYPE_STRING
			   | bool=TYPE_BOOL
//			   | void=TYPE_VOID
			   | ident=IDENTIFIER;


//include : '#include' '<' file=.*? '>'
//		| '#include' '"' file=.*? '"';
//Tokens

// standard types 
TYPE_INT : 'int';
TYPE_STRING : 'string';
TYPE_BOOL : 'bool';
TYPE_VOID : 'void';

// hexadecimal numbers 
INTEGER: [0-9]+;
INTEGER_HEX: '0x'[0-9a-fA-F]+;
INTEGER_BIN: '0b'[0-1_]+;
STRING: '"'(~('"')|(' '|'\b'|'\f'|'r'|'\n'|'\t'|'\\"'|'\\'|'\\0'))*'"';
CHAR: '\''(~('\'')|(' '|'\b'|'\f'|'r'|'\n'|'\t'|'\\\''|'\\'|'\\0'))'\'';
BOOL: 'true' | 'false';

// Access modifiers
CLASS: 'class';
STRUCT: 'struct';
PUBLIC: 'public';
PRIVATE: 'private';
VIRTUAL : 'virtual';
ABSTRACT : 'abstract';

//CONST : 'const';
//IF : 'if';
//CLASS : 'class';
//VOID : 'void';
IDENTIFIER : [a-zA-Z_][a-zA-Z0-9_]*;

//OPPERATOR : '+' | '-' | '*' | '/' | '%';
//COMPARATOR : '==' | '!=' | '>' | '>=' | '<' | '<=' ;

// ignore include statements 
INCLUDE: '#include' .*? '\n' -> skip;

// ignore newlines, tabs, line breaks, page breaks
SPACES1: [ \t\n\r\f]+ -> skip;

// ignore comments single and multi line
//BUG: expecting newline means that a // comment can not be at the end of input, without trailing \n
COMMENT: '//' .*? '\n' -> skip;
//COMMENT: '//' .*? ('\n'|EOF) -> skip;
ML_COMMENT: '/*' .*? '*/' -> skip;

