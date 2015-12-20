Implicit type declarations:
-	Properties of anonymous types are considered nullable if their type is a reference type.
-	Element type of an array created by implicitly typed array creation expression is considered nullable if the type is a reference type.
-	Type of a var local is considered nullable if the type is a reference type.

For the purpose of flow analysis, result of expression can have three states:
-	Assumed to have not null value.
-	Possibly has null value.
-	Unknown nullability (no diagnostics is derived from this state). For example, this state can arise from usage of an API that hasn’t been annotated yet.  

Tracking state of 
-	Locals
-	Parameters
-	Fields of structures
-	Anonymous Type properties

The goal is to track state of the same entities, which are tracked by Definite Assignment, plus state of properties of Anonymous Types.  

Warn about possible null reference on 
-	An assignment to a local statically typed as not-nullable.
-	An assignment to a parameter statically typed as not-nullable.
-	An assignment to a field statically typed as not-nullable.
-	An assignment to a property statically typed as not-nullable.
-	An assignment to an indexer statically typed as not-nullable.
-	An assignment to an array element statically typed as not-nullable.
-	A member initializer for a member statically typed as not-nullable.
-	An argument passed to a parameter statically typed as not-nullable.
-	An argument (local, parameter, field, array element) statically typed as not-nullable passed as ref/out to a parameter statically typed as nullable.
-	A return expression if return type of the method is statically typed as not-nullable.
-	A receiver of a method/field/property/indexer access.
-	An array expression of an array access.

A local is considered to have a not null value if the local is statically typed as not-nullable, or the last value assigned to the field was not null.

A parameter is considered to have a not null value if the parameter is statically typed as not-nullable, or the last value assigned to the parameter was not null and the parameter is not ref/out.

A field is considered to have a not null value if the field is statically typed as not-nullable, or the field is tracked by the flow analysis and the last value assigned to the field was not null.

A property is considered to have a not null value if the property is statically typed as not-nullable, or the property is tracked by the flow analysis (readonly auto-property of a structure in its constructor, or a property of an Anonymous Type) and the last value assigned to the property was not null.

Result of an indexer access is considered to be a not null value if the indexer is statically typed as not-nullable.

Result of a method call is considered to be a not null value if its return type is statically typed as not-nullable.

Passing a tracked structure or a tracked Anonymous Type instance by reference invalidates accumulated tracking information for their members.

When a built-in or a user-defined operator ==/!= is used to compare an expression to a null value. 
For the purpose of flow analysis, a trackable expression is considered to be not null when == evaluates to false, or != evaluates to true. ~~If, according to flow analysis, the expression has a not null value before the operator is evaluated, a warning is reported that result of the comparison is always false (==), or always true (!=).~~ 

When expression is used as a left operand of a Null Coalescing Operator (??). 
For the purpose of flow analysis, a trackable expression is considered to be null before the right operand is evaluated. ~~If, according to flow analysis, the expression has a not null value before the operator is evaluated, a warning is reported that the operand is never null.~~ Result of the operator is considered to be not null for the purpose of the flow analysis if either operand is considered to be not null.

When expression is used as a receiver of a conditional access (?./?[]). 
For the purpose of flow analysis, a trackable expression is considered to be not null before the access is evaluated. ~~If, according to flow analysis, the expression has a not null value before the receiver is evaluated, a warning is reported that the receiver is never null.~~ Result of the operator is considered to be not null for the purpose of the flow analysis only if both, the receiver and the access are considered to be not null.
