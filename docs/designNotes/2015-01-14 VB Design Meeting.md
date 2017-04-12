VB Design Meeting 2015-01-14
============================

# Overload Resolution for String Interpolation

Discussion thread for these notes can be found at https://github.com/dotnet/roslyn/issues/46.

# Summary

* Exactly like C#, we will consider an interpolated string to be like a string in most cases such as overload resolution and searches for user-defined/intrinsic conversion. This implies that what the IDE intellisense shows after $"hello".| should be the same as that for regular strings.

* We wrote out some gnarly type-inference test cases for overload resolution and array literals with interpolated strings, and explained the expected output.

* We would like both C# and VB to give more actionable error messages if they end up binding to a factory Create method whose return type isn't right

* We reaffirmed intended behavior for full-width characters in interpolated strings

# Overload resolution for string interpolation

We want interpolated strings to be easily used as both String and FormattableString:
```vb
  Dim x As String = $"hello {p}"              ' works fine
  Dim x As FormattableString = $"hello {p}"   ' works fine
  Dim x = $"hello {p}"                        ' works fine, and infers x As String
```

The question is about overload resolution... Which candidate should it prefer? Or should it be an ambiguity error?
```vb
  Sub f(x As FormattableString) : End Sub
  Sub f(x As String) : End Sub

  f($"hello {p}")    ' ... which does it pick?
```

One important principle is that if there's an existing API `Sub f(x As String)` then consumers MUST be able to call it with `f($"hello {p}")`.


Another question is: if there's a language intrinsic conversion from string, does that conversion also apply to interpolated strings? e.g.
```vb
   Dim x As Char() = "hello people"  ' works fine
   Dim x As Char() = $"hello {x}"    ' should this also work?
```
And separately, if there's a user-defined intrinsic conversion from string, does that conversion also apply to interpolated strings?

