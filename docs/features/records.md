Records for C#
==============

Records are a new, simplified declaration form for C# class and struct types that combine the benefits of a number of simpler features. We describe the new features (caller-receiver parameters and *with-expression*s), give the syntax and semantics for record declarations, and then provide some examples.

# caller-receiver parameters

Currently a method parameter's *default-argument* must be 
- a *constant-expression*; or
- an expression of the form `new S()` where `S` is a value type; or
- an expression of the form `default(S)` where `S` is a value type

We extend this to add the following
- an expression of the form `this.Identifier`

This new form is called a *caller-receiver default-argument*, and is allowed only if all of the following are satisfied
- The method in which it appears is an instance method; and
- The expression `this.Identifier` binds to an instance member of the enclosing type, which must be either a field or a property; and
- The member to which it binds (and the `get` accessor, if it is a property) is at least as accessible as the method; and
- The type of `this.Identifier` is implicitly convertible by an identity or nullable conversion to the type of the parameter (this is an existing constraint on *default-argument*).

When an argument is omitted from an invocation of a function member for a corresponding optional parameter with a *caller-receiver default-argument*, the value of the receiver's member is implicitly passed. 

> **Design Notes**: the main reason for the caller-receiver parameter is to support the *with-expression*. The idea is that you can declare a method like this
> ```cs
> class Point
> {
>     public readonly int X;
>     public readonly int Y;
>     public Point With(int x = this.X, int y = this.Y) => new Point(x, y);
>     // etc
> }
> ```
> and then use it like this
> ```cs
>     Point p = new Point(3, 4);
>     p = p.With(x: 1);
> ```
> To create a new `Point` just like an existing `Point` but with the value of `X` changed.
> 
> It is an open question whether or not the syntactic form of the *with-expression* is worth adding once we have support for caller-receiver parameters, so it is possible we would do this *instead of* rather than *in addition to* the *with-expression*.

- [ ] **Open issue**: What is the order in which a *caller-receiver default-argument* is evaluated with respect to other arguments? Should we say that it is unspecified?

# with-expressions

A new expression form is proposed:

```antlr
primary_expression
    : with_expression
    ;

with_expression
    : primary_expression 'with' '{' with_initializer_list '}'
    ;

with_initializer_list
    : with_initializer
    | with_initiaizer ',' with_initializer_list
    ;

with_initializer
    : identifier '=' expression
    ;
```

The token `with` is a new context-sensitive keyword.

Each *identifier* on the left of a *with_initilaizer* must bind to an accessible instance field or property of the type of the *primary_expression* of the *with_expression*. There may be no duplicated name among these identifiers of a given *with_expression*.

A *with_expression* of the form

> *e1* `with` `{` *identifier* = *e2*, ... `}`

is treated as an invocation of the form

> *e1*`.With(`*identifier2*`:` *e2*, ...`)`

Where, for each method named `With` that is an accessible instance member of *e1*, we select *identifier2* as the name of the first parameter in that method that has a caller-receiver parameter that is the same member as the instance field or property bound to *identifier*. If no such parameter can be identified that method is eliminated from consideration. The method to be invoked is selected from among the remaining candidates by overload resolution.

> **Design Notes**: Given caller-receiver parameters, many of the benefits of the *with-expression* are available without this special syntax form. We are therefore considering whether or not it is needed. Its main benefit is allowing one to program in terms of the names of fields and properties, rather than in terms of the names of parameters. In this way we improve both readability and the quality of tooling (e.g. go-to-definition on the identifier of a *with_expression* would navigate to the property rather than to a method parameter).

- [ ] **Open issue**: This description should be modified to support extension methods.
- [ ] **Open issue**: Does this syntactic sugar actually pay for itself?

# pattern-matching

See the [Pattern Matching Specification](patterns.md) for a specification of `operator is` and its relationship to pattern-matching.

> **Design Notes**: By virtue of the compiler-generated `operator is` as specified herein, and the specification for pattern-matching, a record declaration
> ```cs
> public class Point(int X, int Y);
> ```
> will support positional pattern-matching as follows
> ```cs
> Point p = new Point(3, 4);
> if (p is Point(3, var y)) { // if X is 3
>     Console.WriteLine(y);   // print Y
> }
> ```

