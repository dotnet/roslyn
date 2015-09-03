### High Level Language Proposal:  
Allow Open Generic Names (i.e. ```Dictionary<,>```) to be used within a ```nameof``` expression.

#### Grammar changes  
To support this proposal this we first change the grammar in the following ways.  ```+``` is used to indicate a grammar additiona.  ```strike-through``` is used to indicate a grammer removal:

type-arguments:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;type-argument  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;type-arguments   ,   type-argument  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;+commas_opt   

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

commas (already exists):  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;,  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;commas   ,


This grammar naturally precludes things like ```A<int,>```.  It also means that open generic types (like ```A<>``` and ```A<,,,>```) can now show up naturally in nameof and typeof expressions.  (Note: this formulation of the grammar also matches how our existing parser and language model represent this construct.  Something which i think is a bonus :)).

One issue is that we now find that gramatically certain constructs are allowed.  For example, ```new List<>()``` would be allowed.

To prevent this problem we add normative text saying that it is an error to have the ```commas_opt``` version of ```type-arguments``` if the root grammar production is not ```named-entity``` or ```typeof-type```.  The former will ensure these forms are only legal in ```nameof``` expressions, and the latter will ensure these forms are only legal in ```typeof``` expressions.

Another piece of normative text would be to say that in a typeof/nameof context, all ```type-arguments``` would all have to agree on having or not-having type-arguments.  i.e. you could not have ```A<>.B<int>``` or ```A<int>.B<>```.  This would also prevent things like ```List<List<>>```.

One final piece of text would be to explicitly disallow this form of ```type-arguments``` if referenced from any other usages of ```type``` in the grammar.  So this would preclude you from being able to do things like ```typeof(List<>[])``` or ```typeof(List<>?)```.   Note: we could allow this.  But this preserves existing compiler behavior.  I also don't think there is value in these forms, so continuing to disallow it seems sensible.

The net effect would be to allow/disallow the following grammatically.  
Note: this is not an exhaustive list.  
Note: what is/isn't allowed in "typeof" should be completely unchanged. 

```c#
nameof(List<>);
nameof(List<>.Enumerator);
nameof(List<>.Add);
nameof(alias::List<>);
List<> a;                       // Error.  Not in a nameof/typeof context
nameof(List<int,>);             // Error.  you must provide all type arguments, or no type arguments.
nameof(List<>[]);               // Error.  open type cannot be nested in another type.
nameof(List<List<>>);           // Error.  You cannot have both open and non-open types.
nameof(List<int>.Inner<>);      // Error.  You cannot have both open and non-open types.
nameof(List<>.Inner<int>);      // Error.  You cannot have both open and non-open types.
```

#### Semantic Changes  

The semantic side of this grammar change would be as follows:

> The last form of ```type-arguments``` consists of a (possibly empty) comma list.  This form will produce an unbound-type-name.  The meaning of an unbound-type-name is determined as follows:  

> •	Convert the sequence of comma tokens into a ```type-arguments``` construct with the ```type-arguments``` having the same number of commas and the keyword object as each type-argument.  

> In a type-of expression:  
•	Evaluate the resulting type-name, while ignoring all type parameter constraints.  
•	The unbound-type-name resolves to the unbound generic type associated with the resulting constructed type (§4.4.3).  
•	The result of the typeof-expression is the System.Type object for the resulting unbound generic type

> In a nameof-expression:  
•	Evaluate the resulting simple-name, while ignoring all type parameter constraints.  
•	The meaning of the named-entity of a nameof-expression is the meaning of it as an expression; that is, either as a simple-name, a base-access or a member-access. However, where the lookup described in §7.6.3 and §7.6.5 results in an error because an instance member was found in a static context, a nameof-expression produces no such error. 

> Additionally, in a nameof-expression, if an unbound-type-name is specified, and it contains dots, only the last identifier in the dotted name is allowed to refer to a non-type-member.  All other identifiers in the dotted name must refer to namespaces or types.

This last bit of text is to prevent users from doing things like ```nameof(Foo<>.StringProperty.Length)```.  We feel there is little benefit to this (as you can already do ```nameof(String.Length)```.  In essence you can specify a complicated open type.  But then you can access at most one member off of it.

##### Language Model Details  

Syntactically, the language model will not change at all.  The parser/syntax-API already supports seeing open generic types anywhere.  It simply produces nodes with a GenericNameSyntax that has a TypeArgumentList with OmittedTypeArgument nodes in the type positions.  We will continue to do this.  This means that you will get very similar trees in a ```nameof``` expression for ```nameof(A.B.C<,,>)``` and ```nameof(A.B.C<int,int,int>)```.  Both of these arguments to ```nameof``` will be member access expressions.  The nodes will be nearly the same, except that the former will have ```OmittedTypeArgument```s for its ```TypeArgumentList``` while the latter will have ```PredefinedType```s for its.  


Semantically the language model will change slightly.  Today, if you write ```nameof(List<>)``` and you attempt to get Symbol/Type info for ```List<>```, you will get the ```List<T>``` *original definition type* (along with diagnostics stating this is not allowed).   This will change moving forward.  Now, you will get a normal ```Unbound Generic Type```, with no diagnostics.  This is the same symbol  you get if you for ```List<>``` in ```typeof(List<>)```.  This unifies the symbolic information between ```nameof``` and ```typeof```, which i think is sensible.


Also, "Unbound Types" will be slightly different than they are today.  Namely they will have inheritance.  This is so that we get consistent behavior between code like ```nameof(List<int>.Select)``` and ```nameof(List<>.Select)```.  If we didn't have inheritance for unbound types, then we would not thnk that ```List<>``` was an ```IEnumerable<>``` and we would not find this extension method suitable.