(In C# the intention is that both should work. Have to verify that we've covered that in unit tests.)



## API DESIGN Proposal 1a
"Some library APIs really want consumers to use FormattableString because it is safer or faster. The API that takes string and the API that takes FormattableString actually do different things and hence shouldn't be overloaded on the same name. Library authors will want to lead people to use interpolated strings, hence it should have a shorter name."
```vb
   Sub ExecuteQueryUnsafe(s As String) ...
   Sub ExecuteQuery(s As FormattableString) ...

   Sql.ExecuteQueryUnsafe(GetRegValueEx(path))
   Sql.ExecuteQueryUnsafe("from p in people select p.Name")
   Sql.ExecuteQuery($"from p in {x} select p.Name")
```

Q. If they do different things, then wouldn't you want an ambiguity error here?


## API DESIGN Proposal 1b
"In other library APIs, strings and FormattableStrings are equally fine; overloads make sense; we should prefer string overload because it will be more efficient"
```vb
   Sub WriteLine(s As String) ...

   Console.WriteLine("hello everyone")
   Console.WriteLine($"hello {fred}")
```

Q. Isn't it an *antipattern* for an API to have both String and FormattableString if they just do the same thing?

A. Well, maybe, but it could be valid and useful to overload on both String and IFormattable. (Or an overload of both String and Object and then do a TryCast to IFormattable).


## Proposal 2
"I don't like Safe/Unsafe. How about these names? ..."
```vb
   Sub ExecuteQuery(s As String) ...
   Sub ExecuteQuery(s As FormattableString) ...

   Sql.ExecuteQuery(GetRegValueEx(path))
   Sql.ExecuteQuery("from p in people select p.Name")
   Sql.ExecuteQueryWithFormat($"from p in {x} select p.Name")
```


## API DESIGN Proposal 3
"Someone will start with ExecuteQuery, and when they change the argument to $ then they won't see or understand the differently-named method. So let's pick the FormattableString overload which is most likely to be safe."
```vb
   Sub ExecuteQuery(s As String) ...
   Sub ExecuteQuery(s As FormattableString) ...

   Sql.ExecuteQuery("from p in people select p.Name")  ' picks String overload
   Sql.ExecuteQuery(GetRegValueEx(path))  ' picks String overload
   Sql.ExecuteQuery($"from p in {x} select p.Name")  ' picks FormattableString overload
```
 
Q. What about the following consequence? Imagine an API has existed whose behavior is to format its string in a particular culture
```vb
   Sub f(x As IFormattable)
   f($"hello {p}")
```
And later on down the line someone adds a new overload that takes string
```vb
   Sub f(x As String)
```
Then the user's call will change behavior upon recompilation.


## RESOLUTION
We generally believe that libraries will mostly be written with different API names for methods which do different things. Therefore overload resolution differences between FormattableString and String don't matter, so string might as well win. Therefore we should stick with the simple principle that an interpolated string *is* a string. End of story.

Implication: in intellisense `$"hello".|` will show extension methods off String, but will *NOT* show extension methods off FormattableString.

Implication: both intrinsic and user-defined conversions that apply to string will also apply to interpolated string

Implication: overload resolution will prefer String over FormattableString candidates when given an interpolated string argument.

Implication: type inference works as follows.
```vb
Sub f(Of T)(x As T)
f($"hello {p}")
' then it picks string. (it has *contributed* string as a candidate)

Sub f(Of T)(x As T, y As T)
f($"hello {p}", CType(Nothing, FormattableString))
' Then it gets two candidates, "String" and "FormattableString"
' In most of the language (other than array literals), it checks whether
' the *type* of each argument can convert to the candidate type.
' In this case it will give an error.
```

Implication: if you have an array literal that contains an interpolated string expression
```vb
   Dim x = {$"hello", CType(Nothing, IFormattable)}
```
then this will pick "Object Assumed" in Option Strict Off, and give an error in Option Strict On. The reason is that there is no dominant type between the candidate types "String" and "IFormattable". (There's no widening/identity conversion from one to the other, and there is a narrowing conversion from each to the other).


## About the factory method that interpolation strings use

The language conversion rules bake in knowledge of `System.IFormattable` and `System.FormattableString` for their knowledge of widening conversions.

The compiler emits a call to a factory method when there is an interpolated string in source code. The factory method looks like this. There might be a whole family of overloaded Create methods.
```vb
System.Runtime.CompilerServices.FormattableStringFactory
   Function Create(...) As ...
```
The compiler separates the interpolated-string into a format string and a comma-separated list of expressions for the holes which it classifies as values before generating a call to `Create(fmtstring, expr1, expr2, ...)`. It will rely on normal VB overload resolution to pick the best Create method. This leaves the implementors of the factory free to do lots of nice optimizations.

The question is, what return type do we expect from the Create method?

Option1: We could bake in the requirement that the factory method gives back a System.FormattableString, and this type must implement System.IFormattable, and do this as a pre-filter prior to doing overload resolution of the Create() overload family.

Option2: Or we could merely invoke the method, and do a cast of the return type to IFormattable/FormattableString depends on what the user asked for. But then...
* Do we give a warning if it has the [Obsolete] attribute?
* Do we give a warning if it is narrowing?
* What if it picks a Sub() ?

Option3: Just do plain ordinary overload resolution, and if there were ANY errors or warnings, them emit them. In addition, if there were any errors (not just warnings that happened to be WarnAsError'd), then additionally report an error message at the same location "The factory is malformed". Precedent: we do this for queries. [Note: this message might appear or disappear if you change option strict on/off in your file].

Option4: As with Option3 but enforcing it to use Option Strict On for its overload resolution and its conversion to IFormattable/FormattableString.

RESOLUTION: Option3.



Q. What about delegate relaxation level of $"hello" to FormattableString/IFormattable ?
```vb
Sub f(lambda as Func(Of FormattableString))
Sub f(lambda as Func(Of String))
f(Function() $"hello")
```
RESOLUTION: From the principle above, we'd like this to pick the String overload. The way to accomplish this is to classify the lambda conversion as DelegateRelaxationLevelReturnWidening.



Q. What about full-width characters?
e.g. does $"{{" give the non-full-width string "{" even if the two source characters had different widths?
e.g. can you write $"{p}" where the open is wide and the close is normal width?
e.g. there is no escape to put a full-width {, similar to how there's no escape to put a full-width " ?

RESOLUTION: Yes that's all fine.



---

These are the workitems left to do...

C#: additionally report error message about bad factory
C#: verify (add a test case) that user-defined and intrinsic conversions on string really are used for interpolated strings.
VB: change array literal dominant type stuff
VB: all the dominant type stuff
VB: fix up delegate relaxation level to be widening (and write test for it)

