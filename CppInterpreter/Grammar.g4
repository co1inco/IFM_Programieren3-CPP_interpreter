grammar Grammar;

program : topLevelStatement*;

replStatement : statement | expression;

topLevelStatement : functionDefinition
		          | variableDefinition ';'
		          | class;

// classes
class : defaultVis=(CLASS | STRUCT) typeIdentifier classInheritance? classBlock ';'; 

classBlock: '{' classBlockStatement* '}';

classBlockStatement : classMemberMod functionDefinition 
					| classMemberMod variableDefinition ';'
					| classConstructor
					| classDestructor
					| pub=PUBLIC ':' 
					| prv=PRIVATE ':';

classMemberMod : virtual=VIRTUAL?;

classInheritance : ':' classInheitanceIdent (',' classInheitanceIdent)*;  

classInheitanceIdent : vis=(PRIVATE|PUBLIC)? typeIdentifierUsage;

classConstructor : ident=IDENTIFIER '(' parameterList ')' block;

classDestructor : '~' ident=IDENTIFIER '(' ')' block;

// Statements
statement : returnStmt ';'
		  | breakStmt ';'
		  | continueStmt ';'
 		  | functionDefinition
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

forStmt : 'for' '(' setup=statement? ';' cond=expression? ';' incr=expression? ')' innerBlock;

doWhileStmt : 'do' block 'while' '(' cond=expression ')' ';';

breakStmt : 'break';

continueStmt : 'continue';

innerBlock : block | statement | ';';

block : '{' statement* '}';

// Expressions
expression : '(' brace=expression ')'
 		   | func=expression '(' funcParameters? ')' 
		   | subscript=expression '[' param=expression ']'
		   // TODO: Member access
		   | memberExpr=expression '.' memberAtom=atom 
		   | unary=('++' | '--') expression 
           | unary=('+' | '-' | '!' | '~' ) expression 
		   | left=expression binop=('*' | '/' | '%') right=expression
		   | left=expression binop=('+' | '-') right=expression
		   | left=expression comp=('<' | '<=' | '>' | '>=') right=expression 
		   | left=expression comp=('==' | '!=') right=expression 
		   | left=expression bit='&' right=expression 
		   | left=expression bit='^' right=expression 
		   | left=expression bit='|' right=expression 
		   | left=expression logic=('&&' | '||') right=expression 
		   | left=expression assign='=' right=expression
		   | atom
		   | literal; //TODO


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


TYPE_INT : 'int';
TYPE_STRING : 'string';
TYPE_BOOL : 'bool';
TYPE_VOID : 'void';

INTEGER: [0-9]+;
INTEGER_HEX: '0x'[0-9a-fA-F]+;
INTEGER_BIN: '0b'[0-1_]+;
STRING: '"'(~('"')|(' '|'\b'|'\f'|'r'|'\n'|'\t'|'\\"'|'\\'|'\\0'))*'"';
CHAR: '\''(~('\'')|(' '|'\b'|'\f'|'r'|'\n'|'\t'|'\\\''|'\\'|'\\0'))'\'';
BOOL: 'true' | 'false';

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

INCLUDE: '#include' .*? '\n' -> skip;

SPACES1: [ \t\n\r\f]+ -> skip;
COMMENT: '//' .*? '\n' -> skip;
ML_COMMENT: '/*' .*? '*/' -> skip;

