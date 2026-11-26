grammar Fortran;

options { caseInsensitive = true; }

programUnit : mainProgram ;

//Program

mainProgram : (PROGRAM ID eos)? specPart execPart endProgram ;

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
    | INTENT '(' ID ')'
    | DIMENSION '(' dimSpec (',' dimSpec)* ')'
    | ID
    ;

entityDecl
    : ID ('(' dimSpec (',' dimSpec)* ')')? ('=' expr)?
    ;

dimSpec
    : expr ':' ( expr | '*' )
    | expr ':'
    | expr
    | ':'
    | '*'
    ;

//Statements
execPart : block ;

block : stmt* ;

stmt
    : actionStmt
    | ifConstruct
    | logicalIfStmt
    | doWhileConstruct
    | doConstruct
    | doInfiniteConstruct
    | unsupportedStmt
    ;

actionStmt
    : assignmentStmt
    | callStmt
    | printStmt
    | readStmt
    | writeStmt
    | exitStmt
    | cycleStmt
    | stopStmt
    | returnStmt
    | continueStmt
    ;

assignmentStmt : expr '=' expr eos ;

callStmt : CALL ID ('(' (expr (',' expr)*)? ')')? eos ;

printStmt : PRINT ioFormat (',' exprList)? eos ;

readStmt : READ ioFormat (',' exprList)? eos | READ '(' ioUnit ',' ioFormat ')' exprList? eos ;

writeStmt : WRITE '(' ioUnit ',' ioFormat ')' exprList? eos ;

ioUnit : '*' | expr ;

ioFormat : '*' | STRING_LIT ;

exitStmt     : EXIT ID? eos ;
cycleStmt    : CYCLE ID? eos ;
stopStmt     : STOP eos ;
returnStmt   : RETURN eos ;
continueStmt : CONTINUE eos ;

logicalIfStmt : IF '(' expr ')' actionStmt ;

ifConstruct
    : IF '(' expr ')' THEN eos block
      (ELSE_IF '(' expr ')' THEN eos block)*
      (ELSE eos block)?
      END_IF eos
    ;

doWhileConstruct : DO WHILE '(' expr ')' eos block END_DO eos ;

doConstruct : DO ID '=' expr ',' expr (',' expr)? eos block END_DO eos ;

doInfiniteConstruct : DO eos block END_DO eos ;

unsupportedStmt : (ID | INT_LIT) ~EOL* eos ;

exprList : expr (',' expr)* ;

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
IMPLICIT_NONE    : 'implicit' [ \t]+ 'none' ;

INTEGER   : 'integer' ;
REAL      : 'real' ;
LOGICAL   : 'logical' ;
CHARACTER : 'character' ;
DOUBLE_PRECISION : 'double' [ \t]+ 'precision' ;
PARAMETER        : 'parameter' ;
DIMENSION        : 'dimension' ;
INTENT           : 'intent' ;

IF               : 'if' ;
THEN             : 'then' ;
ELSE_IF          : 'else' [ \t]* 'if' ;
ELSE             : 'else' ;
END_IF           : 'end' [ \t]* 'if' ;
DO               : 'do' ;
WHILE            : 'while' ;
END_DO           : 'end' [ \t]* 'do' ;
CALL             : 'call' ;
PRINT            : 'print' ;
READ             : 'read' ;
WRITE            : 'write' ;
EXIT             : 'exit' ;
CYCLE            : 'cycle' ;
STOP             : 'stop' ;
RETURN           : 'return' ;
CONTINUE         : 'continue' ;
END              : 'end' ;

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