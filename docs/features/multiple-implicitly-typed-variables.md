### High Level Language Proposal:  
Allow users to provide multiple declarators in an implicitly typed local ('var') declaration, as long as all of them have the same type.

Discussion:  
Back when we added implicitly typed variables, we considered allowing multiple declarators in a single declaration.  i.e.:  

```c#
var x = 0, y = 0;
```

We decided against this because there was a lot of disagreement (including from the community) as to what the following should mean:

```c#
var x = 0, y = 1.0;
```

Opinions differed on if this would be equivalent to:

```c#
double x = 0, y = 1.0;
```

or

```c#
int x = 0; double y = 1.0;
```

Because of this large split in opinions, we decided to disallow this completely.  

However, this is a very restrictive state to have ended up in.  We can relax things a bit, without worrying about the above case being a problem.  This is possible by allowing multiple variable declarations, but with the restriction that all of them have the same type.  Because of this restriction, the above case would still be disallowed as 'x' would have the type 'int' and 'y' would have the type 'double'.  

To accomplish this, we change the language specification like so:

> In the context of a local variable declaration, the identifier var acts as a contextual keyword (§2.4.3).When the local-variable-type is specified as var and no type named var is in scope, the declaration is an implicitly typed local variable declaration, whose type is inferred from the type of the associated initializer expression. Implicitly typed local variable declarations are subject to the following restrictions:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;~~•	The local-variable-declaration cannot include multiple local-variable-declarators.~~  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;•	The local-variable-declarator must include a local-variable-initializer.  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;•	The local-variable-initializer must be an expression.  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;•	The initializer expression must have a compile-time type.  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;•	The initializer expression cannot refer to the declared variable itself  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;•	+ All initializer expressions must have the same compile-time type.  

It is worth noting that the compile-time type of different declarators are not used to contextually type other declarators.  For example the following are all illegal:

```c#
var x = "", y = null;                            // 'null' has no compile time type.  This is not legal.
var x = (Func<int,int>)(a => a), y = b => b;     // 'b => b' has no compile time type.  This is not legal.
var x = (Action)() => {}, y = Console.WriteLine; // 'Console.WriteLine' has no compile time type.  This is not legal.
```

However, despite this, it *is* possible for one variable declarator to influence another.  For example:

```c#
var x = 0, y = x;  
// This is legal.  The compile time type of 'x' is Int32.  The compile time type of 
// 'y' is also Int32.  Because 'x' is definitely assigned by the time it is read for 'y'.
```

Similar strange, but legal, forms are:

```c#
var a = 0, b = Foo(out a);
```
  
  
#### Grammar changes  
None
  
  
#### Language Model changes
Node (except for less errors than before).
