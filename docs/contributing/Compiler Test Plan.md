﻿This document provides guidance for thinking about language interactions and testing compiler changes.

# General concerns:
- Completeness of the specification as a guide for testing (is the spec complete enough to suggest what the compiler should do in each scenario?)
- Other external documentation
- Backward and forward compatibility (interoperation with previous and future compilers, each in both directions)
- Error handling/recovery (missing libraries, including missing types in mscorlib; errors in parsing, ambiguous lookup, inaccessible lookup, wrong kind of thing found, instance vs static thing found, wrong type for the context, value vs variable)
- BCL and other customer impact
- Determinism
- Loading from metadata (source vs. loaded from metadata)
- Public interface of compiler APIs (including semantic model APIs listed below):
    - GetDeclaredSymbol 
    - GetEnclosingSymbol 
    - GetSymbolInfo 
    - GetSpeculativeSymbolInfo 
    - GetTypeInfo 
    - GetSpeculativeTypeInfo 
    - GetMethodGroup 
    - GetConstantValue 
    - GetAliasInfo 
    - GetSpeculativeAliasInfo 
    - LookupSymbols 
    - AnalyzeStatementsControlFlow 
    - AnalyzeStatementControlFlow 
    - AnalyzeExpressionDataFlow 
    - AnalyzeStatementsDataFlow 
    - AnalyzeStatementDataFlow 
    - ClassifyConversion
- VB/F# interop
- Performance and stress testing
 
# Type and members
- Access modifiers (public, protected, internal, protected internal, private), static modifier
- Parameter modifiers (ref, out, params)
- Attributes (including security attribute)
- Generics (type arguments, constraints, variance)
- Default and constant values
- Partial classes
- Literals
- Enum (implicit vs. explicit underlying type)
- Expression trees
- Iterators
- Initializers (object, collection, dictionary)
- Array (single- or multi-dimensional, jagged, initializer)
- Expression-bodied methods/properties/...
- Extension methods
- Partial method
- Named and optional parameters
- String interpolation
- Properties (read-write, read-only, write-only, auto-property, expression-bodied)
- Interfaces (implicit vs. explicit interface member implementation)
- Delegates
- Multi-declaration
- NoPIA
- Dynamic
 
