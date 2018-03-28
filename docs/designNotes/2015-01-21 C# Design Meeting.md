C# Design Meeting Notes for Jan 21, 2015
========================================

Discussion thread on these notes can be found at https://github.com/dotnet/roslyn/issues/98.

Quotes of the day: 

> Live broadcast of design meetings: we could call it C#-SPAN
>
> We've made it three hours without slippery slopes coming up!


Agenda
------

This is the first design meeting for the version of C# coming after C# 6. We shall colloquially refer to it as C# 7. The meeting focused on setting the stage for the design process and homing in on major themes and features.

1. Design process
2. Themes
3. Features

See also [Language features currently under consideration by the language design group](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+label%3A%22Area-Language+Design%22+label%3A%221+-+Planning%22+ "Language Features Under Consideration").

1. Design process
=================

We have had great success sharing design notes publicly on CodePlex for the last year of C# 6 design. The ability of the community to see and respond to our thinking in real time has been much appreciated.

This time we want to increase the openness further:

- we involve the community from the beginning of the design cycle (as per these notes!)
- in addition to design notes (now issues on GitHub) we will maintain feature proposals (as checked-in Markdown documents) to reflect the current design of the feature
- we will consider publishing recordings of the design meetings themselves, or even live streaming
- we will consider adding non-Microsoft members to the design team.

Design team
-----------

The C# 7 design team currently consists of

