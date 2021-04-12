ExperimentalAttribute
=====================
Report warnings for references to types and members marked with `Windows.Foundation.Metadata.ExperimentalAttribute`.
```
namespace Windows.Foundation.Metadata
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
        AttributeTargets.Interface | AttributeTargets.Delegate,
        AllowMultiple = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
    }
}
```

## Warnings
The warning message is a specific message, where `'{0}'` is the fully-qualified type or member name.
```
'{0}' is for evaluation purposes only and is subject to change or removal in future updates.
```

The warning is reported for any reference to a type or member marked with the attribute.
(The `AttributeUsage` is for types only so the attribute cannot be applied to non-type members from C# or VB,
but the attribute could be applied to non-type members outside of C# or VB.)
```
[Experimental]
class A
{
    internal object F;
    internal static object G;
    internal class B { }
}
class C
{
    static void Main()
    {
        F(default(A));   // warning CS08305: 'A' is for evaluation purposes only ...
        F(typeof(A));    // warning CS08305: 'A' is for evaluation purposes only ...
        F(nameof(A));    // warning CS08305: 'A' is for evaluation purposes only ...
        var a = new A(); // warning CS08305: 'A' is for evaluation purposes only ...
        F(a.F);
        F(A.G);          // warning CS08305: 'A' is for evaluation purposes only ...
        F<A.B>(null);    // warning CS08305: 'A' is for evaluation purposes only ...
    }
    static void F<T>(T t)
    {
    }
}
```

The attribute is not inherited from base types or overridden members.
```
[Experimental]
class A<T>
{
    [Experimental]
    internal virtual void F()
    {
    }
}
class B : A<int> // warning CS08305: 'A<int>' is for evaluation purposes only ...
{
    internal override void F()
    {
        base.F(); // warning CS08305: 'A<int>.F' is for evaluation purposes only ...
    }
}
class C
{
    static void F(A<object> a, B b) // warning CS08305: 'A<object>' is for evaluation purposes only ...
    {
        a.F(); // warning CS08305: 'A<object>.F' is for evaluation purposes only ...
        b.F();
    }
}
```

There is no automatic suppression of warnings: warnings are generated for all references, even within `[Experimental]` members.
Suppressing the warning requires an explicit compiler option or `#pragma`.
```
[Experimental] enum E { }
[Experimental]
class C
{
    private C(E e) // warning CS08305: 'E' is for evaluation purposes only ...
    {
    }
    internal static C Create() // warning CS08305: 'C' is for evaluation purposes only ...
    {
        return Create(0);
    }
#pragma warning disable 8305
    internal static C Create(E e)
    {
        return new C(e);
    }
}
```

## ObsoleteAttribute and DeprecatedAttribute
`ExperimentalAttribute` is independent of `System.ObsoleteAttribute` or `Windows.Framework.Metadata.DeprecatedAttribute`.

Warnings for `[Experimental]` are reported within `[Obsolete]` or `[Deprecated]` members.
Warnings and errors for `[Obsolete]` and `[Deprecated]` are reported inside `[Experimental]` members.
```
[Obsolete]
class A
{
    static object F() => new C(); // warning CS08305: 'C' is for evaluation purposes only ...
}
[Deprecated(null, DeprecationType.Deprecate, 0)]
class B
{
    static object F() => new C(); // warning CS08305: 'C' is for evaluation purposes only ...
}
[Experimental]
class C
{
    static object F() => new B(); // warning CS0612: 'B' is obsolete
}
```

Warnings and errors for `[Obsolete]` and `[Deprecated]` are reported instead of `[Experimental]` if there are multiple attributes.
```
[Obsolete]
[Experimental]
class A
{
}
[Experimental]
[Deprecated(null, DeprecationType.Deprecate, 0)]
class B
{
}
class C
{
    static A F() => null; // warning CS0612: 'A' is obsolete
    static B G() => null; // warning CS0612: 'B' is obsolete
}

```