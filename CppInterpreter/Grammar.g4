grammar Grammar;

program : topLevelStatement*;

topLevelStatement : functionDefinition
		          | variableDefinition ';';

statement : expression ';'
		  | functionDefinition
		  | variableDefinition ';'
		  | ifStmt
		  | whileStmt
		  | forStmt
		  | doWhileStmt
		  | 'return' expression? ';';


functionDefinition : (typeIdentifier | TYPE_VOID) ident=IDENTIFIER '(' parameterList ')' block;

parameterList : (typeIdentifierUsage varIdentifier)? (',' typeIdentifierUsage varIdentifier)* ; 

variableDefinition : typeIdentifierUsage varIdentifier ('=' expression);

ifStmt : 'if' '(' cond=expression ')' innerBlock elseStmt?;

elseStmt : 'else' (ifStmt | innerBlock);


whileStmt : 'while' '(' cond=expression ')' innerBlock;

forStmt : 'for' '(' setup=statement? ';' cond=expression? ';' incr=expression? ')' innerBlock;

doWhileStmt : 'do' block 'while' '(' cond=expression ')' ';';


expression : func=expression '(' param=expression (',' param=expression)* ')' 
		   | subscript=expression '[' param=expression ']'
		   // TODO: Member access
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
		   | assignment
		   | atom
		   | literal; //TODO

assignment : varIdentifier '=' expression;

atom : IDENTIFIER;

literal : intLiteral
		| str=STRING 
		| char=CHAR 
		| bool=BOOL;

intLiteral :  int=INTEGER 
           |  hex=INTEGER_HEX 
           |  bin=INTEGER_BIN; 

varIdentifier : ident=IDENTIFIER;

typeIdentifierUsage : typeIdentifier ref='&'?; // & should actually be part of name?
typeIdentifier : int=TYPE_INT
			   | str=TYPE_STRING
			   | bool=TYPE_BOOL
//			   | void=TYPE_VOID
			   | ident=IDENTIFIER;

innerBlock : block | statement ';' | ';';

block : '{' statement* '}';

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

