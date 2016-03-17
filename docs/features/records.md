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

- [ ] **Open issue**: What is the order in which a *caller-receiver default-argument* is evaluated with respect to other arguments? Should we say that it is unspecified?

# with-expressions

A new expression form is proposed:

> *primary-expression*:<br>
&nbsp;&nbsp;&nbsp;&nbsp;*with-expression*

> *with-expression*:<br>
&nbsp;&nbsp;&nbsp;&nbsp;*primary-expression* `with` `{` *with-initializer-list* `}`

> *with-initializer-list*:<br>
&nbsp;&nbsp;&nbsp;&nbsp;*with-initializer*<br>
&nbsp;&nbsp;&nbsp;&nbsp;*with-initializer* `,` *with-initializer-list*

> *with-initializer*:<br>
&nbsp;&nbsp;&nbsp;&nbsp;*identifier* `=` *expression*

The token `with` is a new context-sensitive keyword.

A with expression of the form

> *e* `with` `{` *identifier* = *e1*, ... `}`

is treated as an invocation of the form

> *e*.With(*identifier*: *e1*, ...)

- [ ] **Open issue**: Does this syntactic sugar actually pay for itself? Is it really so much better than simply writing the invocation?

# pattern-matching

See the [Pattern Matching Specification](patterns.md) for a specification of `operator is` and its relationship to pattern-matching.

# record type declarations

The syntax for a `class` or `struct` declaration is extended to support value parameters; the parameters become properties of the type:

>*class-declaration*:<br>
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*attributes*<sub>opt</sub> *class-modifiers*<sub>opt</sub> *partial*<sub>opt</sub> `class` *identifier* *type-parameter-list*<sub>opt</sub> *record-parameters*<sub>opt</sub></span> *class-base*<sub>opt</sub> *type-parameter-constraints-clauses*<sub>opt</sub> *class-body*

>struct-declaration:<br>
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*attributes*<sub>opt</sub> *struct-modifiers*<sub>opt</sub> *partial*<sub>opt</sub> `struct` *identifier* *type-parameter-list*<sub>opt</sub> *record-parameters*<sub>opt</sub> *struct-interfaces*<sub>opt</sub> *type-parameter-constraints-clauses*<sub>opt</sub> *struct-body*

>*record-parameters*:<br>
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;`(` *record-parameter-list*<sub>opt</sub> `)`

>*record-parameter-list*:<br>
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*record-parameter*<br>
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*record-parameter-list* `,` *record-parameter*

>*record-parameter*:<br>
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*attributes*<sub>opt</sub> *type* *identifier* *default-argument*<sub>opt</sub>

>*class-body*:<br>
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;`{` *class-member-declarations*<sub>opt</sub> `}`<br>
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;`;`

>*struct-body*:<br>
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;`{` *struct-member-declarations*<sub>opt</sub> `}`<br>
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;`;`

- [ ] **Open issue**: What are the name conflict rules for the parameter names? Presumably one is not allowed to conflict with a type parameter or another *record-parameter*.

Because record types are often useful without the need for any members explicitly declared in a class-body, we modify the syntax of the declaration to allow a body to be simply a semicolon.

A class (struct) declared with the *record-parameters* is called a *record class* (*record struct*), either of which is a *record type*.

The *class-modifiers* of a record class declaration must contain either the modifier `abstract` or `sealed`.

- [ ] **Open issue**: We need to specify the scope of the record-parameters. Where can they be used?
- [ ] **Open issue**: Can a record type declaration be partial? If so, must the parameters be repeated on each part?

### Members of a record type

In addition to the members declared in the *class-body*, a record type has the following additional members:

#### Primary Constructor

A *record struct* and a `sealed` *record class* has a `public` constructor whose signature corresponds to the value parameters of the type declaration. This is called the *primary constructor* for the type, and causes the *default constructor* to be suppressed. An `abstract` *record class* has only the explicitly declared constructors or, if none are declared, the default constructor.

