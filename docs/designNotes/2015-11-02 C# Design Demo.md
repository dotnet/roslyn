Here’s an outline of a demo at the MVP summit on 2015-11-02
===========================================================

Discussion for these notes can be found at https://github.com/dotnet/roslyn/issues/6505.

### Let’s talk about local functions.

A method often has other private “helper” methods that are used in its implementation. Those methods are in the scope of the enclosing type, even though they are only intended to be used in a single place. Local functions allow you to define a function where it is used. For example, given a helper method

    static int Fib(int n)
    {
        return (n < 2) ? 1 : Fib(n - 1) + Fib(n - 2);
    }

Or, using the new syntax added in C# 6:

    static int Fib(int n) => (n < 2) ? 1 : Fib(n - 1) + Fib(n - 2);

And the method that it is used in

    static void Main(string[] args)
    {
        Console.WriteLine(Fib(7));
        Console.ReadKey();
    }

In C# 7 you’ll be able to define the helper function in the scope where it is used:

    static void Main(string[] args)
    {
        int Fib(int n) => (n < 2) ? 1 : Fib(n - 1) + Fib(n - 2); //!

        Console.WriteLine(Fib(7));
        Console.ReadKey();
    }

Local functions can use variables from the enclosing scope: 

    static void Main(string[] args)
    {
        int fib0 = 1; //!
        int Fib(int n) => (n < 2) ? fib0 : Fib(n - 1) + Fib(n - 2);

        Console.WriteLine(Fib(7));
        Console.ReadKey();
    }

You can imagine having to pass such state as additional parameters to a helper method if it were declared in the enclosing type, but local function can use local variables directly.

Capturing state like this does not require allocating frame objects on the heap as it would for delegates, or allocating a delegate object either, so this is much more efficient than what you would have to do to simulate this feature by hand.

### Let’s talk about pattern matching.

With object-oriented programming, you define a virtual method when you have to dispatch an operation on the particular kind of object. That works best when the author of the types can identify ahead of time all of the operations (virtual methods) on the types, but it enables you to have an open-ended set of types.

In the functional style, on the other hand, you define your data as a set of types without virtual functions, and define the functions separately from the data. Each operation provides an implementation for each type in the type hierarchy. That works best when the author of the types can identify ahead of time all of the shapes of the data, but it enables you to have an open-ended set of operations.

C# does a great job for the object-oriented style, but the functional style (where you cannot identify all the operations ahead of time) shows up as a frequent source of awkwardness in C# programs.

Let’s get really concrete. Suppose I have a small hierarchy of types

	// class Person(string Name);
	class Person
	{
	    public Person(string name) { this.Name = name; }
	    public string Name { get; }
	}
	
	// class Student(string Name, double Gpa) : Person(Name);
	class Student : Person
	{
	    public Student(string name, double gpa) : base(name)
			{ this.Gpa = gpa; }
	    public double Gpa { get; }
	}
	
	// class Teacher(string Name, string Subject) : Person(Name);
	class Teacher : Person
	{
	    public Teacher(string name, string subject) : base(name)
			{ this.Subject = subject; }
	    public string Subject { get; }
	}

The comments, by the way, shows a possible future syntax we are considering for C# 7 that we call records. We’re still working on records, so I won’t say more about that today. Here is an operation that uses these types

    static string PrintedForm(Person p)
    {
        Student s;
        Teacher t;
        if ((s = p as Student) != null && s.Gpa > 3.5)
        {
            return $"Honor Student {s.Name} ({s.Gpa})";
        }
        else if (s != null)
        {
            return $"Student {s.Name} ({s.Gpa})";
        }
        else if ((t = p as Teacher) != null)
        {
            return $"Teacher {t.Name} of {t.Subject}";
        }
        else
        {
            return $"Person {p.Name}";
        }
    }

And for the purposes of the demo, a client of that operation

    static void Main(string[] args)
    {
        Person[] oa = {
            new Student("Einstein", 4.0),
            new Student("Elvis", 3.0),
            new Student("Poindexter", 3.2),
            new Teacher("Feynmann", "Physics"),
            new Person("Anders"),
        };
        foreach (var o in oa)
        {
            Console.WriteLine(PrintedForm(o));
        }
        Console.ReadKey();
    }

