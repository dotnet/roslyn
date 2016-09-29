# Better Betterness

"Better Betterness" is a language change to C# 6, present in the Roslyn C# compiler that shipped as part of Visual Studio 2015 and later. This change is applied in the compiler no matter what language version is specified on the command-line. Consequently we apply these new rules even when you are using the compiler as a C# 5 compiler using `/langver:5`.

Here are relevant sections of the modified language spec:

### 7.5.3.3 Better conversion from expression
Given an implicit conversion C<sub>1</sub> that converts from an expression E to a type T<sub>1</sub>, and an implicit conversion C<sub>2</sub> that converts from an expression E to a type T<sub>2</sub>, C<sub>1</sub> is a ***better conversion*** than C<sub>2</sub> if E does not exactly match T<sub>2</sub> and one of the following holds:
- E exactly matches T<sub>1</sub> (§7.5.3.4)
- T<sub>1</sub> is a better conversion target than T<sub>2</sub> (§7.5.3.5)

### 7.5.3.4 Exactly matching Expression
Given an expression E and a type T, E ***exactly matches*** T is one of the following holds:
- E has a type S, and an identity conversion exists from S to T
- E is an anonymous function, T is either a delegate type D or an expression tree type Expression<D> and one of the following holds:
  - An inferred return type X exists for E in the context of the parameter list of D (§7.5.2.12), and an identity conversion exists from X to the return type of D
  - Either E is non-async and  D has a return type Y or E is async and  D has a return type Task<Y>, and one of the following holds:
- The body of E is an expression that exactly matches Y
- The body of E is a statement block where every return statement returns an expression that exactly matches Y

### 7.5.3.5 Better conversion target
Given two different types T<sub>1</sub> and T<sub>2</sub>, T<sub>1</sub> is a ***better conversion target*** than T<sub>2</sub> if 
- An implicit conversion from T<sub>1</sub> to T<sub>2</sub> exists
- T<sub>1</sub> is either a delegate type D<sub>1</sub> or an expression tree type Expression&lt;D<sub>1</sub>>, T<sub>2</sub> is either a delegate type D<sub>2</sub> or an expression tree type Expression&lt;D<sub>2</sub>>, D<sub>1</sub> has a return type S<sub>1</sub> and one of the following holds: 
  - D<sub>2</sub> is void returning 
  - D<sub>2</sub> has a return type S<sub>2</sub>, and S<sub>1</sub> is a better conversion target than S<sub>2</sub> 
- T<sub>1</sub> is Task&lt;S<sub>1</sub>>, T<sub>2</sub> is Task&lt;S<sub>2</sub>>, and S<sub>1</sub> is a better conversion target than S<sub>2</sub>
- T<sub>1</sub> is S<sub>1</sub> or S<sub>1</sub>? where S<sub>1</sub> is a signed integral type, and T<sub>2</sub> is S<sub>2</sub> or S<sub>2</sub>? where S<sub>2</sub> is an unsigned integral type. Specifically:
  - S<sub>1</sub> is `sbyte` and S<sub>2</sub> is `byte`, `ushort`, `uint`, or `ulong`
  - S<sub>1</sub> is `short` and S<sub>2</sub> is `ushort`, `uint`, or `ulong`
  - S<sub>1</sub> is `int` and S<sub>2</sub> is `uint`, or `ulong`
  - S<sub>1</sub> is `long` and S<sub>2</sub> is `ulong`

##### Explanation

There no longer is a concept of “better conversion from type”.
 
Note that the new rules remove quite a bit of special casing that was based on the shape of the expression. With these new rules the expression matters only insofar as it is a “perfect match” for the type – which always takes priority. (This maintains the property that you can always choose a specific overload by casting the arguments to the precise types expected in that overload).
 
“Exactly matches” is primarily complicated by logic to “see through” lambdas and match the return type. The old rules would allow you to see through exactly 1 layer of lambdas (which seems completely arbitrary), whereas they are now generalized to any number:
 
- `7` exactly matches `int`
- `() => 7` exactly matches `Func<int>`
- `() => () => 7` exactly matches `Func<Func<int>>` but didn’t in earlier releases!!!
 
The rules that “see through” delegates and tasks now work regardless of the shape of the argument expression, where they used to require things being lambdas and async. So they are now factored into “Better conversion target” which ignores the expression. The rules for nullable were always lacking, and we’ve completed those – some of this was already “accidentally” implemented in the old compiler, most is new behavior.

##### Compatibility fix

In VS2015 Update 2 (Roslyn release 1.2), we detected and fixed a subtle incompatibility in the changed overload resolution rules. See https://github.com/dotnet/roslyn/issues/6560 for an example of affected code. To fix this, the second bullet of §7.5.3.5, above, is modified as follows:

> In case of a method group conversion (§6.6) for the corresponding argument, if a better conversion target (§7.5.3.5 Better conversion target), is a delegate type that is not compatible (§15.2 Delegate compatibility) with the single best method from the method group (§6.6 Method group conversions), then neither delegate type is better.

This explanation is necessarily a bit informal (e.g. there is no "corresponding argument" in §7.5.3.5); factoring it into the specification will require reorganizing the spec.

For reference:

### 15.2 Delegate compatibility 
A method or delegate M is compatible with a delegate type D if all of the following are true:
- D and M have the same number of parameters, and each parameter in D has the same ref or out modifiers as the corresponding parameter in M. 
- For each value parameter (a parameter with no ref or out modifier), an identity conversion (§6.1.1) or implicit reference conversion (§6.1.6) exists from the parameter type in D to the corresponding parameter type in M. 
- For each ref or out parameter, the parameter type in D is the same as the parameter type in M. 
- An identity or implicit reference conversion exists from the return type of M to the return type of D.

### 6.6 Method group conversions
...
- A conversion is considered to exist if the algorithm of §7.6.5.1 produces a single best method M having the same number of parameters as D.
...
