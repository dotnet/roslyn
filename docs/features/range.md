Range
===

* [X] Proposed
* [ ] Prototype
* [ ] Implementation
* [ ] Specification

Summary
===

The expression `x..y` will produce a value of type `System.Range`.

Motivation
===

Overloads for indexers can be written to allow clear slicing syntax:

``` c#
Span<T> this[Range range] { get; }
...
var slice = array[4..10];
```

---

Ranges can be used in patterns to produce simple range checks:

``` c#
switch (x)
{
    case 0..10:
        break;
}
```

---

Ranges implement `IEnumerable<int>` (and the struct-based pattern enumerable), allowing expressive iteration manipulation:

``` c#
foreach (var i in 0..10)
{
    Console.WriteLine(string.Join(",", (0..i).Select(j => i * j)));
}
```

Detailed design
===

The result of the `..` expression is a value of type Range. This struct might look something like this:

``` c#
struct Range : IEnumerable<int>
{
    public Range(int start, int end);
    public int Start { get; }
    public int End { get; }
    public StructRangeEnumerator GetEnumerator();
    ... equals, gethashcode, enumerator implementation, etc. ...
}
```

Any collection type can add a `Range`-taking overload to its indexer, and return an appropriate type for the collection. For arrays, this would be `Span<T>`: the end effect would be zero-copy slicing.

``` c#
public Span<T> this[Range range] { get; }
```

Also, the struct-based and interface-based enumerator allows flexible and fast iteration:

``` c#
foreach (var i in 0..10)
{
    Console.WriteLine(i); // 0, 1, 2, 3...
}
```

LongRange
===

In addition to the `System.Range` type, a parallel `System.LongRange` (names pending) will be created:

``` c#
struct LongRange : IEnumerable<long>
{
    public Range(long start, long end);
    public long Start { get; }
    public long End { get; }
    ...
}
```

RangeWithStep
===

This is not a language feature, but is relevant to the experience as a whole.

Sometimes a range with a "step" is desired: something that steps through all even numbers, for example.

``` c#
struct RangeWithStep : IEnumerable<int>
{
    public Range(int start, int end, int step);
    public int Start { get; }
    public int End { get; }
    public int Step { get; }
    public static implicit operator RangeWithStep(Range range) => new RangeWithStep(range.Start, range.End, 1);
}
static class RangeWithStepExtensions
{
    public static RangeWithStep Step(this Range range, int step);
}
...
var r = (0..100).Step(2);
```

The distinct type is necessary to allow collection types to define distinct indexers for ranges with steps and ranges without (or completely disallow indexing with `RangeWithStep`):

``` c#
T this[Range range] { get; }
T this[RangeWithStep range] { get; }
```

The implicit conversion also makes it convenient to call a `RangeWithStep` indexer with a standard range expression - useful if the implementation of the indexer is identical between `Range` and `RangeWithStep` (for example, the tensor library).

`operator ..`
===

User-defined overloads of the range operator allow customization:

``` c#
public static MyRangeType operator ..(MyType left, MyType right);
```

It's an open question if the lookup of `operator ..` is target-typed or not (i.e. will inspect the target type for applicable `operator ..` overloads, in addition to the argument types). The answer to this question implies many things about the design, so care must be taken around this area.

Inclusive and exclusive ranges
===

There are three options for the inclusive/exclusive question (i.e. whether `2..5` includes `5` or not).

1) The `..` range operator is exclusive.
2) The `..` range operator is inclusive.
3) Two operators are created, one which means inclusive, and one which means exclusive.

Operator examples for option 3 are:

1) Rust
    1) Exclusive: `..`
    1) Inclusive: `..=`
1) Ruby, Rust (original)
    1) Exclusive: `..`
    1) Inclusive: `...`
1) Swift
    1) Exclusive: `..<`
    1) Inclusive: `...`

Open-ended ranges?
===

1) `5..`: "5, and everything after". `array[5..]` means "slice the array from 5 until the end of the array"
2) `..5`: "everything up to 5". `array[..5]` means "slice the array from the start up to (and including?) 5" (inclusion is dependent on the normal exclusive/inclusive decision)
3) `..`: "just give me everything": `array[..]`
