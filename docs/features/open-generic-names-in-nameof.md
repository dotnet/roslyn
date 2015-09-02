### Language Proposal:  
Allow Open Generic Names (i.e. ```Dictionary<,>```) to be used within a ```nameof``` expression.

#### Grammar changes  
To implement this we first change the grammar in the following ways:

named-entity:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;simple-name  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;named-entity-target   .   identifier   type-argument-listopt  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;+unbound-type-name  

Note: this is what we would need to effectively change on the grammar side.  We would have to explain in depth that, unlike typeof, you can refer to a member as the last name in the un "unbound-type-name".

We can either do this with normative text.  Or we can have a parallel set of grammar rules.  I leave it to Mads to decide:)

The net effect would be to allow/disallow the following grammatically: 

```c#
nameof(List<>);
nameof(List<>.Enumerator);
nameof(List<>.Add);
nameof(List<int,>);             // Error.  you must provide all type arguments, or no type arguments.
nameof(List<>[]);               // Error.  unconstructed type must be at top level.
nameof(List<List<>>);           // Error.  unconstructed type must be at top level.
nameof(List<int>.Inner<>);      // Error.  you must provide all type arguments, or no type arguments.
nameof(List<>.Inner<int>);      // Error.  you must provide all type arguments, or no type arguments.
```

#### Semantic Changes  

The explanation for this grammar change would be as follows:


> The last second form of nameof-expression consists of a nameof keyword followed by a parenthesized unbound-type-name.  The meaning of an unbound-type-name is determined as follows:  

> •	Convert the sequence of tokens to a expression by replacing each generic-dimension-specifier with a type-argument-list having the same number of commas and the keyword object as each type-argument.  

> The meaning of the named-entity of a nameof-expression is the meaning of it as an expression; that is, either as a simple-name, a base-access or a member-access. However, where the lookup described in §7.6.3 and §7.6.5 results in an error because an instance member was found in a static context, a nameof-expression produces no such error


##### Semantic Model Details  
This change produces some interesting cases for the semantic model.  For example (as raised by Neal) what should the SemanticModel produce for the following:

```C#
class KeyValuePair<A,B> 
{
}
class G<X,Y>
{
    public KeyValuePair<X,Y> AProperty { get; }
}
class D<T> : G<T, int>
{
}
class Test {
  void M(string s = nameof(D<>.AProperty)) {
  }
}
```

While the compiler will simply replace ```nameof(D<>.AProperty)``` with the constant "AProperty", the semantic model can be used to query for information about ```D<>``` and ```D<>.AProperty```.  What sort of results should one get back for these?  

To me, i don't think there is much difficulty in these scenarios.  In the example, provided, the Symbol you get back for ```D<>.AProperty``` is simply the property symbol for ```G<T,int>.AProperty```.  In this case the first type argument for ```G``` is ```D<T>```'s first *type parameter*.  Similarly, the return type of that property symbol would be ```KeyValuePair<A,B>``` constructed with ```D<T>```'s first type parameter, and ```Int32```.