Note the need to declare the variables `s` and `t` ahead of time in `PrintedForm`. Even though they are only used in one branch of the series of if-then-else statements, they are in scope throughout. That means that you have to think up distinct names for all of these temporary variables. As part of the pattern-matching feature we are repurposing the “is” operator to take a pattern on the right-hand-side. And one kind of pattern is a variable declaration. That allows us to simplify the code like this

    static string PrintedForm(Person p)
    {
        if (p is Student s && s.Gpa > 3.5) //!
        {
            return $"Honor Student {s.Name} ({s.Gpa})";
        }
        else if (p is Student s)
        {
            return $"Student {s.Name} ({s.Gpa})";
        }
        else if (p is Teacher t)
        {
            return $"Teacher {t.Name} of {t.Subject}";
        }
        else
        {
            return $"Person {p.Name}";
        }
    }


Now the temporary variables `s` and `t` are declared and scoped to just the place they need to be. Unfortunately we’re testing against the type `Student` more than once. Back to that in a moment.

We’ve also repurposed the `switch` statement so that the case branches are patterns instead of just constants (though constants are one kind of pattern). That enables you to use `switch` as a "type switch":

    static string PrintedForm(Person p)
    {
        switch (p) //!
        {
            case Student s when s.Gpa > 3.5 :
                return $"Honor Student {s.Name} ({s.Gpa})";
            case Student s :
                return $"Student {s.Name} ({s.Gpa})";
            case Teacher t :
                return $"Teacher {t.Name} of {t.Subject}";
            default :
                return $"Person {p.Name}";
        }
    }

The compiler is careful so that we don’t type-test against `Student` more than once in the generated code for `switch`.

Note the new `when` clause in the switch statement.

We’re also working on an expression equivalent to the switch statement, which is like a multi-branch `?:` operator for pattern matching:

    static string PrintedForm(Person p)
    {
        return p match ( //!
            case Student s when s.Gpa > 3.5 :
                $"Honor Student {s.Name} ({s.Gpa})"
            case Student s :
                $"Student {s.Name} ({s.Gpa})"
            case Teacher t :
                $"Teacher {t.Name} of {t.Subject}"
            case * :
                $"Person {p.Name}"
        );
    }

Because you sometimes need to throw an exception when some condition is unexpected, we’re adding a *throw expression* that you can use in a match expression:

        return p match (
            case Student s when s.Gpa > 3.5 :
                $"Honor Student {s.Name} ({s.Gpa})"
            case Student s :
                $"Student {s.Name} ({s.Gpa})"
            case Teacher t :
                $"Teacher {t.Name} of {t.Subject}"
            case null :
                throw new ArgumentNullException(nameof(p)) //!
            case * :
                $"Person {p.Name}"
        );

Another useful kind of pattern allows you to match on members of a type:

        return p match (
            case Student s when s.Gpa > 3.5 :
                $"Honor Student {s.Name} ({s.Gpa})"
            case Student { Name is "Poindexter" } : //!
                "A Nerd"
            case Student s :
                $"Student {s.Name} ({s.Gpa})"
            case Teacher t :
                $"Teacher {t.Name} of {t.Subject}"
            case null :
                throw new ArgumentNullException(nameof(p))
            case * :
                $"Person {p.Name}"
        );

Since this is an expression, we can use the new “=>” form of a method. Our final method is

    static string PrintedForm(Person p) => p match (
        case Student s when s.Gpa > 3.5 :
            $"Honor Student {s.Name} ({s.Gpa})"
        case Student { Name is "Poindexter" } :
            "A Nerd"
        case Student s :
            $"Student {s.Name} ({s.Gpa})"
        case Teacher t :
            $"Teacher {t.Name} of {t.Subject}"
        case null :
            throw new ArgumentNullException(nameof(p))
        case * :
            $"Person {p.Name}"
        );

In summary:
- Local functions (capturing state is cheap)
- Pattern-matching
  - Operator is
  - Switch, “when” clauses
  - `match` expression
  - Patterns
    - Constant patterns e.g. `1` in an ordinary switch
    - type-match patterns e.g. `Student s`
    - property patterns e.g. `Student { Name is "Poindexter" }`
    - wildcard e.g. `*`
- Still working on
  - Records, algebraic data types
  - Tuples
