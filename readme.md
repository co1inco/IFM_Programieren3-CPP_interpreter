

# Parser / Interpreter design

## Parser

 * Antlr Parser: (Text input -> Antlr Parse tree)
 * AstParser: (Antlr Parse Tree -> Ast)
 * CppParser:
    - Stage 1: User type declaration (Classes / structs)
    - Stage 2: TopLevel & ClassLevel variable and function declarations 
    - Stage 3: Function bodies and initializers (ie. everything else)  
   -> Semantische Analyse und generation eines Interpreters 
   
## Interpreter

**3 basis typen**

### Funktionen ```ICppFunction```

Basis für alle Arten von Funktionen.
Statische / Generische implementationen für die Implementation von basis funktionen.  
`CppUserFunction` wird dynamisch aus Funktionsdefinitionen generiert.


### Values ``ICppValue``

Instanz eines Typs. Sind die Symbole des Scopes.


### Typen ``ICppType``

Stellt einen Cpp typen dar. Enthält alle Metadaten (Name, Methoden, ...) über einen typen.


#### PrimitiveType / PrimitiveValue
Basis für simple typen (int, char, bool, void)

#### Callable 
Scope symbol für Funktionen. Enthält eine Liste von Funktionen. Dadurch werden Funktionsüberladungen möglich

#### UserType




*Note: Klassische Funktions- und Variablendeklarationen werden nicht unterstützt. Gemeint ist, dass nur der Deklarationsteil analysiert wird*