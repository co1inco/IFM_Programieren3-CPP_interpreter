grammar Grammar;

program : statement*;

//declaration : 

statement : varDecl ;

expression : statement; 




varDeclAssignment : varDecl assignment;
varAssignment : identifier assignment;

funcDecl : funcDeclType ident=IDENTIFIER '(' ')';
funcDeclType : (void=VOID (pointer CONST?)?) | (declType (pointer CONST? | AMP)?);
funcDefinition : funcDecl (block | statement);


varDecl : declType varDeclIdent (',' varDeclIdent)*;
varDeclIdent : (pointer const_ptr=CONST?)? IDENTIFIER;
declType : const=CONST typeidentifier 
		 | typeidentifier const=CONST 
		 | typeidentifier;


identifier : pointer? IDENTIFIER;

assignment : '=' expression END;

typeidentifier : TYPE_INT 
			   | TYPE_STRING 
			   | TYPE_BOOL 
			   | IDENTIFIER;


block : '{' statement* '}';

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
