grammar Grammar;

program : statement*;

//declaration : 

statement : funcDefinition 
		  | terminalStastement ';';
terminalStastement : varDecl 
				   | funcDecl;


expression : statement; 


funcDecl : funcReturnType funcReturnMod ident=IDENTIFIER '(' varDeclSingle* ')';
funcReturnMod : reference | (pointer CONST?)*;
funcReturnType : void=VOID | typeUsage;
//funcParameters : '';
funcDefinition : funcDecl (block | statement);


varDeclAssignment : varDecl assignment;
varAssignment : identifier assignment;


varDecl : typeUsage varDeclIdent (',' varDeclIdent)*;
varDeclSingle : typeUsage varDeclIdent;
varDeclIdent : (pointer const_ptr=CONST?)? IDENTIFIER arrayDecl?;

arrayDecl : '[' intLiteral ']';

typeUsage : const=CONST typeIdentifier 
          | typeIdentifier const=CONST 
          | typeIdentifier;



identifier : pointer? IDENTIFIER;

assignment : '=' expression END;

typeIdentifier : TYPE_INT 
			   | TYPE_STRING 
			   | TYPE_BOOL 
			   | IDENTIFIER;


block : '{' statement* '}';

constExpression : literal;

literal : int=intLiteral
		| string=STRING 
		| char=CHAR; 

intLiteral : int=INTEGER 
		   | hex=INTEGER_HEX
           | bin=INTEGER_BIN;

reference : AMP;
pointer : STAR;



//Tokens


TYPE_INT : 'int';
TYPE_STRING : 'string';
TYPE_BOOL : 'bool';

INTEGER: [0-9]+;
INTEGER_HEX: '0x'[0-9a-fA-F]+;
INTEGER_BIN: '0b'[0-1_]+;
STRING: '"'(~('"')|(' '|'\b'|'\f'|'r'|'\n'|'\t'|'\\"'|'\\'))*'"';
CHAR: '\''(~('\'')|(' '|'\b'|'\f'|'r'|'\n'|'\t'|'\\\''|'\\'))'\'';

CONST : 'const';
IF : 'if';
CLASS : 'class';
VOID : 'void';
IDENTIFIER : [a-zA-Z_][a-zA-Z0-9_]*;

STAR : '*';
AMP : '&';
END : ';';

SPACES1: [ \t\n\r\f]+ -> skip;
COMMENT: '//' .*? '\n' -> skip;
ML_COMMENT: '/*' .*? '*/' -> skip;