# record type declarations

The syntax for a `class` or `struct` declaration is extended to support value parameters; the parameters become properties of the type:

```antlr
class_declaration
    : attributes? class_modifiers? 'partial'? 'class' identifier type_parameter_list?
      record_parameters? record_class_base? type_parameter_constraints_clauses? class_body
    ;

struct_declaration
    : attributes? struct_modifiers? 'partial'? 'struct' identifier type_parameter_list?
      record_parameters? struct_interfaces? type_parameter_constraints_clauses? struct_body
    ;

record_class_base
    : class_type record_base_arguments?
    | interface_type_list
    | class_type record_base_arguments? ',' interface_type_list
    ;

record_base_arguments
    : '(' argument_list? ')'
    ;

record_parameters
    : '(' record_parameter_list? ')'
    ;

record_parameter_list
    : record_parameter
    | record_parameter record_parameter_list
    ;

record_parameter
    : attributes? type identifier record_property_name? default_argument?
    ;

record_property_name
    : ':' identifier
    ;

class_body
    : '{' class_member_declarations? '}'
    | ';'
    ;

struct_body
    : '{' struct_members_declarations? '}'
    | ';'
    ;
```

> **Design Notes**: Because record types are often useful without the need for any members explicitly declared in a class-body, we modify the syntax of the declaration to allow a body to be simply a semicolon.

A class (struct) declared with the *record-parameters* is called a *record class* (*record struct*), either of which is a *record type*.

- [ ] **Open issue**: We need to include *primary_constructor_body* in the grammar so that it can appear inside a record type declaration.
- [ ] **Open issue**: What are the name conflict rules for the parameter names? Presumably one is not allowed to conflict with a type parameter or another *record-parameter*.
- [ ] **Open issue**: We need to specify the scope of the record-parameters. Where can they be used? Presumably within instance field initializers and *primary_constructor_body* at least.
- [ ] **Open issue**: Can a record type declaration be partial? If so, must the parameters be repeated on each part?

### Members of a record type

In addition to the members declared in the *class-body*, a record type has the following additional members:

#### Primary Constructor

A record type has a `public` constructor whose signature corresponds to the value parameters of the type declaration. This is called the *primary constructor* for the type, and causes the implicitly declared *default constructor* to be suppressed.

At runtime the primary constructor

