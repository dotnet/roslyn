This document describes the new language features in Visual Basic 14, the next version of VB. All of these are implemented and available in Visual Studio 2015 but can also be used when targeting older versions of the .NET Framework (such as .NET 2.0) from within Visual Studio 2015.

* [String interpolation](#string-interpolation)
* [Null-conditional operators](#null-conditional-operators)
* [NameOf operator](#nameof-operator)
* [Read-only auto-properties](#read-only-auto-properties)
* [Multiline string literals](#multiline-string-literals)
* [Year-first Date literals](#year-first-date-literals)
* [Comments before implicit line continuation](#comments-before-implicit-line-continuation)
* [#Disable Warning and #Enable Warning directives](#disable-warning-and-enable-warning-directives)
* [Partial Modules and Interfaces](#partial-modules-and-interfaces)
* [TypeOf <_expression_> IsNot <_type_> operator](#typeof-expression-isnot-type-operator)
* [XML documentation comment improvements](#xml-documentation-comment-improvements)
* [Minor features and fixes](#minor-features-and-fixes)
 * [Smart name resolution](#smart-name-resolution)
 * [Read-only interface properties can now be implemented by read-write properties](#read-only-interface-properties-can-now-be-implemented-by-read-write-properties)
 * [#Region is now allowed inside of methods](#region-is-now-allowed-inside-of-methods)
 * [Overrides methods not implicitly include the Overloads modifier in metadata](#overrides-methods-now-implicitly-include-the-overloads-modifier-in-metadata)
 * [CObj is now permitted inside attribute arguments](#cobj-is-now-permitted-inside-attribute-arguments)
 * [Declaration and consumption of ambiguous methods from unrelated interfaces](#declaration-and-consumption-of-ambiguous-methods-from-unrelated-interfaces)

String interpolation
====================
String interpolation is an easier way of writing strings with expressions in them:

``` VB.NET
Dim s = $"hello {p.Name} you are {p.Height:0.00}m tall"

' is shorthand for
Dim s = String.Format("hello {0} you are {1:0.00}m tall", p.Name, p.Height)
```

String interpolation is often easier than String.Format because it saves you having to juggle the positional placeholders like {0} and {1}.

Note that, since it's shorthand for the specified call to String.Format, (1) string interpolation uses the current culture, and (2) it isn't a constant. However the compiler is at liberty to optimize string interpolation if it knows how String.Format will behave and if it can figure a faster way to do that (e.g. by avoiding boxing).

You can still format using the invariant culture by using the `System.FormattableString.Invariant` helper method.

``` VB.NET
Imports System.FormattableString
...
' This will still use the '.' as the decimal separator even in cultures that use ',' as the decimal separator
' such as de-DE (German - Germany)
Dim s = Invariant($"The price is {price:0.00}")
```

`FormattableString` is actually a general mechanism. Anyone can declare their methods as taking `FormattableString` instead of String and instead of directly calling String.Format the compiler will instead construct a `FormattableString` object containing the computed format string and values to interpolate and pass that to the method being called. The called method can then format the values in whatever ways it sees fit. So, for example, a method taking a SQL query string could choose to parameterize the query automatically instead of directly embedding the values in order to be more resilient to SQL-injection attacks, or a method constructing HTML from the arguments could first HTML escape the values before substitution.

If you want to embed an actual curly brace character write two curly braces in a row instead, e.g.

``` VB.NET
Dim s = $"{{ {guid} }}"
```

You can also write newline characters directly in an interpolated string and those newline characters will be included in the final output. However, newlines are only permitted inside the literal text portion of the interpolated string and not in the format-string portion of an interpolation:

``` VB.NET
WriteLine($"User:
{My.User.Name}") ' Legal.

WriteLine($"The time is {Date.Now:yyyy-MM-dd
HH:mm:ss}") ' Not legal.
```

Null-conditional operators
==========================
This new operator is a convenient shorthand for the many occasions when you have to check for null:

``` VB.NET
Dim x = customer.Address?.Country

' is shorthand for
Dim _temp = customer.Address
Dim x = If(_temp IsNot Nothing, _temp.Country, Nothing)
```

You can also use it in a sequence and you can mix with the regular `.` operator, e.g. `a?.b.c?.d`. It reads left-to-right. Any null value before a `?.` will just stop the sequence short, and any null value before a `.` will raise a `NullReferenceException` as usual.

For a string value like `customer.Address?.Name`, if it stops short then the result is a null value typed as `String`; likewise for other reference types. For an integer value like `customer?.Age`, if it stops short, then the result is the null value typed as a nullable Integer (`Integer?`); likewise for other value types.

You can use it in other handy ways, e.g.

``` VB.NET
If customer?.Age > 50 Then ...
    ' branch taken ONLY IF customer is non-null and is older than 50

Dim name = If(customer?.Name, "blank")  ' a default name if customer is null
```

In addition to `?.` there are several other null-checking operators in VB 14:

**Method invocation**
``` VB.NET
Dim s = customer?.ToString() ' only invoke ToString if customer is non-null
```

**Delegate invocation**
``` VB.NET
Dim x As Action(Of Integer) = GetCallbackAction()
x?(5)  ' only invoke if x is non-null
```

**Indexing and default property access**
``` VB.NET
Dim x As List(Of Integer) = GetList()
Dim elem = x?(1)  ' only fetch the first element if x is non-null
```

**Dictionary access**
``` VB.NET
Dim x As Dictionary(Of String, Object) = GetDictionary()
Dim val = x?!name  ' only look up the key "name" if x is non-null
```

**XML member access**
``` VB.NET
Dim x = <customer name="Jane Doe">
            <address>
                <line1></line1>
            </address>
        </customer>
Dim name = x?.@name     ' only get the "name" attribute if x is non-null
Dim name = x?.<address> ' only get the "address" element if x is non-null
Dim name = x?...<line1> ' only get the "line1" descendents if x is non-null
```

NameOf operator
==================
The NameOf operator is a better way of embedding a string literal into your code, when that string literal refers to a programmatic element.

Such as when throwing `ArgumentException`:
``` VB.NET
Sub f(s As String)
   If s Is Nothing Then Throw New ArgumentNullException(NameOf(s))
End Sub
```

Or raising PropertyChanged events:
``` VB.NET
Private _Age As Integer
Property Age As Integer
   Get
      Return _Age
   End Get
   Set
      _Age = value
      RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Age)))
   End Set
End Property
```

Or registering dependency properties:
``` VB.NET
Public AgeProperty As DependencyProperty = DependencyProperty.Register(NameOf(Age), GetType(Of Integer), GetType(C))
```

Because the operand of `NameOf` is an expression referring to a programmatic element rather than a string literal, you get proper IntelliSense while you're typing its argument, and if you make a typo then you'll get a compile-time error, and it gets renamed property when you do a refactor-rename.

The result of the `NameOf` operator is simply the unqualified identifier name of the programmatic element you specify.

`NameOf` works in a few unusual ways, for convenience. You can write `NameOf(x.Log)` to get the name of every method overload of `Log` on `x`, also including extension members. You can also write `NameOf(Customer.Age)` to get the name of an instance property off a type-name.

`NameOf` is a new reserved keyword. Thus, if you had declared any identifiers called `NameOf` in your code before, they will now produce compile-time errors.

Read-only auto-properties
=========================
VB has long had read-only properties and auto-implemented properties. Now auto-implemented properties can be read-only.

``` VB.NET
Class Customer
   Public ReadOnly Property Tags As New List(Of String)
   Public ReadOnly Property Name As String = ""
   Public ReadOnly Property File As String

   Sub New(file As String)
      Me.File = file
   End Sub
End Class
```

Just as with normal auto-implemented properties, you can assign initial values to ReadOnly autoprops. Also you can assign to them in the constructor: this writes direct to the backing field.

As with normal auto-implemented properties, the private backing fields of the above properties are `_Tags`, `_Name`, and `_File`. These backing fields are hidden from IntelliSense but can still be assigned to at any time, not just in the constructor.

Multiline string literals
=========================
VB now allows string literals that split over multiple lines.

``` VB.NET
Dim x = "Hello,
World!"
```

This is cleaner and easier to use than either manually concatenating the vbCrLf constant to your strings:

``` VB.NET
Dim x = "Hello," & vbCrLf & "World"
```

Or using string interpolation (a workaround which was technically never necessary since the feature didn't exist before VB had multiline string literals but nonetheless would have become more tempting had we not added support for multiline string literals at the same time).

``` VB.NET
Dim x = "Hello,{vbCrLf}World!"
```

Or, for the truly desperate, using XML literals as workaround:

``` VB.NET
Dim x = <xml><![CDATA[Hello
World]]></xml>.Value
```

The string will include whatever kind of separator (vbCr or vbCrLf or whatever) that was found in your source file.

Year-first Date literals
========================
VB developers who aren't accustomed to US-style date formats often complain of confusion as to whether `#11/12/2014#` is supposed to be 11th December or 12th November. There's a good unambiguous ISO standard for writing dates in year-month-day order, and VB supports a similar format:

``` VB.NET
Dim d = #2014-11-12#  ' yyyy-MM-dd, November 12th 2014
```

You can also use slashes `#2014/11/12#`, and as always you can specify the time as well `#2014-11-12 19:30#` or `#2014-11-12 7:30 PM#`

Comments before implicit line continuation
==========================================
Comments are now permitted before an implicit line continuation. Here are examples which failed to compile before VB 14 but now work fine.

``` VB.NET
Dim invitees = {"Jim",    ' got to invite him!
                "Marcy",  ' Jim's wife
                "Jones"}

Dim addresses = From i In invitees      ' go through list
                Let address = Lookup(i) ' look it up
                Select i, address
```

 #Disable Warning and #Enable Warning directives
==========================
VB14 now lets you disable / enable warnings for regions within a file:

``` VB.NET
#Disable Warning BC42356 ' suppress warning about no awaits in this method
    Async Function TestAsync() As Task
        Console.WriteLine("testing")
    End Function
#Enable Warning BC42356
```

This feature will be particularly valuable as VB developers consume more Roslyn-powered diagnostic analyzers and Code Aware Libraries. You can disable and enable a comma-separated list of warnings in a single directive and it's not strictly required that a `#Disable Warning` directive is paired with a matching `#Enable Warning` directive and vice-versa.

Partial Modules and Interfaces
==============================
VB used to only allow `Partial` on classes and structures. Now it is allowed on modules and interfaces as well:

``` VB.NET
Partial Module DatabaseExtensions
   ' Extension methods from code generator
End Module

Partial Interface IBindable
   ' WPF-specific binding contract additions
End Interface
```

Partial types are great for anyone doing any kind of code generation because you can separate out the parts of the class that are generated by tooling, and the parts that are written by hand. Beyond that, partial interfaces are also handy when you want to reuse code files but separate out some parts of an interface that only makes sense on a particular platform.

TypeOf <_expression_> IsNot <_type_> operator
===============================
VB has always championed syntax that reads more like a natural language. In 2005 we even added the IsNot operator to make negative reference checks read more naturally. But you still had to write negative type checks with this awkward syntax:

``` VB.NET
If Not TypeOf sender Is Button Then
```

Now in VB14 you can write it more readably:

``` VB.NET
If TypeOf sender IsNot Button Then
```

No patent is pending for the `TypeOf ... IsNot ...` operator.

XML documentation comment improvements
======================================
Previously when you wrote references in your comments then you were basically on your own. Now the language has full support:

* `paramref` tags are now validated to ensure they actually refer to a declared parameter.
* `crefs` attributes can now refer to specific method overloads, generics, operators, and those references are validated by the compiler.
* There is complete tooling support in Visual Studio for doc comments including IntelliSense for crefs, colorization of references, refactoring (e.g. renames) will cascade into doc-comments references, etc.

``` VB.NET
''' <summary>
''' Syncs <paramref name="md"/>, which is
''' a <see cref="DataObject(Of String).CloudMetadata"/>, to the cloud
''' </summary>
''' <param name="md">The local copy to sync</param>
Sub SyncFromClientToAzure(md As DataObject(Of String).CloudMetadata)
   ...
End Sub
```

Minor features and fixes
========================
Smart name resolution
---------------------
VB 14 improves its rules for partially qualified names. When a namespace is imported at either the file or project level it's always been valid in VB to refer to child namespaces in that namespace by their simple name. So, for example, if the `System` namespace is imported then the `System.Threading` namespace is in scope under the simple name `Threading`.

However, this can lead to ambiguities if two parent namespaces contain child namespaces with the same simple name. For example, take the expression `Threading.Thread.Sleep(1000)`. In Console and Windows Forms apps this is okay because `Threading` unambiguously refers to `System.Threading`. But in Windows Presentation Foundation apps it is ambiguous between `System.Threading` and `System.Windows.Threading`.

Previously, VB used to look up the namespace `Threading`, discover it was ambiguous between `System.Threading` and `System.Windows.Threading`, and just give an error. This was particularly painful when adding a new `Imports` statement to a file that had previously unambiguously referred to types using partial qualification and the newly imported namespace introduced an ambiguity.

Now, VB 14 will entertain both possible namespaces at once. If you type `Threading.` in the code editor then in IntelliSense after the dot you'll see the members of both namespaces. If after evaluating the entire qualified name there are two types named `Threading.Thread` VB will still report an ambiguity error. However, if only one meaning of `Threading` actually contains a type named `Thread` then `Threading` unambiguously refers to that namespace. This process works to arbitrary levels of depth so if your qualified name has 4 levels of namespaces VB will still look for an unambiguous result for the full name even if the first three levels would otherwise have an ambiguity. 

There are many other similar cases where this feature makes your code more resilient to - e.g. `ComponentModel.INotifyPropertyChanged` used to be ambiguous in WinForms apps between `System.ComponentModel` and `System.Windows.Forms.ComponentModel` but is now fine. And `Diagnostics.Debug.WriteLine` used to be ambiguous in WinRT apps between `System.Diagnostics` and `Windows.Foundation.Diagnostics` but now is fine.

Read-only interface properties can now be implemented by read-write properties
------------------------------------------------------------------------------
This cleans up a quirky corner of the language. Look at this example:

``` VB.NET
Interface I
    ReadOnly Property P As Integer
End Interface

Class C : Implements I
    Public Property P As Integer Implements I.P
End Class
```

Previously, if you were implementing the read-only property `I.P`, then you had to implement it with a read-only property as well. This was unfortunate since implementing a contract which only requires readability restricted you from fulfilling that contract with a property which elsewhere in your code was fine to write to. Now that restriction has been relaxed: you can implement it with a read/write property if you want. This example happens to implement it with a read/write auto-prop, but you can also use an expanded property with a getter and setter.

 #Region is now allowed inside of methods
----------------------------------------
 #Region is now allowed within method bodies and can cross method bodies. For instance:

``` VB.NET
Function Range(min As Integer, max As Integer) As IEnumerable(Of Integer)
   If min > max Then Throw New ArgumentException
#Region " Validation"
   Return Helper(min, max)
End Function

Private Iterator Function Helper(min As Integer, max As Integer) As IEnumerable(Of Integer)
#End Region
   For i = min To max
      Yield i
   Next
End Function
```

Overrides methods now implicitly include the Overloads modifier in metadata
---------------------------------------------------------------------------
Previously, VB libraries had to write both modifiers Overrides Overloads to play nice with C# users. Now, 'Override' members are also implicitly 'Overloads'.

CObj is now permitted inside attribute arguments
------------------------------------------------
The following code used to give an error that `CObj(...)` isn't a constant. But it is, so we no longer give the error.

``` VB.NET
<DefaultValue(CObj(DayOfWeek.Sunday))>
Public Property FirstDayOfWeek As DayOfWeek
```

Previously, you had no way to specify the "object" overload of an attribute (like this one) that also took an Enum.

Declaration and consumption of ambiguous methods from unrelated interfaces
--------------------------------------------------------------------------
This is a subtle corner of the VB language. Consider the following code:

``` VB.NET
Interface ICustomer
  Sub GetDetails(x As Integer)
End Interface

Interface ITime
  Sub GetDetails(x As String)
End Interface

Interface IMock : Inherits ICustomer, ITime
  Overloads Sub GetDetails(x As Char)
End Interface

Interface IMock2 : Inherits ICustomer, ITime
End Interface
```

Previously it used to be illegal to call the method `GetDetails` in either `IMock` or `IMock2`, on the grounds that you the caller might not know which `GetDetails` you meant to invoke. And VB also used to disallow you to declare an interface like `IMock` on the grounds that you couldn't call it anyway. But C# did let you declare interfaces like these, leaving VB users unable to call them. 

So now VB 14 relaxes the rules. VB will let you declare these interfaces, and will let you invoke methods on them. It will use the normal rules of overload resolution to figure out which `GetDetails` is the most appropriate for a given call site.
