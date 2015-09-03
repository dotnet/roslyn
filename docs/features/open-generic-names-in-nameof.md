### Language Proposal:  
Allow Open Generic Names (i.e. ```Dictionary<,>```) to be used within a ```nameof``` expression.

#### Grammar changes  
To implement this we first change the grammar in the following ways:

type-arguments:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;type-argument  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;type-arguments   ,   type-argument  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;+dim-separators_opt   

typeof-expression:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~typeof   (   type   )~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~typeof   (   unbound-type-name   )~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~typeof ( void )~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;typeof   (   typeof-type   ) 

+typeof-type:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;+void  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;+type

~~unbound-type-name:~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~identifier   generic-dimension-specifieropt~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~identifier   ::   identifier   generic-dimension-specifieropt~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~unbound-type-name   .   identifier   generic-dimension-specifieropt~~  

~~generic-dimension-specifier:~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~<   commasopt   >~~  

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~commas:~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~,~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~commas   ,~~




This grammar naturally precludes things like ```A<int,>```.  

We would then have normative text saying something akin to "it is an error to produce this last type of type-arguments unless the root grammar production is 'named-entity'".  This means that it would be an error to have 


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


> The last form of nameof-expression consists of a nameof keyword followed by a parenthesized unbound-type-name.  The meaning of an unbound-type-name is determined as follows:  

> •	Convert the sequence of tokens to a expression by replacing each generic-dimension-specifier with a type-argument-list having the same number of commas and the keyword object as each type-argument.  

> The meaning of the named-entity of a nameof-expression is the meaning of it as an expression; that is, either as a simple-name, a base-access or a member-access. However, where the lookup described in §7.6.3 and §7.6.5 results in an error because an instance member was found in a static context, a nameof-expression produces no such error. 

> Additionally, if an unbound-type-name is specified, and it contains dots, only the last identifier in the is allowed to refer to a non-type-member.  All other identifiers in the dotted name must refer to namespaces or types.

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

From the discussion at the LDM, ```D<>``` will produce the special "Unbound Generic Type" (the same one you get when you have ```typeof(D<>)```.  These unbound generic types are just the NamedTypeSymbol constructed with a special dummy "type argument".  These unbound generic types would have nested types (as they do today), but will also have the members of unconstructed type (just constructed with the dummy type argument).  This way, ```nameof(List<>.Add)``` will work.

As such, ```D<>.AProperty``` will have the type ```KeyValuePair<DummyUnboundTypeArgument,int>```