* initializes compiler-generated backing fields for the properties corresponding to the value parameters (if these properties are compiler-provided; [see 1.1.2](#1.1.2)); then
* executes the instance field initializers appearing in the *class-body*; and then
* invokes a base class constructor:
    * If there are arguments in the *record_base_arguments*, a base constructor selected by overload resolution with these arguments is invoked;
    * Otherwise a base constructor is invoked with no arguments.
* executes the body of each *primary_constructor_body*, if any, in source order.

- [ ] **Open issue**: We need to specify that order, particularly across compilation units for partials.
- [ ] **Open Issue**: We need to specify that every explicitly declared constructor must chain to the primry constructor.
- [ ] **Open issue**: Should it be allowed to change the access modifier on the primary constructor?
- [ ] **Open issue**: In a record struct, it is an error for there to be no record parameters?

#### Primary constructor body

```antlr
primary_constructor_body
    : attributes? constructor_modifiers? identifier block
    ;
```

A *primary_constructor_body* may only be used within a record type declaration. The *identifier* of a *primary_constructor_body* shall name the record type in which it is declared.

The *primary_constructor_body* does not declare a member on its own, but is a way for the programmer to provide attributes for, and specify the access of, a record type's primary constructor. It also enables the programmer to provide additional code that will be executed when an instance of the record type is constructed.

- [ ] **Open issue**: We should note that a struct default constructor bypasses this.
- [ ] **Open issue**: We should specify the execution order of initialization.
- [ ] **Open issue**: Should we allow something like a *primary_constructor_body* (presumably without attributes and modifiers) in a non-record type declaration, and treat it like we would the code of an instance field initializer?

#### Properties

For each record parameter of a record type declaration there is a corresponding `public` property member whose name and type are taken from the value parameter declaration. Its name is the *identifier* of the *record_property_name*, if present, or the *identifier* of the *record_parameter* otherwise. If no concrete (i.e. non-abstract) public property with a `get` accessor and with this name and type is explicitly declared or inherited, it is produced by the compiler as follows:

* For a *record struct* or a `sealed` *record class*:
 * A `private` `readonly` field is produced as a backing field for a `readonly` property. Its value is initialized during construction with the value of the corresponding primary constructor parameter.
 * The property's `get` accessor is implemented to return the value of the backing field.
 * Each "matching" inherited virtual property's `get` accessor is overridden.

> **Design notes**: In other words, if you extend a base class or implement an interface that declares a public abstract property with the same name and type as a record parameter, that property is overridden or implemented.

- [ ] **Open issue**: Should it be possible to change the access modifier on a property when it is explicitly declared?
- [ ] **Open issue**: Should it be possible to substitute a field for a property?

#### Object Methods

For a *record struct* or a `sealed` *record class*, implementations of the methods `object.GetHashCode()` and `object.Equals(object)` are produced by the compiler unless provided by the user.

- [ ] **Open issue**: We should precisely specify their implementation.
- [ ] **Open issue**: We should also add the interface `IEquatable<T>` for the record type and specify that implementations are provided.
- [ ] **Open issue**: We should also specify that we implement every `IEquatable<T>.Equals`.
- [ ] **Open issue**: We should specify precisely how we solve the problem of Equals in the face of record inheritance: specifically how we generate equality methods such that they are symmetric, transitive, reflexive, etc.
- [ ] **Open issue**: It has been proposed that we implement `operator ==` and `operator !=` for record types.
- [ ] **Open issue**: Should we auto-generate an implementation of `object.ToString`?

#### `operator is`

A record type has a compiler-generated `public static void operator is` unless one with any signature is provided by the user. Its first parameter is the enclosing record type, and each subsequent parameter is an `out` parameter of the same name and type as the corresponding parameter of the record type. The compiler-provided implementation of this method shall assign each `out` parameter with the value of the corresponding property.

See [the pattern-matching specification](patterns.md) for the semantics of `operator is`.

#### `With` method

Unless there is a user-declared member named `With` declared, a record type has a compiler-provided method named `With` whose return type is the record type itself, and containing one value parameter corresponding to each *record-parameter* in the same order that these parameters appear in the record type declaration. Each parameter shall have a *caller-receiver default-argument* of the corresponding property.

In an `abstract` record class, the compiler-provided `With` method is abstract. In a record struct, or a sealed record class, the compiler-provided `With` method is `sealed`. Otherwise the compiler-provided `With` method is `virtual and its implementation shall return a new instance produced by invoking the the primary constructor with the parameters as arguments to create a new instance from the parameters, and return that new instance.

- [ ] **Open issue**: We should also specify under what conditions we override or implement inherited virtual `With` methods or `With` methods from implemented interfaces.
- [ ] **Open issue**: We should say what happens when we inherit a non-virtual `With` method.

> **Design notes**: Because record types are by default immutable, the `With` method provides a way of creating a new instance that is the same as an existing instance but with selected properties given new values. For example, given
> ```cs
> public class Point(int X, int Y);
> ```
> there is a compiler-provided member
> ```cs
>     public virtual Point With(int X = this.X, int Y = this.Y) => new Point(X, Y);
> ```
> Which enables an variable of the record type
> ```cs
> var p = new Point(3, 4);
> ```
> to be replaced with an instance that has one or more properties different
> ```cs
>     p = p.With(X: 5);
> ```
> This can also be expressed using the *with_expression*:
> ```cs
>     p = p with { X = 5 };
> ```

# 5. Examples

### record struct example

This record struct

```cs
public struct Pair(object First, object Second);
```

is translated to this code

```cs
public struct Pair : IEquatable<Pair>
{
    public object First { get; }
    public object Second { get; }
    public Pair(object First, object Second)
    {
        this.First = First;
        this.Second = Second;
    }
    public bool Equals(Pair other) // for IEquatable<Pair>
    {
        return Equals(First, other.First) && Equals(Second, other.Second);
    }
    public override bool Equals(object other)
    {
        return (other as Pair)?.Equals(this) == true;
    }
    public override GetHashCode()
    {
        return (First?.GetHashCode()*17 + Second?.GetHashCode()).GetValueOrDefault();
    }
    public Pair With(object First = this.First, object Second = this.Second) => new Pair(First, Second);
    public static void operator is(Pair self, out object First, out object Second)
    {
        First = self.First;
        Second = self.Second;
    }
}
```

- [ ] **Open issue**: should the implementation of Equals(Pair other) be a public member of Pair?
- [ ] **Open issue**: This implementation of `Equals` is not symmetric in the face of inheritance.

> **Design notes**: Because one record type can inherit from another, and this implementation of `Equals` would not be symmetric in that case, it is not correct. We propose to implement equality this way:
> ```cs
>     public bool Equals(Pair other) // for IEquatable<Pair>
>     {
>         return other != null && EqualityContract == other.EqualityContract &&
>             Equals(First, other.First) && Equals(Second, other.Second);
>     }
>     protected virtual Type EqualityContract => typeof(Pair);
> ```
> Derived records would `override EqualityContract`. The less attractive alternative is to restrict inheritance.

### sealed record example

This sealed record class

```cs
public sealed class Student(string Name, decimal Gpa);
```

is translated into this code

```cs
public sealed class Student : IEquatable<Student>
{
    public string Name { get; }
    public decimal Gpa { get; }
    public Student(string Name, decimal Gpa)
    {
        this.Name = Name;
        this.Gpa = Gpa;
    }
    public bool Equals(Student other) // for IEquatable<Student>
    {
        return other != null && Equals(Name, other.Name) && Equals(Gpa, other.Gpa);
    }
    public override bool Equals(object other)
    {
        return this.Equals(other as Student);
    }
    public override int GetHashCode()
    {
        return (Name?.GetHashCode()*17 + Gpa?.GetHashCode()).GetValueOrDefault();
    }
    public Student With(string Name = this.Name, decimal Gpa = this.Gpa) => new Student(Name, Gpa);
    public static void operator is(Student self, out string Name, out decimal Gpa)
    {
        Name = self.Name;
        Gpa = self.Gpa;
    }
}
```

### abstract record class example

This abstract record class

```cs
public abstract class Person(string Name);
```

is translated into this code

```cs
public abstract class Person : IEquatable<Person>
{
    public string Name { get; }
    public Person(string Name)
    {
        this.Name = Name;
    }
    public bool Equals(Person other)
    {
        return other != null && Equals(Name, other.Name);
    }
    public override Equals(object other)
    {
        return Equals(other as Person);
    }
    public override int GetHashCode()
    {
        return (Name?.GetHashCode()).GetValueOrDefault();
    }
    public abstract Person With(string Name = this.Name);
    public static void operator is(Person self, out string Name)
    {
        Name = self.Name;
    }
}
```

### combining abstract and sealed records

Given the abstract record class `Person` above, this sealed record class

```cs
public sealed class Student(string Name, decimal Gpa) : Person(Name);
```

is translated into this code

```cs
public sealed class Student : Person, IEquatable<Student>
{
    public override string Name { get; }
    public decimal Gpa { get; }
    public Student(string Name, decimal Gpa) : base(Name)
    {
        this.Name = Name;
        this.Gpa = Gpa;
    }
    public override bool Equals(Student other) // for IEquatable<Student>
    {
        return Equals(Name, other.Name) && Equals(Gpa, other.Gpa);
    }
    public bool Equals(Person other) // for IEquatable<Person>
    {
        return (other as Student)?.Equals(this) == true;
    }
    public override bool Equals(object other)
    {
        return (other as Student)?.Equals(this) == true;
    }
    public override int GetHashCode()
    {
        return (Name?.GetHashCode()*17 + Gpa.GetHashCode()).GetValueOrDefault();
    }
    public Student With(string Name = this.Name, decimal Gpa = this.Gpa) => new Student(Name, Gpa);
    public override Person With(string Name = this.Name) => new Student(Name, Gpa);
    public static void operator is(Student self, out string Name, out decimal Gpa)
    {
        Name = self.Name;
        Gpa = self.Gpa;
    }
}
```
