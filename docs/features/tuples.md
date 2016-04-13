
A lot of details and motivation for the feature is given in 
[https://github.com/dotnet/roslyn/issues/347](https://github.com/dotnet/roslyn/issues/347)

Current design notes:
https://github.com/dotnet/roslyn/issues/10429

NOTE: The goal of this document is capture what is being implemented. As design evolves, the document will undergo adjustments. 
Changes in design will be applied to this document as the changes are implemented.

-----------------------------------

Tuple types
-----------

Tuple types would be introduced with syntax very similar to a parameter list:

```C#
public (int sum, int count) Tally(IEnumerable<int> values) { ... }
    
var t = Tally(myValues);
Console.WriteLine($"Sum: {t.sum}, count: {t.count}");   
 ```
 
The syntax `(int sum, int count)` indicates an anonymous struct type with public fields of the given names and types.

With no further syntax additions to C#, tuple values could be created as
``` c#
var t1 = new (int sum, int count) (0, 1);
var t2 = new (int sum, int count) { sum = 0, count = 0 };
```

Tuple literals
--------------

``` c#
var t1 = (0, 2); 				// infer tuple type from values
var t2 = (sum: 0, count: 1);	// infer tuple type from names and values
```

Creating a tuple value of a known target type:
``` c#
public (int sum, int count) Tally(IEnumerable<int> values) 
{
    var s = 0; var c = 0;
    foreach (var value in values) { s += value; c++; }
    return (s, c); // target typed to (int sum, int count)
}
```

Note that specifying field names is optional, however when names are provided, all fields must be named. Duplicate names are disallowed.


``` c#
var t1 = (sum: 0, 1);		// error! some fields are named some are not.
var t2 = (sum: 0, sum:1);	// error! duplicate names.
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

``` c#
var t = (sum: 0, count: 1);	
t.sum   = 1;				// sum   is the name for the field #1 
t.Item1 = 1;				// Item1 is the name of underlying field #1 and is also avaialable

var t1 = (0, 1);			// tuple omits the field names.
t.Item1 = 1;				// underlying field name is still avaialable 

t.ToString() 				// ToString on the underlying tuple type is called.

System.ValueTuple<int, int> vt = t;  // identity conversion 
(int foo, int bar) t2 = vt;  		 // identity conversion

```

Because of the dual nature of tuples, it is not allowed to assign field names that overlap with preexisting member names of the underlying type.
The only exception is the use of predefined "Item1", "Item2",..."ItemN" at corresponding position N, since that would not be ambiguous.

``` c#
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

``` c#
var t = (sum: 0, count: 1);	

System.ValueTuple<int, int> vt = t;  // identity conversion 
(int foo, int bar) t2 = vt;  		 // identity conversion

t2.foo = 1;
```
That said, if you have an element name at *one* position on one side of a conversion, and the same name at *another* position on the other side. That would be indication that the code almost certainly contains a bug:

``` c#
(int sum, int count) foo ()
{
	return (count: 1, sum 3); // warning!! 
}	
```
Overloading, Overriding, Hiding
--------------
For the purpose of Overloading Overriding Hiding, tuples of the same types and lengths as well as their underlying ValueTuple types are considered equivalent. All other differences are immaterial.

```C#
class Base 
{
	virtual void foo(ValueTuple<int, int> arg){...}
}
class Derived : Base
{
	override void foo((int c, int d) arg){...} // valid override, signatures are equivalent
}
class Derived2 : Derived 
{
	override void foo((int c1, int c) arg){...} // also valid, warning on possible misuse of name 'c' 
}

class InvalidOverloading 
{
	virtual void foo((int c, int d) arg){...}
	virtual void foo((int x, int y) arg){...}			// invalid overload, signatures are eqivalent
	virtual void foo(ValueTuple<int, int> arg){...}		// also invalid
}

```
Name erasure at runtime 
--------------
Importantly, the tuple field names aren't part of the runtime representation of tuples, but are tracked only by the compiler. 

As a result, the field names will not be available to a 3rd party observer of a tuple instance - such as reflection or dynamic code.

In alignment with the identity conversions, a boxed tuple does not retain the names of the fields and will unbox to any tuple type that has the same element types in the same order.

```C#
object o = (a: 1, b: 2);    		 // boxing conversion 
var t = ((int foo, int bar))o;		 // unboxing conversion
```

Target typing
--------------

A tuple literal is "target typed" when used in a context specifying a tuple type. What that means is that the tuple literal has a "conversion from expression" to any tuple type, as long as the element expressions of the tuple literal have an implicit conversion to the element types of the tuple type.

``` c#
(string name, byte age) t = (null, 5); // Ok: the expressions null and 5 convert to string and byte
```

In cases where the tuple literal is not part of a conversion, a tuple is used by its "natural type", which means a tuple type where the element types are the types of the constituent expressions. Since not all expressions have types, not all tuple literals have a natural type either:

``` c#
var t = ("John", 5);  						//   Ok: the type of t is (string, int)
var t = (null, 5);    						//   Error: tuple expression doesn't have a type because null does not have a type
((1,2, null), 5).ToString();    	    	//   Error: tuple expression doesn't have a type

ImmutableArray.Create((()=>1, 1));        	//   Error: tuple expression doesn't have a type because lambda does not have a type
ImmutableArray.Create((Func<int>)()=>1, 1); //   ok
```

A tuple literal may include names, in which case they become part of the natural type:

``` c#
var t = (name: "John", age: 5); // The type of t is (string name, int age)
t.age++;						// t has field named age.
```

A successful conversion from tuple expression to tuple type is classified as _ImplictTuple_ conversion, unless tuple's natural type matches the target type exactly, in such case it is an _Identity_ conversion.

```C#
void foo((int x, int y) arg){...};
void foo((object x, object y) arg){...};

foo((1, 2));   			// first overload is used. Identity conversion is better than implicit conversion. 
foo(("hi", "hello"));   // second overload is used. Implicit tuple conversion is better than no conversion. 
```

Target typing will "see through" nullable target types. A successfull conversion from tuple expression to a nullable tuple type is classified as _ImplicitNullable_ conversion.

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
void foo((int x, Func<(int, int)>) arg){...};
void foo((int x, Func<(int, byte)>) arg){...};

foo((1, ()=>(2, 3)));         // the first overload is used due to "exact match" rule

``` 

