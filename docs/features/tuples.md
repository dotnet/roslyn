
Quickstart guide for tuples (C# 7.0 and Visual Basic 15)
------------------------------------
1. Install VS2017
2. Start a C# or VB project
3. Add a reference to the `System.ValueTuple` package from NuGet
![Install the ValueTuple package](img/install-valuetuple.png)
4. Use tuples in C#:

```C#
public class C
{
        public static (int code, string message) Method((int, string) x) 
        { 
                return x;
        }

        public static void Main()
        {
                var pair1 = (42, "hello");
                System.Console.Write(Method(pair1).message);
        
                var pair2 = (code: 43, message: "world");
                System.Console.Write(pair2.message);
        }
}
```
    
5. Or use tuples in VB:

```VB
Public Class C
        Public Shared Function Method(x As (Integer, String)) As (code As Integer, message As String)
                Return x
        End Function

        Public Shared Sub Main()
                Dim x = (42, "hello")
                System.Console.Write(C.Method(x).message)
        
                Dim pair2 = (code:=43, message:="world")
                System.Console.Write(pair2.message)
        End Sub
End Class
```

6. Use deconstructions (C# only): see the [deconstruction page](deconstruction.md)

Without the `System.ValueTuple` package from NuGet, the compiler will produce an error:
``error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported``

Design
------
The goal of this document is capture what is being implemented. As design evolves, the document will undergo adjustments. 
Changes in design will be applied to this document as the changes are implemented.


Tuple types
-----------

Tuple types would be introduced with syntax very similar to a parameter list:

```C#
public (int sum, int count) Tally(IEnumerable<int> values) { ... }
    
var t = Tally(myValues);
Console.WriteLine($"Sum: {t.sum}, count: {t.count}");   
 ```
 
The syntax `(int sum, int count)` indicates an anonymous data structure with public fields of the given names and types, also referred to as *tuple*.

With no further syntax additions to C#, tuple values could be created as
```C#
var t1 = new (int sum, int count) (0, 1);
var t2 = new (int sum, int count) { sum = 0, count = 0 };
var t3 = new (int, int) (0, 1);     // field names are optional    
```
Note that specifying field names is optional; however, when names are provided, all fields must be named. Duplicate names are disallowed.

Tuple literals
--------------

```C#
var t1 = (0, 2);				// infer tuple type from values
var t2 = (sum: 0, count: 1);	// infer tuple type from names and values
```

Creating a tuple value of a known target type:
```C#
public (int sum, int count) Tally(IEnumerable<int> values) 
{
    var s = 0; var c = 0;
    foreach (var value in values) { s += value; c++; }
    return (s, c); // target typed to (int sum, int count)
}
```

Note that specifying field names is optional, however when names are provided, all fields must be named. Duplicate names are disallowed.


```C#
var t1 = (sum: 0, 1);		// error! Some fields are named, some are not.
var t2 = (sum: 0, sum: 1);	// error! duplicate names.
```

Duality with underlying type
--------------

Tuples map to underlying types of particular names - 

```
System.ValueTuple<T1, T2>
System.ValueTuple<T1, T2, T3>
...
System.ValueTuple<T1, T2, T3,..., T7, TRest>
```

In all scenarios tuple types behave exactly like underlying types with only additional optional enhancement of the more expressive field names given by the programmer.

```C#
var t = (sum: 0, count: 1);	
t.sum   = 1;				// sum   is the name for the field #1 
t.Item1 = 1;				// Item1 is the name of underlying field #1 and is also available

var t1 = (0, 1);			// tuple omits the field names.
t.Item1 = 1;				// underlying field name is still available 

t.ToString()				// ToString on the underlying tuple type is called.

System.ValueTuple<int, int> vt = t;	// identity conversion 
(int moo, int boo) t2 = vt;			// identity conversion

```

Because of the dual nature of tuples, it is not allowed to assign field names that overlap with preexisting member names of the underlying type.
The only exception is the use of predefined "Item1", "Item2",..."ItemN" at corresponding position N, since that would not be ambiguous.

```C#
var t =  (ToString: 0, ObjectEquals: 1);		// error: names match underlying member names
var t1 = (Item1: 0, Item2: 1);					// valid
var t2 = (misc: 0, Item1: 1);					// error: "Item1" was used in a wrong position
```

Example of underlying tuple type implementation:
https://github.com/dotnet/roslyn/blob/features/tuples/docs/features/ValueTuples.cs
To be replaced by the actual tuple implementation in FX Core.


Identity conversion
--------------

Element names are immaterial to tuple conversions. Tuples with the same types in the same order are identity convertible to each other or to and from corresponding underlying ValueTuple types, regardless of the names.

```C#
var t = (sum: 0, count: 1);	

System.ValueTuple<int, int> vt = t;  // identity conversion 
(int moo, int boo) t2 = vt;  		 // identity conversion

t2.moo = 1;
```
That said, if you have an element name at *one* position on one side of a conversion, and the same name at *another* position on the other side. That would be indication that the code almost certainly contains a bug:

```C#
(int sum, int count) moo ()
{
	return (count: 1, sum: 3); // warning!! 
}	
```
Overloading, Overriding, Hiding
--------------
For the purpose of Overloading Overriding Hiding, tuples of the same types and lengths as well as their underlying ValueTuple types are considered equivalent. All other differences are immaterial.

When overriding a member it is permitted to use tuple types with same or different field names than in the base member. 

A situation where same field names are used for non-matching fields between base and derived member signatures, a warning is reported by the compiler.  

```C#
class Base 
{
	virtual void M1(ValueTuple<int, int> arg){...}
}
class Derived : Base
{
	override void M1((int c, int d) arg){...} // valid override, signatures are equivalent
}
class Derived2 : Derived 
{
	override void M1((int c1, int c) arg){...} // also valid, warning on possible misuse of name 'c' 
}

class InvalidOverloading 
{
	virtual void M1((int c, int d) arg){...}
	virtual void M1((int x, int y) arg){...}			// invalid overload, signatures are eqivalent
	virtual void M1(ValueTuple<int, int> arg){...}		// also invalid
}

```
Name erasure at runtime 
--------------
Importantly, the tuple field names aren't part of the runtime representation of tuples, but are tracked only by the compiler. 

As a result, the field names will not be available to a 3rd party observer of a tuple instance - such as reflection or dynamic code.

In alignment with the identity conversions, a boxed tuple does not retain the names of the fields and will unbox to any tuple type that has the same element types in the same order.

```C#
object o = (a: 1, b: 2);    		 // boxing conversion 
var t = ((int moo, int boo))o;		 // unboxing conversion
```

Target typing
--------------

A tuple literal is "target typed" when used in a context specifying a tuple type. What that means is that the tuple literal has a "conversion from expression" to any tuple type, as long as the element expressions of the tuple literal have an implicit conversion to the element types of the tuple type.

```C#
(string name, byte age) t = (null, 5); // Ok: the expressions null and 5 convert to string and byte
```

In cases where the tuple literal is not part of a conversion, a tuple is used by its "natural type", which means a tuple type where the element types are the types of the constituent expressions. Since not all expressions have types, not all tuple literals have a natural type either:

```C#
var t = ("John", 5);  						//   Ok: the type of t is (string, int)
var t = (null, 5);    						//   Error: tuple expression doesn't have a type because null does not have a type
((1,2, null), 5).ToString();    	    	//   Error: tuple expression doesn't have a type

ImmutableArray.Create((()=>1, 1));        	//   Error: tuple expression doesn't have a type because lambda does not have a type
ImmutableArray.Create(((Func<int>)(()=>1), 1)); //   ok
```

A tuple literal may include names, in which case they become part of the natural type:

```C#
var t = (name: "John", age: 5); // The type of t is (string name, int age)
t.age++;						// t has field named age.
```

A successful conversion from tuple expression to tuple type is classified as _ImplictTuple_ conversion, unless tuple's natural type matches the target type exactly, in such case it is an _Identity_ conversion.

```C#
void M1((int x, int y) arg){...};
void M1((object x, object y) arg){...};

M1((1, 2));   			// first overload is used. Identity conversion is better than implicit conversion. 
M1(("hi", "hello"));   // second overload is used. Implicit tuple conversion is better than no conversion. 
```

Target typing will "see through" nullable target types. A successful conversion from tuple expression to a nullable tuple type is classified as _ImplicitNullable_ conversion.

```C#
((int x, int y, int z)?, int t)? SpaceTime()
{
	return ((1,2,3), 7); 	// valid, implicit nullable conversion
}
``` 

Overload resolution and tuples with no natural types.
--------------

A situation may arise during overload resolution where multiple equally qualified candidates could be available due to a tuple argument without natural type being implicitly convertible to the corresponding parameters.

In such situations overload resolution employs _exact match_ rule where arguments without natural types are recursively matched to the types of the corresponding parameters in terms of constituent structural elements.

Tuple expressions are no exception from this rule and the _exact match_ rule is based on the natural types of the constituent tuple arguments.

The rule is mutually recursive with respect to other containing or contained expressions not in a possession of a natural type.

```C#
void M1((int x, Func<(int, int)>) arg){...};
void M1((int x, Func<(int, byte)>) arg){...};

M1((1, ()=>(2, 3)));         // the first overload is used due to "exact match" rule

``` 

Conversions
--------------

Tuple types and expressions support a variety of conversions by "lifting" conversions of the elements into overal _tuple conversion_.
For the classification purpose, all element conversions are considered recursively. For example: To have an implicit conversion, all element expressions/types must have implicit conversions to the corresponding element types.

Typele conversions are *Standard Conversions* and therefore can stack with user-defined operators to form user-defined conversions.

A tuple conversion can be classified as a valid instance conversion for an extension method invocation as long as all element conversions are applicable as instance conversions.

Language grammar changes
---------------------
This is based on the [ANTLR grammar](https://raw.githubusercontent.com/ljw1004/csharpspec/gh-pages/csharp.g4) from Lucian.

For tuple type declarations:

```ANTLR
struct_type
    : type_name
    | simple_type
    | nullable_type
    | tuple_type // new
    ; 
    
tuple_type
    : '(' tuple_type_element_list ')'
    ;
    
tuple_type_element_list
    : tuple_type_element ',' tuple_type_element
    | tuple_type_element_list ',' tuple_type_element
    ;
    
tuple_type_element
    : type identifier?
    ;
```

For tuple literals:

```ANTLR
literal
    : boolean_literal
    | integer_literal
    | real_literal
    | character_literal
    | string_literal
    | null_literal
    | tuple_literal // new
    ;

tuple_literal
    : '(' tuple_literal_element_list ')'
    ;

tuple_literal_element_list
    : tuple_literal_element ',' tuple_literal_element
    | tuple_literal_element_list ',' tuple_literal_element
    ;

tuple_literal_element
    : ( identifier ':' )? expression
    ;
```

Note that because it is not a constant expression, a tuple literal cannot be used as default value for an optional parameter.

Open issues:
-----------

- [ ] Provide more details on semantics of tuple type declarations, both static (Type rules, constraints, all-or-none names, can't be used on right-hand-side of a 'is', ...) and dynamic (what does it do at runtime?).
- [ ] Provide more details on semantics of tuple literals, both static (new kind of conversion from expression, new kind of conversion from type, all-or-none, scrambled names, underlying types, underlying names, listing the members of this type, what it means to access, ) and dynamic (what happens when you do this conversion?).
- [ ] Exactly matching expression

References:
-----------

A lot of details and motivation for the feature is given in 
[Proposal: Language support for Tuples](https://github.com/dotnet/roslyn/issues/347)

[C# Design Notes for Apr 6, 2016](https://github.com/dotnet/roslyn/issues/10429)