# Code
- Operators (see Eric's list below)
- Lambdas (capture of parameters or locals, target typing)
- Execution order
- Target typing (var, lambdas, integrals)
- Conversions (boxing/unboxing)
- Nullable (wrapping, unwrapping)
- Overload resolution, override/hide/implement (OHI)
- Inheritance (virtual, override, abstract, new)
- Anonymous types
- Tuples
- Local functions
- Unsafe code
- LINQ
- Constructors, properties, indexers, events, operators, and destructors.
- Async
- Lvalues: the synthesized fields are mutable 
    - Ref / out parameters
    - Compound operators (+=, /=, etc ..) 
    - Assignment exprs
- Ref returns
- `this = e;` in `struct` .ctor

# Misc
- reserved keywords (sometimes contextual)
- pre-processing directives
- COM interop
- modopt and modreq
- ref assemblies

# Testing in interaction with other components
Interaction with IDE, Debugger, and EnC should be worked out with relevant teams. A few highlights:
- IDE
    - Colorization
    - Typing experience and dealing with incomplete code
    - Intellisense (squiggles, dot completion)
    - "go to" and renaming
    - More: [IDE Test Plan](https://github.com/dotnet/roslyn/blob/master/docs/contributing/IDE%20Test%20Plan.md)

- Debugger / EE
    - Stepping, setting breakpoints
    - Displaying Locals/Autos windows
        - Type and Value display
        - Expanding instance members
        - Locals/parameters/this in closure classes
        - Attributes on locals and expressions (e.g.: [Dynamic], [TupleElementNames])
    - Compiling expressions in Immediate/Watch windows or hovering over an expression
    - Compiling expressions in [DebuggerDisplay("...")]
	- Assigning values in Locals/Autos/Watch windows
 
- Edit-and-continue

# Eric's cheatsheet

## Statements 
```
{ … }  
;   
label : … 
T x = whatever; 
M(); 
++x; 
x++; 
--x; 
x--; 
new C(); 
if (…) … else … 
switch(…) { … case (…) when (…): … } 
while(…) … 
do … while(…); 
for( … ; … ; … ) … 
foreach(…) …
goto … ; 
throw … ; 
return … ; 
try  { … } catch (…) when (…) { … } finally { … } 
checked { … } 
unchecked { … } 
lock(…) … 
using (…) … 
yield return …; 
yield break; 
break; 
continue; 
```

## Expression classifications 
  
Every expression can be classified as exactly one of these: 
  
- Value 
- Variable 
- Namespace 
- Type 
- Method group 
- Null literal 
- Anonymous function 
- Property 
- Indexer 
- Event 
- Void-returning method call 
- Array initializer (\*) 
- __arglist (\*)  

(\*) Technically not an expression according to the spec. 
  
Note that only values, variables, properties, indexers and events have a type. 
  
## Variable classifications 
  
A variable is a storage location. These are all the different ways to refer to a storage location: 

- Static field 
- Instance field 
- Array element 
- Formal param, value 
- Formal param, ref 
- Formal param, out 
- Local variable 
- Pointer dereference 
- __refvalue 

## Operators 

```
x.y 
f( ) 
a[e] 
x++ 
x-- 
new X() 
typeof(T) 
default(T)
default 
checked(e) 
unchecked(e) 
delegate ( ) { } 
+x 
-x 
!x 
~x 
++x 
--x 
(X)x 
x * y 
x / y 
x % y 
x + y 
x - y 
x << y 
x >> y 
x < y 
x > y 
x <= y 
x >= y 
x is X 
x as X 
x == y 
x != y 
x & y 
x ^ y 
x | y 
x && y 
x || y 
x ?? y 
x ? : y : z 
x = y 
x *= y 
x /= y 
x %= y 
x += y 
x -= y 
x <<= y 
x >>= y 
x &= y 
x ^= y 
x |= y 
x => { } 
sizeof( ) 
*x 
& x 
x->y 
e is pattern
await x 
__arglist( ) 
__refvalue( x, X ) 
__reftype( x ) 
__makeref( x ) 
```

## Explicit conversions 
  
- Numeric 
- Enum 
- Nullable 
- Reference  
- Unboxing 
- Dynamic 
- Type parameter 
- User defined 
- Pointer to pointer 
- Pointer to integral 
- Integral to pointer 
- Tuple literal
- Tuple

## Implicit conversions 
  
- Identity 
- Numeric 
- Literal zero to enum (we actually do constant zero, not literal zero) 
- Nullable 
- Null literal 
- Reference 
- Boxing 
- Dynamic 
- Constant 
- Type parameter 
- User defined 
- Anonymous function 
- Method group  
- Pointer to void pointer 
- Null literal to pointer
- Interpolated string
- Tuple literal
- Tuple

## Types 
  
- Class 
- Interface 
- Delegate 
- Struct 
- Enum 
- Nullable 
- Pointer 
- Type parameter 
  
## Members
  
- Class 
- Struct 
- Interface 
- Enum 
- Delegate 
- Namespace 
- Property 
- Event 
- Constructor 
- Destructor 
- Method 
- Interface method 
- Field 
- User-defined indexer 
- User-defined operator 
- User-defined conversion 
  
## Metadata table numbers / token prefixes 
 
If you look at a 32 bit integer token as a hex number, the first two digits identify the “table number” and the last six digits are an offset into that table. The table numbers are: 
```
00 Module 
01 TypeRef 
02 TypeDef 
03 FieldPtr 
04 Field 
05 MethodPtr 
06 Method 
07 ParamPtr 
08 Param 
09 InterfaceImpl 
0A MemberRef 
0B Constant 
0C CustomAttr 
0D FieldMarshal 
0E DeclSecurity 
0F ClassLayout 
10 FieldLayout 
11 StandAloneSig 
12 EventMap 
13 EventPtr 
14 Event 
15 PropertyMap 
16 PropertyPtr 
17 Property 
18 MethodSemantics 
19 MethodImpl 
1A ModuleRef 
1B TypeSpec 
1C ImplMap 
1D FieldRVA 
1E ENCLog 
1F ENCMap 
20 Assembly 
21 AssemblyProcessor 
22 AssemblyOS 
23 AssemblyRef 
24 AssemblyRefProcessor 
25 AssemblyRefOS 
26 File 
27 ExportedType 
28 ManifestResource 
29 NestedClass 
2A GenericParam 
2B MethodSpec 
2C GenericConstraint 
```
