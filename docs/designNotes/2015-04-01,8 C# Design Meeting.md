C# Design Meeting Notes for Apr 1 and Apr 8, 2015
=================================================

Discussion thread for these notes is at https://github.com/dotnet/roslyn/issues/2119.

Agenda
------

Matt Warren wrote a Roslyn analyzer as a low cost way to experiment with nullability semantics. In these two meetings we looked at evolving versions of this analyzer, and what they imply for language design.

The analyzer is here: https://github.com/mattwar/nullaby.



Flow-based nullability checking
===============================

At the design review on Mar 25 (#1921), there was strong support for adding nullability support to the language, but also the advice to make it easy to transition into using nullability checking by recognizing current patterns for null checking.

This suggests a flow-based approach, where the "null state" of variables is tracked by the compiler, and may be different in different blocks of code. Comparisons of the variable, and assignments to the variable, would all change its null state within the scope of effect of those operations.

An inherent danger with flow-based checks like that is that a variable may change in an untracked way. The risk of that for parameters and local variables is limited: the variable would have to be captured and modified by a lambda, and that lambda would have to be executed elsewhere *during* the running of the current function. Given that any null-checking machinery we build would have to be somewhat approximate anyway, we can probably live with this risk.

It gets gradually worse if we try to track fields of `this` or other objects, or even properties or array elements. In all likelihood, tracking just parameters and local variables would deliver the bulk of the value, but at least "dotted chains" would certainly also be useful.



Attributes versus syntax
========================

The analyzer makes use of attributes to denote when a variable is "nullable" (`[CouldBeNull]`) or "non-nullable" (`[ShouldNotBeNull]`). Compared to a built-in syntax, this has several disadvantages:

* It's less syntactic convenient of course
* The attribute cannot be applied to local variables
* The attribute cannot be applied to a type argument or an array element type

These limitations are inherent to the nature of the experiment, but we know how to counter them if we add language syntax, even if that syntax is encoded with the use of attributes. (We learned all the tricks when we introduced `dynamic`.)



Analyzers versus built-in rules
===============================

Providing nullability diagnostics by means of an analyzer comes with a number of pros and cons compared to having those diagnostics built in to the language.

* Language rules need to be clearly specified and reasonable to explain. An analyzer can employ more heuristics.
* Language-based diagnostics need to consider back compat for the language, whereas analyzers can introduce warnings on currently valid code.
* For the same reason, analyzers can evolve over time
* With an analyzer, individual rules can be turned on and off. Some may want a harsher medicine than others. With the language it has to be one size fits all.

On the other hand:

* Rules can probably be implemented more efficiently in the compiler itself (though we might be able to come up with tricks to deal with that for analyzers too)
* The language has an opportunity to standardize what exactly is allowed
* The rules would still apply in contexts where analyzers aren't run
* It would be odd to add `!` and `?` syntax to the language, without adding the accompanying semantics
* We could avoid many issues with back compat if we adopt the notion of "warning waves" (#1580), where later versions of the language can add new warnings.

The analyzer has several shortcomings due to the lack of a public flow analysis API in Roslyn. That would be a great addition, regardless of what we do for nullability checking.

With a cruder/simpler analysis built-in to the language, you can imagine folks building enhancement analyzers on top of it. Those may not just add new diagnostics, but might want to *turn off* compiler warnings where more heuristics can determine that they are in fact not warranted. The analyzer infrastructure doesn't currently support this.



Taking the analyzer for a spin
==============================

Given the following declaration

``` c#
void Foo([ShouldNotBeNull] string s) { }
```

The following statements would yield warnings because `s` is declared to be nullable, but is used in a way that requires it not to be:

``` c#
void Bad([CouldBeNull] string s)
{
    Foo(s);           // Warning!
    var l = s.Length; // Warning!
}
```

However, all the following methods are ok, because the flow analysis can determine that `s` is not null at the point where it is used:
 
``` c#
void Ok1([CouldBeNull] string s)
{
    s = "Not null";
    Foo(s); // Ok
}
void Ok2([CouldBeNull] string s)
{
    if (s != null)
    {
        Foo(s); // Ok
    }
}
void Ok3([CouldBeNull] string s)
{
    if (s == null)
    {
        throw new ArgumentNullException();
    }
    Foo(s); // Ok
}
void Ok4([CouldBeNull] string s)
{
    if (s == null)
    {
        s = "NotNull";
    }
    Foo(s); // Ok
}
void Ok5([CouldBeNull] string s)
{
    if (s != null && s.Length > 0) // Ok
    {
    }
}
void Ok6([CouldBeNull] string s)
{
    if (s == null || s.Length > 0) // Ok
    {
    }
}
```

This seems hugely useful, because current code will just continue to work in the vast majority of cases.

It is a change of thinking from where nullability is strongly part of the *type* of a variable, and is established at declaration time. In that paradigm, establishing that a given variable is not null doesn't help you; you have to capture its value in a new, more strongly typed variable, which is more cumbersome, and which would require existing code to be extensively rewritten to match new patterns.



Conclusion
==========

Matt's experiment is great, and we are very interested in the nullability tracking approach, because it has the potential to make the bulk of existing code work in the face of new annotations.
