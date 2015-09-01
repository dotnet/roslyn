### Language Proposal:  
Allow Open Generic Names (i.e. ```Dictionary<,>```) to be used within a ```nameof``` expression.

#### Grammar changes  
To implement this we first change the grammar in the following ways:

typeof-expression:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~typeof   (   type   )~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;typeof   (   type   )  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;+typeof   (   unbound-type   )  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~typeof   (   unbound-type-name   )~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;typeof ( void )
  
~~unbound-type-name:~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~identifier   generic-dimension-specifieropt~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~identifier   ::   identifier   generic-dimension-specifieropt~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~unbound-type-name   .   identifier   generic-dimension-specifieropt~~

~~generic-dimension-specifier:~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~&lt;   commasopt   &gt;~~  

~~commas:~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~,~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~commas   ,~~


unbound-type:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;type

type-argument:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;type  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&lt;empty&gt;


The net effect of these grammar changes is that anywhere where we used to allow a generic-name, we now allow 'ommitted-type-arguments' to be present in the 'type-argument-list'.  i.e. *grammatically* the following are allowed:

```c#
List<> a;                       // should be error
Dictionary<,> d;                // should be error
Dictionary<int,> d;             // should be error
new Dictionary<,>();            // should be error
Method<>();                     // should be error
Method<,>();                    // should be error
Method<int,>();                 // should be error
typeof(List<>);
typeof(List<>.Enumerator);
typeof(List<>.Add);
nameof(List<>);
nameof(List<>.Enumerator);
nameof(List<>.Add);
typeof(List<>[]);
typeof(List<List<>>);           // should be error
nameof(List<List<>>);           // should be error
nameof(List<int>.Inner<>);      // should be error
nameof(List<>.Inner<int>);      // should be error
```

Note: This is nearly identical to how the C# compiler *today* parses things.  It already uses unbound-type-arguments for error recovery purposes.   As such, these grammar changes need almost no changes to the C# parser.

#### Semantic Changes  

Of course, many of these we do not want to actually be allowed by the language as a whole.  This is where the "unbound-type" grammar-production comes in.  On the static-semantic side of things we  effectively want to say is that in all places in the langauge where we see "type" explicitly used, it is not ok to have an "omitted-type-argument".  The only times it is ok to have an ommitted type argument, is when the production starts with "unbound-type" or within a 'real' "nameof" expression.

This will prevent these use cases:
```c#
List<> a;
Dictionary<,> d;
Dictionary<int,> d;
new Dictionary<,>();
Method<>();
Method<,>();
Method<int,>();
```

We still need to prevent cases like ```typeof(List<List<>>)``` though.  To enforce this we will require that type-argument-lists cannot contain omitted-type-arguments if they are contained within a type-argument-list with non-ommitted-type-argument.  Similarly, a type name cannot have a mix of type-arguments-lists with/without in a fully qualified name.  (Note: the compiler already does this today so it can error in this case for 'typeof').


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
