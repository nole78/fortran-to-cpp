grammar Fortran;

options { caseInsensitive = true; }

programUnit : mainProgram ;

//Program

mainProgram : (PROGRAM ID eos)? specPart endProgram ;

endProgram : (END_PROGRAM | END) ID? eos ; 

//Declarations
specPart : implicitStmt? declaration* ;

implicitStmt : IMPLICIT_NONE eos ;

declaration : typeSpec (',' attr)* '::'? entityDecl (',' entityDecl)* eos ;

typeSpec 
     : INTEGER kindSelector?
     | REAL kindSelector?
     | DOUBLE_PRECISION
     | LOGICAL kindSelector?
     | CHARACTER kindSelector?
     ;

kindSelector : '(' typeParam (',' typeParam)* ')' ;

typeParam : (ID '=')? (expr | '*') ;

attr
    : PARAMETER
    | INTENT '(' ID ID? ')'
    | DIMENSION '(' dimSpec (',' dimSpec)* ')'
    | ID
    ;

entityDecl
    : ID ('(' dimSpec (',' dimSpec)* ')')? ('=' expr)?
    ;

dimSpec
    : expr ':' expr
    | expr ':'
    | expr
    | ':'
    | '*'
    ;

eos : EOL+ ;

//Expressions
expr 
     : '(' expr ')'
     | <assoc=right> expr '**' expr
     | expr mulOp expr
     | addOp expr
     | expr addOp expr
     | expr '//' expr
     | expr relOp expr
     | NOT expr
     | expr AND expr
     | expr OR expr
     | expr eqvOp expr
     | callName '(' (subscript (',' subscript)*)? ')'
     | ID
     | INT_LIT
     | REAL_LIT
     | STRING_LIT
     | LOGICAL_LIT
     ;

callName
    : ID
    | REAL
    ;

subscript
    : expr
    | expr? ':' expr? (':' expr)?
    ;

//Operators
mulOp : '*' | '/' ;
addOp : '+' | '-' ;
relOp : EQ | NE | LT | LE | GT | GE ;
eqvOp: EQV | NEQV ;

//Key words
PROGRAM : 'program';
END_PROGRAM      : 'end' [ \t]* 'program' ;
END              : 'end' ;
IMPLICIT_NONE    : 'implicit' [ \t]+ 'none' ;

INTEGER   : 'integer' ;
REAL      : 'real' ;
LOGICAL   : 'logical' ;
CHARACTER : 'character' ;
DOUBLE_PRECISION : 'double' [ \t]+ 'precision' ;
PARAMETER        : 'parameter' ;
DIMENSION        : 'dimension' ;
INTENT           : 'intent' ;


//Literals
INT_LIT   : [0-9]+ KIND? ;

REAL_LIT       
     : [0-9]+ '.' [0-9]* EXPONENT? KIND?
     | '.' [0-9]+ EXPONENT? KIND?
     | [0-9]+ EXPONENT KIND?
     ;

STRING_LIT     
     : '\'' ( ~['\r\n&] | '\'\'' | STR_CONT | '&' )* '\''
     | '"' ( ~["\r\n&]  | '""'   | STR_CONT | '&')* '"'
     ;

LOGICAL_LIT : '.true.' | '.false.' ;

fragment KIND  : '_' [a-z0-9]+ ;
fragment EXPONENT : [ed] [+\-]? [0-9]+ ;
fragment LINE_ENDING : [ \t]* ('!' ~[\r\n]*)? '\r'? '\n' ;
fragment STR_CONT : '&' LINE_ENDING [ \t]* '&'? ;

//Logical operators
AND  : '.and.' ;
OR   : '.or.' ;
NOT  : '.not.' ;
EQV  : '.eqv.' ;
NEQV : '.neqv.' ;

//Relational operators
EQ   : '==' | '.eq.' ;
NE   : '/=' | '.ne.' ;
LT   : '<'  | '.lt.' ;
LE   : '<=' | '.le.' ;
GT   : '>'  | '.gt.' ;
GE   : '>=' | '.ge.' ;

//Identifiers
ID   : [a-z] [a-z0-9_]* ;

//Comments, whitespace characters, etc.
CONT   : '&' LINE_ENDING [ \t]* '&'? -> skip ;
COMMENT: '!' ~[\r\n]* -> skip ; 
WS     : [ \t]+ -> skip ;
EOL    : ('\r'? '\n' | ';' )+ ;