- [Anders Hejlsberg](https://github.com/ahejlsberg)
- [Mads Torgersen](https://github.com/MadsTorgersen)
- [Lucian Wischik](https://github.com/ljw1004)
- [Matt Warren](https://github.com/mattwar)
- [Neal Gafter](https://github.com/gafter)
- [Anthony D. Green](https://github.com/AnthonyDGreen)
- [Stephen Toub](https://github.com/stephentoub)
- [Kevin Pilch-Bisson](https://github.com/Pilchie)
- [Vance Morrison](https://github.com/vancem)
- [Immo Landwerth](https://github.com/terrajobst)

Anders, as the chief language architect, has ultimate say, should that ever become necessary. Mads, as the language PM for C#, pulls together the agenda, runs the meetings and takes the notes. (Oooh, the power!)

To begin with, we meet 4 hours a week as we decide on the overall focus areas. There will not be a separate Visual Basic design meeting during this initial period, as many of the overall decisions are likely to apply to both and need to happen in concert. 

Feature ideas
-------------

Anyone can put a feature idea up as an *issue* on GitHub. We'll keep an eye on those, and use them as input to language design.

A way to gauge interest in a feature is to put it up on UserVoice, where there's a voting system. This is important, because the set of people who hang out in our GitHub repo are not necessarily representative of our developer base at large. 

Design notes
------------

Design notes are point-in-time documents, so we will put them up as *issues* on GitHub. For a period of time, folks can comment on them and the  reactions will feed into subsequent meetings.

Owners and proposals
--------------------

If the design team decides to move on with a feature idea, we'll nominate an *owner* for it, typically among the design team members, who will drive the activities related to the design of that feature: gathering feedback, making progress between meetings, etc. Most importantly, the owner will be responsible for maintaining a *proposal* document that describes the current state of that feature, cross-linking with the design notes where it was discussed.

Since the proposals will evolve over time, they should be documents in the repo, with history tracked. When the proposal is first put up, and if there are major revisions, we will probably put up an issue too, as a place to gather comments. There can also be pull requests to the proposals.

We'll play with this process and find a balance.

Other ways of increasing openness
---------------------------------

We are very interested in other ideas, such as publishing recordings (or even live streaming?) of the design meeting themselves, and inviting non-Microsoft luminaries, e.g., from major players in the industry, onto the design team itself. We are certainly open to have "guests" (physical or virtual) when someone has insights that we want to leverage.

However, these are things we can get to over time. We are not going to do them right out of the gate.

Decisions
---------

It's important to note that the C# design team is still in charge of the language. This is not a democratic process. We derive immense value from comments and UserVoice votes, but in the end the governance model for C# is benevolent dictatorship. We think design in a small close-knit group where membership is long-term is the right model for ensuring that C# remains tasteful, consistent, not too big and generally not "designed by committee".

If we don't agree within the design team, that is typically a sign that there are offline activities that can lead to more insight. Usually, at the end of the day, we don't need to vote or have the Language Allfather make a final call.

Prototypes
----------

Ideally we should prototype every feature we discuss, so as to get a good feel fro the feature and allow the best possible feedback from the community. That may note be realistic, but once we have a good candidate feature, we should try to fly it.

The cost of the prototyping is an issue. This may be feature dependent: Sometimes you want a quick throwaway prototype, sometimes it's more the first version of an actual implementation.

Could be done by a member of the design team, the product team or the community.

Agenda
------
 
It's usually up to Mads to decide what's ready to discuss. Generally, if a design team member wants something on the agenda, they get it. There's no guarantee that we end up following the plan in the meeting; the published notes will just show the agenda as a summary of what was *actually* discussed.


2. Themes
=========

If a feature is great, we'll want to add it whether it fits in a theme or not. However, it's useful to have a number of categories that we can rally around, and that can help select features that work well together.

We discussed a number of likely themes to investigate for C# 7.

Working with data
-----------------

Today’s programs are connected and trade in rich, structured data: it’s what’s on the wire, it’s what apps and services produce, manipulate and consume. 

Traditional object-oriented modeling is good for many things, but in many ways it deals rather poorly with this setup: it bunches functionality strongly with the data (through encapsulation), and often relies heavily on mutation of that state. It is "behavior-centric" instead of "data-centric".

Functional programming languages are often better set up for this: data is immutable (representing *information*, not *state*), and is manipulated from the outside, using a freely growable and context-dependent set of functions, rather than a fixed set of built-in virtual methods. Let’s continue being inspired by functional languages, and in particular other languages – F#, Scala, Swift – that aim to mix functional and object-oriented concepts as smoothly as possible.

Here are some possible C# features that belong under this theme:

- pattern matching
- tuples
- "denotable" anonymous types
- "records" - compact ways of describing shapes
- working with common data structures (List/Dictionary)
- extension members
- slicing
- immutability
- structural typing/shapes?
    
A number of these features focus on the interplay between "kinds of types" and the ways they are used. It is worth thinking of this as a matrix, that lets you think about language support for e.g. denoting the types (*type expressions*), creating values of them (*literals*) and consuming them with matching (*patterns*) :

| Type       | Denote                  | Create                       | Match                    |
|------------|-------------------------|------------------------------|--------------------------|
| General    | `T`                     | `new T()`, `new T { x = e }` | `T x`, `var x`, `*`      |
| Primitive  | `int`, `double`, `bool` | `5`, `.234`, `false`         | `5`, `.234`, `false`     |
| String     | `string`                | `"Hello"`                    | `"Hello"`                |
| Tuple      | `(T1, T2)`              | `(e1, e2)`                   | `(P1, P2)`               |
| Record     | `{ T1 x1, T2 x2 }`      | `new { x1 = e1, x2 = e2 }`   | `{ x1 is P1, x2 is P2 }` |  
| Array      | `T[]`                   | `new T[e]`, `{ e1, e2 }`     | `{ P1, P2 }`, `P1 :: P2` |
| List       | ?                       | ?                            | ?                        |
| Dictionary | ?                       | ?                            | ?                        |
| ...        |                         |                              |                          |


A lot of the matrix above is filled in with speculative syntax, just to give an idea of how it could be used.

We expect to give many of the features on the list above a lot of attention over the coming months: they have a lot of potential for synergy if they are designed together.

Performance and reliability (and interop)
-----------------------------------------

C# and .NET has a heritage where it sometimes plays a bit fast and loose with both performance and reliability. 

While (unlike, say, Java) it has structs and reified generics, there are still places where it is hard to get good performance. A top issue, for instance is the frequent need to copy, rather than reference. When devices are small and cloud compute cycles come with a cost, performance certainly starts to matter more than it used to.

On the reliability side, while (unlike, say, C and C++) C# is generally memory safe, there are certainly places where it is hard to control or trust exactly what is going on (e.g., destruction/finalization).

Many of these issues tend to show up in particular on the boundary to unmanaged code - i.e. when doing interop. Having coarse-grained interop isn't always an option, so the less it costs and the less risky it is to cross the boundary, the better.

Internally at Microsoft there have been research projects to investigate options here. Some of the outcomes are now ripe to feed into the design of C# itself, while others can affect the .NET Framework, result in useful Roslyn analyzers, etc.

Over the coming months we will take several of these problems and ideas and see if we can find great ways of putting them in the hands of C# developers.

Componentization
----------------

The once set-in-stone issue of how .NET programs are factored and combined is now under rapid evolution.

With generalized extension members as an exception, most work here may not fall in the language scope, but is more tooling-oriented:

- generating reference assemblies
- static linking instead of IL merge
- determinism
- NuGet support
- versioning and adaptive light-up

This is a theme that shouldn't be driven primarily from the languages, but we should be open to support at the language level.

Distribution
------------

There may be interesting things we can do specifically to help with the distributed nature of modern computing.

- Async sequences: We introduced single-value asynchrony in C# 5, but do not yet have a satisfactory approach to asynchronous sequences or streams
- Serialization: we may no longer be into directly providing built-in serialization, but we need to make sure we make it reasonable to custom-serialize data - even when it's immutable, and without requiring costly reflection.

Also, await in catch and finally probably didn't make it into VB 14. We should add those the next time around.

Metaprogramming
---------------

Metaprogramming has been around as a theme on the radar for a long time, and arguably Roslyn is a big metaprogramming project aimed at writing programs about programs. However, at the language level we continue not to have a particularly good handle on metaprogramming. 

Extention methods and partial classes both feel like features that could grow into allowing *generated* parts of source code to merge smoothly with *hand-written* parts. But if generated parts are themselves the result of language syntax - e.g. attributes in source code, then things quickly get messy from a tooling perspective. A keystroke in file A may cause different code to be generated into file B by some custom program, which in turn may change the meaning of A. Not a feedback loop we're eager to have to handle in real time at 20 ms keystroke speed!

Oftentimes the eagerness to generate source comes from it being too hard to express your concept beautifully as a library or an abstraction. Increasing the power of abstraction mechanisms in the language itself, or just the syntax for applying them, might remove a lot of the motivation for generated boilerplate code.

Features that may reduce the need for boilerplate and codegen:

- Virtual extension methods/default interface implementations
- Improvements to generic constraints, e.g.:
    - generic constructor constraints
    - delegate and enum constraints
    - operators or object shapes as constraints (or interfaces), e.g. similar to C++ concepts
- mixins or traits
- delegation

Null
----

With null-conditional operators such as `x?.y` C# 6 starts down a path of more null-tolerant operations. You could certainly imagine taking that further to allow e.g. awaiting or foreach'ing null, etc.

On top of that, there's a long-standing request for non-nullable reference types, where the type system helps you ensure that a value can't be null, and therefore is safe to access.

Importantly such a feature might go along well with proper safe *nullable* reference types, where you simply cannot access the members until you've checked for null. This would go great with pattern matching!

Of course that'd be a lot of new expressiveness, and we'd have to reconcile a lot of things to keep it compatible. In his [blog](http://blog.coverity.com/2013/11/20/c-non-nullable-reference-types), Eric Lippert mentions a number of reasons why non-nullable reference types would be next to impossible to fully guarantee. To be fully supported, they would also have to be known to the runtime; they couldn't just be handled by the compiler.

Of course we could try to settle for a less ambitious approach. Finding the right balance here is crucial.

Themeless in Seattle
--------------------

*Type providers*: This is a whole different kind of language feature, currently known only from F#. We wouldn't be able to just grab F#'s model though; there'd be a whole lot of design work to get this one right!

*Better better betterness*: In C# we made some simplifications and generalizations to overload resolution, affectionately known as "better betterness". We could think of more ways to improve overload resolution; e.g. tie breaking on staticness or whether constraints match, instead of giving compiler errors when other candidates would work.

*Scripting*: The scripting dialect of C# includes features not currently allowed in C# "proper": statements and member declarations at the top level. We could consider adopting some of them.

*params IEnumerable*.

*Binary literals and digit separators*.



3. Features
===========

The Matrix above represents a feature set that's strongly connected, and should probably be talked about together: we can add kinds of types (e.g. tuples, records), we can add syntax for representing those types or creating instances of them, and we can add ways to match them as part of a greater pattern matching scheme.

Pattern matching
----------------

Core then is to have a pattern matching framework in the language: A way of asking if a piece of data has a particular shape, and if so, extracting pieces of it.

``` c#
if (o is Point(var x, 5)) ...
```

There are probably at least two ways you want to use "patterns":

1. As part of an expression, where the result is a bool signaling whether the pattern matched a given value, and where variables in the pattern are in scope throughout the statement in which the pattern occurs.
2. As a case in a switch statement, where the case is picked if the pattern matches, and the variables in the pattern are in scope throughout the statements of that case.

A strong candidate syntax for the expression syntax is a generalization of the `is` expression: we consider the type in an `is` expression just a special case, and start allowing any pattern on the right hand side. Thus, the following would be valid `is` expressions:

``` c#
if (o is Point(*, 5) p) Console.WriteLine(o.x);
if (o is Point p) Console.WriteLine(p.x);
if (p is (var x, 5) ...
```

Variable declarations in an expression would have the same scope questions as declaration expressions did. 

A strong candidate for the switch syntax is to simply generalize current switch statements so that

- the switch expression can be any type
- the case labels can contain patterns, not just constants
- the cases are checked in order of appearance, since they can now overlap

``` c#
switch (o) {
case string s:
    Console.WriteLine(s);
    break;
case int i:
    Console.WriteLine($"Number {i}");
    break;
case Point(int x, int y):
    Console.WriteLine("({x},{y})");
    break;
case null:
    Console.WriteLine("<null>);
    break
}
```

Other syntaxes you can think of:

*Expression-based switch*: An expression form where you can have multiple cases, each producing a result value of the same type.

*Unconditional deconstruction*: It might be useful to separate the deconstruction functionality out from the checking, and be able to unconditionally extract parts from a value that you know the type of:

``` c#
(var x, var y) = getPoint();
```

There is a potential issue here where the value could be null, and there's no check for it. It's probably ok to have a null reference exception in this case.

It would be a design goal to have symmetry between construction and deconstruction syntaxes. 

Patterns *at least* have type testing, value comparison and deconstruction aspects to them.

There may be ways for a type to specify its deconstruction syntax.

In addition it is worth considering something along the lines of "active patterns", where a type can specify logic to determine whether a pattern applies to it or not.

Imagine positional deconstruction or active patterns could be expressed with certain methods:

``` c#
class Point {
    public Point(int x, int y) {...}
    void Deconstruct(out int x, out int y) { ... }
    static bool Match(Point p, out int x, out int y) ...
    static bool Match(JObject json, out int x, out int y) ...
}
```

We could imagine separate syntax for specifying this.

One pattern that does not put new requirements on the type is matching against properties/fields:

``` c#
if (o is Point { X is var x, Y is 0 }) ...
```

Open question: are the variables from patterns mutable?

This has a strong similarity to declaration expressions, and they could coexist, with shared scope rules.

Records
-------

Let's not go deep on records now, but we are aware that we need to reconcile them with primary constructors, as well as with pattern matching.

Array Slices
------------

One feature that could lead to a lot of efficiency would be the ability to have "windows" into arrays - or even onto unmanaged swaths of memory passed along through interop. The amount of copying that could be avoided in some scenarios is probably very significant.

Array slices represent an interesting design dilemma between performance and usability. There is nothing about an array slice that is functionally different from an array: You can get its length and access its elements. For all intents and purposes they are indistinguishable. So the best user experience would certainly be that slices just *are* arrays - that they share the same type. That way, all the existing code that operates on arrays can work on slices too, without modification.

Of course this would require quite a change to the runtime. The performance consequences of that could be negative even on the existing kind of arrays. As importantly, slices themselves would be more efficiently represented by a struct type, and for high-perf scenarios, having to allocate a heap object for them might be prohibitive.

One intermediate approach might be to have slices be a struct type Slice<T>, but to let it implicitly convert to T[] in such a way that the underlying storage is still shared. That way you can use Slice<T> for high performance slice manipulation (e.g. in recursive algorithms where you keep subdividing), but still make use of existing array-based APIs at the cost of a boxing-like conversion allocating a small object.

ref locals and ref returns
--------------------------

Just like the language today has ref parameters, we could allow locals and even return values to be by `ref`. This would be particularly useful for interop scenarios, but could in general help avoid copying. Essentially you could return a "safe pointer" e.g. to a slot in an array.

The runtime already fully allows this, so it would just be a matter of surfacing it in the language syntax. It may come with a significant conceptual burden, however. If a method call can return a *variable* as opposed to a *value*, does that mean you can now assign to it?:

``` c#
m(x, y) = 5;
```

You can now imagine getter-only properties or indexers returning refs that can be assigned to. Would this be quite confusing?

There would probably need to be some pretty restrictive guidelines about how and why this is used.

readonly parameters and locals
------------------------------

Parameters and locals can be captured by lambdas and thereby accessed concurrently, but there's no way to protect them from shared-mutual-state issues: they can't be readonly.

In general, most parameters and many locals are never intended to be assigned to after they get their initial value. Allowing `readonly` on them would express that intent clearly.

One problem is that this feature might be an "attractive nuisance". Whereas the "right thing" to do would nearly always be to make parameters and locals readonly, it would clutter the code significantly to do so.

An idea to partly alleviate this is to allow the combination `readonly var` on a local variable to be contracted to `val` or something short like that. More generally we could try to simply think of a shorter keyword than the established `readonly` to express the readonly-ness.

Lambda capture lists
--------------------

Lambda expressions can refer to enclosing variables:

``` c#
var name = GetName();
var query = customers.Where(c => c.Name == name);
```

This has a number of consequences, all transparent to the developer:
- the local variable is lifted to a field in a heap-allocated object
- concurrent runs of the lambda may access and even modify the field at the same time
- because of implementation tradeoffs the content of the variable may be kept live by the GC, sometimes even after lambdas directly using them cease to exist.

For these reasons, the recently introduced lambdas in C++ offer the possibility for a lambda to explicitly specify what can be captured (and how). We could consider a similar feature, e.g.:

``` c#
var name = GetName();
var query = customers.Where([name]c => c.Name == name);
```
This ensures that the lambda only captures `name` and no other variable. In a way the most useful annotation would be the empty `[]`, making sure that the lambda is never accidentally modified to capture *anything*.

One problem is that it frankly looks horrible. There are probably other syntaxes we could consider. Indeed we need to think about the possibility that we would ever add nested functions or class declarations: whatever capture specification syntax we come up with would have to also work for them.

C# always captures "by reference": the lambda can observe and effect changes to the original variable. An option with capture lists would be to allow other modes of capture, notable "by value", where the variable is copied rather than lifted:
``` c#
var name = GetName();
var query = customers.Where([val name]c => c.Name == name);
```
This might not be *too* useful, as it has the same effect as introducing another local initialized to the value of the original one, and then capture *that* instead.

If we don't want capture list as a full-blown feature, we could consider allowing attributes on lambdas and then having a Roslyn analyzer check that the capture is as specified.

Method contracts
----------------

.NET already has a contract system, that allows annotation of methods with pre- and post-conditions. It grew out of the Spec# research project, and requires post-compile IL rewriting to take full effect. Because it has no language syntax, specifying the contracts can get pretty ugly.

It has often been proposed that we should add specific contract syntax:
``` c#
public void Remove(string item)
    requires item != null
    ensures Count >= 0
{
   ...
}

```

One radical idea is for these contracts to be purely runtime enforced: they would simply turn into checks throwing exceptions (or FailFast'ing - an approach that would need further discussion, but seems very attractive).

When you think about how much code is currently occupied with arguments and result checking, this certainly seems like an attractive way to reduce code bloat and improve readability.

Furthermore, the contracts can produce metadata that can be picked up and displayed by tools.

You could imagine dedicated syntax for common cases - notably null checks. Maybe that is the way we get some non-nullability into the system?

