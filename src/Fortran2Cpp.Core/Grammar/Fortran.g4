grammar Fortran;

options { caseInsensitive = true; }

programUnit : expr EOL* EOF ;

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
     | ID
     | INT_LIT
     | REAL_LIT
     | STRING_LIT
     | LOGICAL_LIT
     ;

//Operators
mulOp : '*' | '/' ;
addOp : '+' | '-' ;
relOp : EQ | NE | LT | LE | GT | GE ;
eqvOp: EQV | NEQV ;

//Key words
INTEGER   : 'integer' ;
REAL      : 'real' ;
LOGICAL   : 'logical' ;
CHARACTER : 'character' ;
DOUBLE_PRECISION : 'double' [ \t]+ 'precision' ;

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