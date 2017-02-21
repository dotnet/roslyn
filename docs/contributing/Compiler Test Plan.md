



Lvalues: the synthesized fields are mutable 
- Ref / out parameters
- Compound operators (+=, /=, etc ..) 
- Assignment exprs

- Interaction with overload resolution

General concerns:
- Completeness of the specification as a guide for testing (is the spec complete enough to suggest what the compiler should do in each scenario?)
- Other external documentation
- Backward and forward compatibility (interoperation with previous and future compilers, each in both directions)
- Error handling/recovery (Missing libraries, including missing types in mscorlib; errors in parsing)
- Error handling for semantic errors (e.g. ambiguous lookup, inaccessible lookup, wrong kind of thing found, instance vs static thing found, wrong type for the context, value vs variable)
- BCL and other customer impact
- Determinism
- Loading from metadata (source vs. loaded from metadata)
- Public interface of compiler APIs (including semantic model APIs listed below):
 - SemanticModel.GetDeclaredSymbol 
 - SemanticModel.GetEnclosingSymbol 
 - SemanticModel.GetSymbolInfo 
 - SemanticModel.GetSpeculativeSymbolInfo 
 - SemanticModel.GetTypeInfo 
 - SemanticModel.GetSpeculativeTypeInfo 
 - SemanticModel.GetMethodGroup 
 - SemanticModel.GetConstantValue 
 - SemanticModel.GetAliasInfo 
 - SemanticModel.GetSpeculativeAliasInfo 
 - SemanticModel.LookupSymbols 
 - SemanticModel.AnalyzeStatementsControlFlow 
 - SemanticModel.AnalyzeStatementControlFlow 
 - SemanticModel.AnalyzeExpressionDataFlow 
 - SemanticModel.AnalyzeStatementsDataFlow 
 - SemanticModel.AnalyzeStatementDataFlow 
 - SemanticModel.ClassifyConversion
- VB/F# interop
 
Type and members:
- Access modifiers (public, protected, internal, protected internal, private), static modifier
- Parameter modifiers (ref, out, params)
- Attributes (including security attribute)
- Generics (type arguments, constraints, variance)
- default value
- partial classes
- literals
- enum (implicit vs. explicit underlying type)
- epression trees
- Iterators
- Initializers (object, collection, dictionary)
- array (single- or multi-dimensional, jagged, initilalizer)
- Expression-bodied methods/properties/...
- Extension methods
- Partial method
- Named and optional parameters
- String interpolation
- Properties (read-write, read-only, write-only, auto-property, expression-bodied)
- Interfaces (implicit vs. explicit interface member implementation)
- delegates
- Multi-declaration
- NoPIA
 
Code:
- Operators (see Eric's list)
- lambdas (capture of parameters or locals, target typing)
- execution order
- Target typing (var, lambdas, integrals)
- conversions (boxing/unboxing)
- nullable (wrapping, unwrapping)
- OHI
- inheritance (virtual, override, abstract, new)
- overload resolution
- Anonymous types
- Unsafe code
- LINQ
- constructors, properties, indexers, events, operators, and destructors.
- Async
 
 
Misc:
- reserved keywords (sometimes contextual)
- pre-processing directives
- COM interop

 
Interaction with Debugger:

1. typing in immediate/watch window (that also covers hovering over a variable)
2. displaying locals (that also covers autos)
 stepping, setting breakpoints, evaluating variables
 
Interaction with IDE: 

1. Colorization
2. Typing experience
3. Intellisense (squiggles, dot completion)
4. "go to" and renaming
5. More: https://github.com/dotnet/roslyn/issues/8389

- interaction with IDE in incomplete code scenarios (e.g. while typing)

- edit-and-continue

Performance and stress testing


Eric's cheatsheet:

Statements 
  
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
switch(…) { … } 
while(…) … 
do … while(…); 
for( … ; … ; … ) … 
foreach(…) … 
goto … ; 
throw … ; 
return … ; 
try  { … } catch (…) { … } finally { … } 
checked { … } 
unchecked { … } 
lock(…) … 
using (…) … 
yield return …; 
yield break; 
break; 
continue; 
  
Expression classifications 
  
Every expression can be classified as exactly one of these: 
  
Value 
Variable 
Namespace 
Type 
Method group 
Null literal 
Anonymous function 
Property 
Indexer 
Event 
Void-returning method call 
Array initializer (*) 
__arglist (*) 
  
(*) Technically not an expression according to the spec. 
  
Note that only values, variables, properties, indexers and events have a type. 
  
Variable classifications 
  
A variable is a storage location. These are all the different ways to refer to a storage location: 
  
Static field 
Instance field 
Array element 
Formal param, value 
Formal param, ref 
Formal param, out 
Local variable 
Pointer dereference 
__refvalue 
  
Operators 
  
x.y 
f( ) 
a[ ] 
x++ 
x--  
new X() 
typeof( ) 
default( ) 
checked( ) 
unchecked( ) 
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
await x 
__arglist( ) 
__refvalue( x, X ) 
__reftype( x ) 
__makeref( x ) 
  
Explicit conversions 
  
Numeric 
Enum 
Nullable 
Reference  
Unboxing 
Dynamic 
Type parameter 
User defined 
Pointer to pointer 
Pointer to integral 
Integral to pointer 
  
Implicit conversions 
  
Identity 
Numeric 
Literal zero to enum (we actually do constant zero, not literal zero) 
Nullable 
Null literal 
Reference 
Boxing 
Dynamic 
Constant 
Type parameter 
User defined 
Anonymous function 
Method group  
Pointer to void pointer 
Null literal to pointer 
  
Types 
  
Class 
Interface 
Delegate 
Struct 
Enum 
Nullable 
Pointer 
Type parameter 
  
Things that can be members of another thing 
  
Class 
Struct 
Interface 
Enum 
Delegate 
Namespace 
Property 
Event 
Constructor 
Destructor 
Method 
Interface method 
Field 
User-defined indexer 
User-defined operator 
User-defined conversion 
  
Metadata table numbers / token prefixes 
 
If you look at a 32 bit integer token as a hex number, the first two digits identify the “table number” and the last six digits are an offset into that table. The table numbers are: 
  
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