The programmer may provide an explicit constructor with the same signature as the primary constructor, which must be declared `public`, whose body provides additional code to execute during construction. It may not contain a *constructor-initializer*.

At runtime the primary constructor

* initializes compiler-generated backing fields for the properties corresponding to the value parameters (if these properties are compiler-provided; [see 1.1.2](#1.1.2)); then

* executes the instance field initializers appearing in the *class-body*; and then

* invokes a base class constructor:

	* If there are arguments in the *class-base* specification, a base constructor selected by overload resolution with these arguments is invoked;

	* Otherwise a base constructor is invoked with no arguments.

* executes the body of an explicitly-declared primary constructor, if any.

- [ ] **Open issue**: Is this the syntax we want for adding code to the primary constructor?
- [ ] **Open issue**: Should it be possible to change the access modifier on the primary constructor?

#### Properties

For each value parameter of a record type declaration there is a corresponding `public` property member whose name and type are taken from the value parameter declaration. If this public property is explicitly declared in the body of the type declaration, it must have the `public` access modifier and a public `get` accessor.

If this `public` property is not explicitly declared within the *class-body*, then the compiler produces the property as follows:

* For a *record struct* or a `sealed` *record class*:
 * A `private` `readonly` field is produced as a backing field for a `readonly` property. Its value is initialized during construction with the value of the corresponding primary constructor parameter.
 * The property's `get` accessor is implemented to return the value of the backing field.
* For an `abstract` *record class*:
 * The property is declared `abstract` with only a `get` accessor.

- [ ] **Open issue**: Should it be possible to change the access modifier on a property when it is explicitly declared?
- [ ] **Open issue**: Should it be possible to substitute a field for a property?
- [ ] **Open issue**: We should specify that the generated properties shall `override` any inherited abstract properties.

#### Object Methods

For a *record struct* or a `sealed` *record class*, implementations of the methods `object.GetHashCode()` and `object.Equals(object)` are produced by the compiler unless provided by the user.

- [ ] **Open issue**: We should precisely specify their implementation.
- [ ] **Open issue**: We should also add the interface `IEquatable<T>` for the record type and specify that implementations are provided.
- [ ] **Open issue**: We should also specify that we implement every `IEquatable<T>.Equals`.

#### `operator is`

A record type has a compiler-generated `public static void operator is` unless one with any signature is provided by the user. Its first parameter is the enclosing record type, and each subsequent parameter is an `out` parameter of the same name and type as the corresponding parameter of the record type. The compiler-provided implementation of this method shall assign each `out` parameter with the value of the corresponding property.

See [the pattern-matching specification](patterns.md) for the semantics of `operator is`.

#### `With` method

Unless there is a user-declared member named `With` declared, a record type has a compiler-provided method named `With` whose return type is the record type itself, and containing one value parameter corresponding to each *record-parameter* in the same order that these parameters appear in the record type declaration. Each parameter shall have a *caller-receiver default-argument* of the corresponding property.

In an `abstract` record class, the compiler-provided `With` method is abstract.

In a record struct, or in a `sealed` record class, the compiler-provided `With` method's implementation shall return a new instance produced by invoking the the primary constructor with the parameters as arguments to create a new instance from the parameters, and return that new instance.

- [ ] **Open issue**: We should also specify when we override or implement inherited virtual `With` methods.

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
		return (other as Pair?)?.Equals(this) == true;
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
        return Equals(Name, other.Name) && Equals(Gpa, other.Gpa);
    }
    public override bool Equals(object other)
    {
		return (other as Student?)?.Equals(this) == true;
    }
    public override GetHashCode()
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
    public abstract string Name { get; }
    public abstract bool Equals(Person other); // for IEquatable<Person>
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
public sealed class Student(string Name, decimal Gpa) : Person;
```

is translated into this code

```cs
public sealed class Student : Person, IEquatable<Student>
{
    public override string Name { get; }
    public decimal Gpa { get; }
    public Student(string Name, decimal Gpa)
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