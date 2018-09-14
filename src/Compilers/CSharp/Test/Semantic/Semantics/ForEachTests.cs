// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) foreach loops.
    /// </summary>
    public class ForEachTests : CompilingTestBase
    {
        [Fact]
        public void TestErrorBadElementType()
        {
            // See bug 7419.
            var text = @"
class C
{
    static void Main()
    {
        System.Collections.IEnumerable sequence = null;
        foreach (MissingType x in sequence)
        {
            bool b = !x.Equals(null);
        }
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
            // (7,18): error CS0246: The type or namespace name 'MissingType' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "MissingType").WithArguments("MissingType"));
        }


        [Fact]
        public void TestErrorNullLiteralCollection()
        {
            var text = @"
class C
{
    static void Main()
    {
        foreach (int x in null)
        {
        }
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0186: Use of null is not valid in this context
                Diagnostic(ErrorCode.ERR_NullNotValid, "null"));
        }

        [Fact]
        public void TestErrorNullConstantCollection()
        {
            var text = @"
class C
{
    static void Main()
    {
        const object NULL = null;
        foreach (int x in NULL)
        {
        }
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
            // (7,27): error CS0186: Use of null is not valid in this context
                Diagnostic(ErrorCode.ERR_NullNotValid, "NULL"));
        }

        [WorkItem(540957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540957")]
        [Fact]
        public void TestErrorDefaultOfArrayType()
        {
            var text = @"
class C
{
    static void Main()
    {
        foreach (int x in default(int[]))
        {
        }
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
            // (7,27): error CS0186: Use of null is not valid in this context
                Diagnostic(ErrorCode.ERR_NullNotValid, "default(int[])"));
        }

        [Fact]
        public void TestErrorLambdaCollection()
        {
            var text = @"
class C
{
    static void Main()
    {
        foreach (int x in (() => {}))
        {
        }
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0446: Foreach cannot operate on a 'lambda expression'. Did you intend to invoke the 'lambda expression'?
                Diagnostic(ErrorCode.ERR_AnonMethGrpInForEach, "(() => {})").WithArguments("lambda expression"));
        }

        [Fact]
        public void TestErrorMethodGroupCollection()
        {
            var text = @"
class C
{
    static void Main()
    {
        foreach (int x in Main)
        {
        }
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0446: Foreach cannot operate on a 'method group'. Did you intend to invoke the 'method group'?
                Diagnostic(ErrorCode.ERR_AnonMethGrpInForEach, "Main").WithArguments("method group"));
        }

        [Fact]
        public void TestErrorNoElementConversion()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in args)
        {
        }
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,9): error CS0030: Cannot convert type 'string' to 'int'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("string", "int"));
        }

        [Fact]
        public void TestErrorPatternNoGetEnumerator()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    //public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS1579: foreach statement cannot operate on variables of type 'Enumerable' because 'Enumerable' does not contain a public definition for 'GetEnumerator'
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new Enumerable()").WithArguments("Enumerable", "GetEnumerator"));
        }

        [Fact]
        public void TestErrorPatternInaccessibleGetEnumerator()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    private Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS1579: foreach statement cannot operate on variables of type 'Enumerable' because 'Enumerable' does not contain a public definition for 'GetEnumerator'
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new Enumerable()").WithArguments("Enumerable", "GetEnumerator"));
        }

        [Fact]
        public void TestErrorPatternNonPublicGetEnumerator()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    internal Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (6,27): warning CS0279: 'Enumerable' does not implement the 'collection' pattern. 'Enumerable.GetEnumerator()' is either static or not public.
                Diagnostic(ErrorCode.WRN_PatternStaticOrInaccessible, "new Enumerable()").WithArguments("Enumerable", "collection", "Enumerable.GetEnumerator()"),
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'Enumerable' because 'Enumerable' does not contain a public definition for 'GetEnumerator'
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new Enumerable()").WithArguments("Enumerable", "GetEnumerator"));
        }

        [Fact]
        public void TestErrorPatternStaticGetEnumerator()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public static Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";

            CreateCompilation(text, parseOptions: TestOptions.Regular7).VerifyDiagnostics(
                // (6,27): warning CS0279: 'Enumerable' does not implement the 'collection' pattern. 'Enumerable.GetEnumerator()' is either static or not public.
                Diagnostic(ErrorCode.WRN_PatternStaticOrInaccessible, "new Enumerable()").WithArguments("Enumerable", "collection", "Enumerable.GetEnumerator()"),
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'Enumerable' because 'Enumerable' does not contain a public definition for 'GetEnumerator'
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new Enumerable()").WithArguments("Enumerable", "GetEnumerator"));
            CreateCompilation(text).VerifyDiagnostics(
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'Enumerable' because 'Enumerable' does not contain a public instance definition for 'GetEnumerator'
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new Enumerable()").WithArguments("Enumerable", "GetEnumerator"));
        }

        [Fact]
        public void TestErrorPatternNullableGetEnumerator()
        {
            var text = @"
struct C
{
    static void Goo(Enumerable? e)
    {
        foreach (long x in e) { }
    }

    static void Main()
    {
        Goo(new Enumerable());
    }
}

struct Enumerable
{
    public Enumerator? GetEnumerator() { return new Enumerator(); }
}

struct Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,28): error CS0117: 'Enumerator?' does not contain a definition for 'Current'
                Diagnostic(ErrorCode.ERR_NoSuchMember, "e").WithArguments("Enumerator?", "Current"),
            // (6,28): error CS0202: foreach requires that the return type 'Enumerator?' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "e").WithArguments("Enumerator?", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternNoCurrent()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    //public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0117: 'Enumerator' does not contain a definition for 'Current'
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new Enumerable()").WithArguments("Enumerator", "Current"),
            // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternInaccessibleCurrent()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    private int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0122: 'Enumerator.Current' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "new Enumerable()").WithArguments("Enumerator.Current"),
            // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternNonPublicCurrent()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    internal int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternStaticCurrent()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public static int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternNonPropertyCurrent()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current;
    public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //         foreach (int x in new Enumerable())
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"),
                // (19,16): warning CS0649: Field 'Enumerator.Current' is never assigned to, and will always have its default value 0
                //     public int Current;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Current").WithArguments("Enumerator.Current", "0")
                );
        }

        [Fact]
        public void TestErrorPatternNoMoveNext()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    //public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0117: 'Enumerator' does not contain a definition for 'MoveNext'
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new Enumerable()").WithArguments("Enumerator", "MoveNext"),
            // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternInaccessibleMoveNext()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    private bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0122: 'Enumerator.MoveNext()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "new Enumerable()").WithArguments("Enumerator.MoveNext()"),
            // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternNonPublicMoveNext()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    internal bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternStaticMoveNext()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public static bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternNonMethodMoveNext()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext;
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //         foreach (int x in new Enumerable())
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"),
                // (20,17): warning CS0649: Field 'Enumerator.MoveNext' is never assigned to, and will always have its default value false
                //     public bool MoveNext;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "MoveNext").WithArguments("Enumerator.MoveNext", "false")
                );
        }

        [Fact]
        public void TestErrorPatternNonBoolMoveNext()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public int MoveNext() { return 1; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternNullableBoolMoveNext()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public bool? MoveNext() { return true; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorPatternNoMoveNextOrCurrent()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        foreach (int x in new Enumerable())
        {
        }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    //public int Current { get { return 1; } }
    //public bool MoveNext() { return false; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0117: 'Enumerator' does not contain a definition for 'Current'
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new Enumerable()").WithArguments("Enumerator", "Current"),
            // (6,27): error CS0202: foreach requires that the return type 'Enumerator' of 'Enumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Enumerable()").WithArguments("Enumerator", "Enumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorMultipleIEnumerableT()
        {
            var text = @"
using System.Collections;
using System.Collections.Generic;

class C
{
    void Goo(Enumerable e)
    {
        foreach (int x in e) { }
    }
}

class Enumerable : IEnumerable<int>, IEnumerable<float>
{
    IEnumerator<float> IEnumerable<float>.GetEnumerator() { throw null; }
    IEnumerator<int> IEnumerable<int>.GetEnumerator() { throw null; }
    IEnumerator IEnumerable.GetEnumerator() { throw null; }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (9,27): error CS1640: foreach statement cannot operate on variables of type 'Enumerable' because it implements multiple instantiations of 'System.Collections.Generic.IEnumerable<T>'; try casting to a specific interface instantiation
                Diagnostic(ErrorCode.ERR_MultipleIEnumOfT, "e").WithArguments("Enumerable", "System.Collections.Generic.IEnumerable<T>"));
        }

        [Fact]
        public void TestErrorMultipleIEnumerableT2()
        {
            var text = @"
using System.Collections;
using System.Collections.Generic;

class C
{
    void Goo(Enumerable e)
    {
        foreach (int x in e) { }
    }
}

class Enumerable : IEnumerable<int>, I
{
    IEnumerator<float> IEnumerable<float>.GetEnumerator() { throw null; }
    IEnumerator<int> IEnumerable<int>.GetEnumerator() { throw null; }
    IEnumerator IEnumerable.GetEnumerator() { throw null; }
}

interface I : IEnumerable<float> { }
";

            CreateCompilation(text).VerifyDiagnostics(
            // (9,27): error CS1640: foreach statement cannot operate on variables of type 'Enumerable' because it implements multiple instantiations of 'System.Collections.Generic.IEnumerable<T>'; try casting to a specific interface instantiation
                Diagnostic(ErrorCode.ERR_MultipleIEnumOfT, "e").WithArguments("Enumerable", "System.Collections.Generic.IEnumerable<T>"));
        }

        /// <summary>
        /// Type parameter with constraints containing
        /// IEnumerable&lt;T&gt; with explicit implementations.
        /// </summary>
        [Fact]
        public void TestErrorExplicitIEnumerableTOnTypeParameter()
        {
            var text =
@"using System.Collections;
using System.Collections.Generic;
struct S { }
interface I : IEnumerable<S> { }
class A : IEnumerable<int>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
class B : A, I
{
    IEnumerator<S> IEnumerable<S>.GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
class C
{
    static void M<T1, T2, T3, T4, T5, T6>(A a, B b, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        where T1 : A
        where T2 : B
        where T3 : I
        where T4 : T1, I
        where T5 : A, I
        where T6 : A, IEnumerable<string>
    {
        foreach (int o in a) { }
        foreach (var o in b) { }
        foreach (int o in t1) { }
        foreach (var o in t2) { }
        foreach (S o in t3) { }
        foreach (S o in t4) { }
        foreach (S o in t5) { }
        foreach (string o in t6) { }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (26,27): error CS1640: foreach statement cannot operate on variables of type 'B' because it implements multiple instantiations of 'System.Collections.Generic.IEnumerable<T>'; try casting to a specific interface instantiation
                Diagnostic(ErrorCode.ERR_MultipleIEnumOfT, "b").WithArguments("B", "System.Collections.Generic.IEnumerable<T>").WithLocation(26, 27),
                // (28,27): error CS1640: foreach statement cannot operate on variables of type 'T2' because it implements multiple instantiations of 'System.Collections.Generic.IEnumerable<T>'; try casting to a specific interface instantiation
                Diagnostic(ErrorCode.ERR_MultipleIEnumOfT, "t2").WithArguments("T2", "System.Collections.Generic.IEnumerable<T>").WithLocation(28, 27));
        }

        /// <summary>
        /// Type parameter with constraints containing
        /// IEnumerable&lt;T&gt; with implicit implementations.
        /// </summary>
        [Fact]
        public void TestErrorImplicitIEnumerableTOnTypeParameter()
        {
            var text =
@"using System.Collections;
using System.Collections.Generic;
struct S { }
interface I : IEnumerable<S> { }
class A : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
class B : A, I
{
    public new IEnumerator<S> GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
class C
{
    static void M<T1, T2, T3, T4, T5, T6>(A a, B b, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        where T1 : A
        where T2 : B
        where T3 : I
        where T4 : T1, I
        where T5 : A, I
        where T6 : A, IEnumerable<string>
    {
        foreach (int o in a) { }
        foreach (var o in b) { }
        foreach (int o in t1) { }
        foreach (var o in t2) { }
        foreach (S o in t3) { }
        foreach (int o in t4) { }
        foreach (S o in t5) { }
        foreach (string o in t6) { }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (31,9): error CS0030: Cannot convert type 'int' to 'S'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("int", "S").WithLocation(31, 9),
                // (32,9): error CS0030: Cannot convert type 'int' to 'string'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("int", "string").WithLocation(32, 9));
        }

        /// <summary>
        /// Type parameter with constraints
        /// using enumerable pattern.
        /// </summary>
        [Fact]
        public void TestErrorEnumerablePatternOnTypeParameter()
        {
            var text =
@"using System.Collections.Generic;
interface I
{
    IEnumerator<object> GetEnumerator();
}
class E : I
{
    IEnumerator<object> I.GetEnumerator() { return null; }
}
class C
{
    static void M<T, U, V>(T t, U u, V v)
        where T : E
        where U : E, I
        where V : T, I
    {
        foreach (var o in t) { }
        foreach (var o in u) { }
        foreach (var o in v) { }
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (17,27): error CS1579: foreach statement cannot operate on variables of type 'T' because 'T' does not contain a public definition for 'GetEnumerator'
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "t").WithArguments("T", "GetEnumerator").WithLocation(17, 27));
        }

        [Fact]
        public void TestErrorNonEnumerable()
        {
            var text = @"
class C
{
    void Goo(int i)
    {
        foreach (int x in i) { }
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'int' because 'int' does not contain a public definition for 'GetEnumerator'
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "i").WithArguments("int", "GetEnumerator"));
        }

        [Fact]
        public void TestErrorModifyIterationVariable()
        {
            var text = @"
class C
{
    void Goo(int[] a)
    {
        foreach (int x in a) { x++; }
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,32): error CS1656: Cannot assign to 'x' because it is a 'foreach iteration variable'
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x").WithArguments("x", "foreach iteration variable"));
        }

        [Fact]
        public void TestErrorImplicitlyTypedCycle()
        {
            var text = @"
class C
{
    System.Collections.IEnumerable Goo(object y)
    {
        foreach (var x in Goo(x)) { }
        return null;
    }
}
";

            CreateCompilation(text).VerifyDiagnostics(
            // (6,31): error CS0103: The name 'x' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x"));
        }

        [Fact]
        public void TestErrorDynamicEnumerator()
        {
            var text = @"
class C
{
    void Goo(DynamicEnumerable e)
    {
        foreach (int x in e) { }
    }
}

public class DynamicEnumerable
{
    public dynamic GetEnumerator() { return null; }
}
";
            // It's not entirely clear why this doesn't work, but it doesn't work in Dev10 either.
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (6,27): error CS0117: 'dynamic' does not contain a definition for 'Current'
                Diagnostic(ErrorCode.ERR_NoSuchMember, "e").WithArguments("dynamic", "Current"),
                // (6,27): error CS0202: foreach requires that the return type 'dynamic' of 'DynamicEnumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "e").WithArguments("dynamic", "DynamicEnumerable.GetEnumerator()"));
        }

        [Fact]
        public void TestErrorTypeEnumerable()
        {
            var text = @"
class C
{
    static void Main()
    {
        foreach (var x in System.Collections.IEnumerable) { }
    }
}
";
            // It's not entirely clear why this doesn't work, but it doesn't work in Dev10 either.
            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0119: 'System.Collections.IEnumerable' is a 'type', which is not valid in the given context
            //         foreach (var x in System.Collections.IEnumerable) { }
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Collections.IEnumerable").WithArguments("System.Collections.IEnumerable", "type"));
        }

        [Fact]
        public void TestErrorTypeNonEnumerable()
        {
            var text = @"
class C
{
    void Goo()
    {
        foreach (int x in System.Console) { }
    }
}
";
            // It's not entirely clear why this doesn't work, but it doesn't work in Dev10 either.
            CreateCompilation(text).VerifyDiagnostics(
            // (6,27): error CS0119: 'System.Console' is a 'type', which is not valid in the given context
            //         foreach (int x in System.Console) { }
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Console").WithArguments("System.Console", "type"));
        }

        [WorkItem(545123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545123")]
        [Fact]
        public void TestErrorForEachLoopWithSyntaxErrors()
        {
            string source = @"
public static partial class Extensions
{
    public static ((System.Linq.Expressions.Expression<System.Func<int>>)(() => int )).Compile()()Fib(this int i)
    {
    }
}

public class ExtensionMethodTest
{
    public static void Run()
    {
        int i = 0;
        var Fib = new[] 
        { 
            i++.Fib()
        };

        foreach (var j in Fib)
        {
        }
    }
}";
            Assert.NotEmpty(CreateCompilation(source).GetDiagnostics());
        }

        [WorkItem(545123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545123")]
        [Fact]
        public void TestErrorForEachOverVoidArray1()
        {
            string source = @"
class C
{
    static void Main()
    {
        var array = new[] { Main() };
        foreach (var element in array)
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,21): error CS0826: No best type found for implicitly-typed array
                //         var array = new[] { Main() };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { Main() }"));
        }

        [WorkItem(545123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545123")]
        [Fact]
        public void TestErrorForEachOverVoidArray2()
        {
            string source = @"
class C
{
    static void Main()
    {
        var array = new[] { Main() };
        foreach (int element in array)
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,21): error CS0826: No best type found for implicitly-typed array
                //         var array = new[] { Main() };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { Main() }"),

                // CONSIDER: Could eliminate this cascading diagnostic.

                // (7,9): error CS0030: Cannot convert type '?' to 'int'
                //         foreach (int element in array)
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("?", "int"));
        }

        [WorkItem(545123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545123")]
        [Fact]
        public void TestErrorForEachWithVoidIterationVariable()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (void element in new int[1])
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (6,18): error CS1547: Keyword 'void' cannot be used in this context
                //         foreach (void element in new int[1])
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(6, 18),
                // (6,9): error CS0030: Cannot convert type 'int' to 'void'
                //         foreach (void element in new int[1])
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("int", "void").WithLocation(6, 9)
                );
        }

        [WorkItem(545123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545123")]
        [Fact]
        public void TestErrorForEachOverErrorTypeArray()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (var element in new Unknown[1])
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,37): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                //         foreach (var element in new Unknown[1])
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown"));
        }

        [Fact]
        public void TestSuccessArray()
        {
            var text = @"
class C
{
    void Goo(int[] a)
    {
        foreach (int x in a) { }
    }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("System.Collections.IEnumerable", info.CollectionType.ToTestDisplayString()); //NB: differs from expression type
            Assert.Equal(SpecialType.System_Int32, info.ElementType.SpecialType);
            Assert.Equal("System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object System.Collections.IEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.ImplicitReference, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Unboxing, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Int32 x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal(SpecialType.System_Collections_IEnumerable, boundNode.Expression.Type.SpecialType);
            Assert.Equal(SymbolKind.ArrayType, ((BoundConversion)boundNode.Expression).Operand.Type.Kind);
        }

        [Fact]
        public void TestSuccessString()
        {
            var text = @"
class C
{
    void Goo(string s)
    {
        foreach (char c in s) { }
    }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal(SpecialType.System_String, info.CollectionType.SpecialType);
            Assert.Equal(SpecialType.System_Char, info.ElementType.SpecialType);
            Assert.Equal("System.CharEnumerator System.String.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Char System.CharEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.CharEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.Identity, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Char c", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal(SpecialType.System_String, boundNode.Expression.Type.SpecialType);
            Assert.Equal(SpecialType.System_String, ((BoundConversion)boundNode.Expression).Operand.Type.SpecialType);
        }

        [Fact]
        public void TestSuccessPattern()
        {
            var text = @"
class C
{
    void Goo(Enumerable e)
    {
        foreach (long x in e) { }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("Enumerable", info.CollectionType.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Int32, info.ElementType.SpecialType);
            Assert.Equal("Enumerator Enumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 Enumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean Enumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.Identity, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.ImplicitNumeric, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Int64 x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal("Enumerable", boundNode.Expression.Type.ToTestDisplayString());
            Assert.Equal("Enumerable", ((BoundConversion)boundNode.Expression).Operand.Type.ToTestDisplayString());
        }

        [Fact]
        public void TestSuccessPatternStruct()
        {
            var text = @"
struct C
{
    void Goo(Enumerable e)
    {
        foreach (long x in e) { }
    }
}

struct Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

struct Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("Enumerable", info.CollectionType.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Int32, info.ElementType.SpecialType);
            Assert.Equal("Enumerator Enumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 Enumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean Enumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.False(info.NeedsDisposeMethod); // Definitely not disposable
            Assert.Equal(ConversionKind.Identity, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.ImplicitNumeric, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Int64 x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal("Enumerable", boundNode.Expression.Type.ToTestDisplayString());
            Assert.Equal("Enumerable", ((BoundConversion)boundNode.Expression).Operand.Type.ToTestDisplayString());
        }

        [Fact]
        public void TestSuccessInterfacePattern()
        {
            var text = @"
class C
{
    void Goo(System.Collections.IEnumerable e)
    {
        foreach (long x in e) { }
    }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("System.Collections.IEnumerable", info.CollectionType.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Object, info.ElementType.SpecialType);
            Assert.Equal("System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object System.Collections.IEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.Identity, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Unboxing, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Int64 x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal("System.Collections.IEnumerable", boundNode.Expression.Type.ToTestDisplayString());
            Assert.Equal("System.Collections.IEnumerable", ((BoundConversion)boundNode.Expression).Operand.Type.ToTestDisplayString());
        }

        [Fact]
        public void TestSuccessInterfaceNonPatternGeneric()
        {
            var text = @"
class C
{
    void Goo(Enumerable e)
    {
        foreach (long x in e) { }
    }
}

class Enumerable : System.Collections.Generic.IEnumerable<int>
{
    // Explicit implementations won't match pattern.
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return null; }
    System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator() { return null; }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("System.Collections.Generic.IEnumerable<System.Int32>", info.CollectionType.ToTestDisplayString()); //NB: differs from expression type
            Assert.Equal(SpecialType.System_Int32, info.ElementType.SpecialType);
            Assert.Equal("System.Collections.Generic.IEnumerator<System.Int32> System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 System.Collections.Generic.IEnumerator<System.Int32>.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString()); //NB: not on generic interface
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.ImplicitReference, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.ImplicitNumeric, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Int64 x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.IEnumerable<System.Int32>", boundNode.Expression.Type.ToTestDisplayString());
            Assert.Equal("Enumerable", ((BoundConversion)boundNode.Expression).Operand.Type.ToTestDisplayString());
        }

        [Fact]
        public void TestSuccessInterfaceNonPatternInaccessibleGeneric()
        {
            var text = @"
class C
{
    void Goo(Enumerable e)
    {
        foreach (object x in e) { }
    }
}

class Enumerable : System.Collections.Generic.IEnumerable<Enumerable.Hidden>
{
    // Explicit implementations won't match pattern.
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return null; }
    System.Collections.Generic.IEnumerator<Hidden> System.Collections.Generic.IEnumerable<Hidden>.GetEnumerator() { return null; }

    private class Hidden { }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("System.Collections.IEnumerable", info.CollectionType.ToTestDisplayString()); //NB: fall back on non-generic, since generic is inaccessible
            Assert.Equal(SpecialType.System_Object, info.ElementType.SpecialType);
            Assert.Equal("System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object System.Collections.IEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.ImplicitReference, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Object x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal(SpecialType.System_Collections_IEnumerable, boundNode.Expression.Type.SpecialType);
            Assert.Equal("Enumerable", ((BoundConversion)boundNode.Expression).Operand.Type.ToTestDisplayString());
        }

        [Fact]
        public void TestSuccessInterfaceNonPatternNonGeneric()
        {
            var text = @"
class C
{
    void Goo(Enumerable e)
    {
        foreach (long x in e) { }
    }
}

class Enumerable : System.Collections.IEnumerable
{
    // Explicit implementation won't match pattern.
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return null; }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("System.Collections.IEnumerable", info.CollectionType.ToTestDisplayString()); //NB: differs from expression type
            Assert.Equal(SpecialType.System_Object, info.ElementType.SpecialType);
            Assert.Equal("System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object System.Collections.IEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.ImplicitReference, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Unboxing, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Int64 x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal(SpecialType.System_Collections_IEnumerable, boundNode.Expression.Type.SpecialType);
            Assert.Equal("Enumerable", ((BoundConversion)boundNode.Expression).Operand.Type.ToTestDisplayString());
        }

        [Fact]
        public void TestSuccessImplicitlyTypedArray()
        {
            var text = @"
class C
{
    void Goo(int[] a)
    {
        foreach (var x in a) { }
    }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("System.Collections.IEnumerable", info.CollectionType.ToTestDisplayString()); //NB: differs from expression type
            Assert.Equal(SpecialType.System_Int32, info.ElementType.SpecialType);
            Assert.Equal("System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object System.Collections.IEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.ImplicitReference, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Unboxing, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal(SpecialType.System_Int32, boundNode.IterationVariables.Single().Type.SpecialType);
        }

        [Fact]
        public void TestSuccessImplicitlyTypedString()
        {
            var text = @"
class C
{
    void Goo(string s)
    {
        foreach (var x in s) { }
    }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal(SpecialType.System_String, info.CollectionType.SpecialType);
            Assert.Equal(SpecialType.System_Char, info.ElementType.SpecialType);
            Assert.Equal("System.CharEnumerator System.String.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Char System.CharEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.CharEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.Identity, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal(SpecialType.System_Char, boundNode.IterationVariables.Single().Type.SpecialType);
        }

        [Fact]
        public void TestSuccessImplicitlyTypedPattern()
        {
            var text = @"
class C
{
    void Goo(Enumerable e)
    {
        foreach (var x in e) { }
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";
            var boundNode = GetBoundForEachStatement(text);
            Assert.NotNull(boundNode.EnumeratorInfoOpt);
            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal(SpecialType.System_Int32, boundNode.IterationVariables.Single().Type.SpecialType);
        }

        [Fact]
        public void TestSuccessImplicitlyTypedInterface()
        {
            var text = @"
class C
{
    void Goo(Enumerable e)
    {
        foreach (var x in e) { }
    }
}

class Enumerable : System.Collections.IEnumerable
{
    // Explicit implementation won't match pattern.
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return null; }
}
";
            var boundNode = GetBoundForEachStatement(text);
            Assert.NotNull(boundNode.EnumeratorInfoOpt);
            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal(SpecialType.System_Object, boundNode.IterationVariables.Single().Type.SpecialType);
        }

        [Fact]
        public void TestSuccessExplicitlyTypedVar()
        {
            var text = @"
class C
{
    void Goo(var[] a)
    {
        foreach (var x in a) { }
    }

    class var { }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("System.Collections.IEnumerable", info.CollectionType.ToTestDisplayString()); //NB: differs from expression type
            Assert.Equal("C.var", info.ElementType.ToTestDisplayString());
            Assert.Equal("System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object System.Collections.IEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.ImplicitReference, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.ExplicitReference, info.CurrentConversion.Kind); //object to C.var
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal("C.var", boundNode.IterationVariables.Single().Type.ToTestDisplayString());
        }

        [Fact]
        public void TestSuccessDynamicEnumerable()
        {
            var text = @"
class C
{
    void Goo(dynamic d)
    {
        foreach (int x in d) { }
    }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal(SpecialType.System_Collections_IEnumerable, info.CollectionType.SpecialType);
            Assert.Equal(SpecialType.System_Object, info.ElementType.SpecialType);
            Assert.Equal("System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object System.Collections.IEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.ImplicitDynamic, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.ExplicitDynamic, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Int32 x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal(SpecialType.System_Collections_IEnumerable, boundNode.Expression.Type.SpecialType);
            Assert.Equal(TypeKind.Dynamic, ((BoundConversion)boundNode.Expression).Operand.Type.TypeKind);
        }

        [Fact]
        public void TestSuccessImplicitlyTypedDynamicEnumerable()
        {
            var text = @"
class C
{
    void Goo(dynamic d)
    {
        foreach (var x in d) { }
    }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal(SpecialType.System_Collections_IEnumerable, info.CollectionType.SpecialType);
            Assert.Equal(TypeKind.Dynamic, info.ElementType.TypeKind); //NB: differs from explicit case
            Assert.Equal("System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object System.Collections.IEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.ImplicitDynamic, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind); //NB: differs from explicit case
            Assert.Equal("dynamic x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal(SpecialType.System_Collections_IEnumerable, boundNode.Expression.Type.SpecialType);
            Assert.Equal(SymbolKind.DynamicType, ((BoundConversion)boundNode.Expression).Operand.Type.Kind);
        }

        [Fact]
        public void TestSuccessTypeParameterConstrainedToInterface()
        {
            var text = @"
class C
{
    static void Test<T>() where T : System.Collections.IEnumerator
    {
        foreach (object x in new Enumerable<T>())
        {
            System.Console.WriteLine(x);
        }
    }
}

public class Enumerable<T>
{
    public T GetEnumerator() { return default(T); }
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("Enumerable<T>", info.CollectionType.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Object, info.ElementType.SpecialType);
            Assert.Equal("T Enumerable<T>.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object System.Collections.IEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.Identity, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.Boxing, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Object x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal("Enumerable<T>", boundNode.Expression.Type.ToTestDisplayString());
            Assert.Equal("Enumerable<T>", ((BoundConversion)boundNode.Expression).Operand.Type.ToTestDisplayString());
        }

        [Fact]
        public void TestSuccessTypeParameterConstrainedToClass()
        {
            var text =
@"using System.Collections;
using System.Collections.Generic;
class A<T> : IEnumerable<T>
{
    IEnumerator<T> IEnumerable<T>.GetEnumerator() { return null; }
    IEnumerator IEnumerable.GetEnumerator() { return null; }
}
class A0 : A<string> { }
class C<T>
{
    static void M<U, V>(A0 a, U u, V v)
        where U : A0
        where V : A<T>
    {
        foreach (var o in a)
        {
            M<string>(o);
        }
        foreach (var o in u)
        {
            M<string>(o);
        }
        foreach (var o in v)
        {
            M<T>(o);
        }
    }
    static void M<U>(U u) { }
}";
            var compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void TestSuccessTypeParameterConstrainedToPattern()
        {
            var text = @"
class C
{
    static void Test<T>() where T : MyEnumerator
    {
        foreach (object x in new Enumerable<T>())
        {
            System.Console.WriteLine(x);
        }
    }
}

public class Enumerable<T>
{
    public T GetEnumerator() { return default(T); }
}

interface MyEnumerator
{
    object Current { get; }
    bool MoveNext();
}
";
            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("Enumerable<T>", info.CollectionType.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Object, info.ElementType.SpecialType);
            Assert.Equal("T Enumerable<T>.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object MyEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean MyEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.Identity, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.Boxing, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Object x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal("Enumerable<T>", boundNode.Expression.Type.ToTestDisplayString());
            Assert.Equal("Enumerable<T>", ((BoundConversion)boundNode.Expression).Operand.Type.ToTestDisplayString());
        }

        // Copied from TestSuccessPatternStruct - only change is that Goo parameter is now nullable.
        [WorkItem(544908, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544908")]
        [Fact]
        public void TestSuccessNullableCollection()
        {
            var text = @"
struct C
{
    void Goo(Enumerable? e)
    {
        foreach (long x in e) { }
    }
}

struct Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

struct Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";
            var boundNode = GetBoundForEachStatement(text);

            // NOTE: info is exactly as if the collection was not nullable.
            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal("Enumerable", info.CollectionType.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Int32, info.ElementType.SpecialType);
            Assert.Equal("Enumerator Enumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Int32 Enumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean Enumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.False(info.NeedsDisposeMethod); // Definitely not disposable
            Assert.Equal(ConversionKind.Identity, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.ImplicitNumeric, boundNode.ElementConversion.Kind);
            Assert.Equal("System.Int64 x", boundNode.IterationVariables.Single().ToTestDisplayString());
            Assert.Equal("Enumerable", boundNode.Expression.Type.ToTestDisplayString());
            Assert.Equal("Enumerable", ((BoundConversion)boundNode.Expression).Operand.Type.ToTestDisplayString());
        }

        [WorkItem(542193, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542193")]
        [Fact]
        public void ForEachStmtWithSyntaxError1()
        {
            var text = @"
public class Test
{
  public static void Main(string [] args)
  {
    foreach(int; i < 5; i++)
                           {
                           }
  }
}
";
            var boundNode = GetBoundForEachStatement(text,
                // (6,13): error CS1525: Invalid expression term 'int'
                //     foreach(int; i < 5; i++)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 13),
                // (6,16): error CS1515: 'in' expected
                //     foreach(int; i < 5; i++)
                Diagnostic(ErrorCode.ERR_InExpected, ";").WithLocation(6, 16),
                // (6,16): error CS0230: Type and identifier are both required in a foreach statement
                //     foreach(int; i < 5; i++)
                Diagnostic(ErrorCode.ERR_BadForeachDecl, ";").WithLocation(6, 16),
                // (6,16): error CS1525: Invalid expression term ';'
                //     foreach(int; i < 5; i++)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 16),
                // (6,16): error CS1026: ) expected
                //     foreach(int; i < 5; i++)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(6, 16),
                // (6,28): error CS1002: ; expected
                //     foreach(int; i < 5; i++)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 28),
                // (6,28): error CS1513: } expected
                //     foreach(int; i < 5; i++)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 28),
                // (6,18): error CS0103: The name 'i' does not exist in the current context
                //     foreach(int; i < 5; i++)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "i").WithArguments("i").WithLocation(6, 18),
                // (6,18): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //     foreach(int; i < 5; i++)
                Diagnostic(ErrorCode.ERR_IllegalStatement, "i < 5").WithLocation(6, 18),
                // (6,25): error CS0103: The name 'i' does not exist in the current context
                //     foreach(int; i < 5; i++)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "i").WithArguments("i").WithLocation(6, 25));
            Assert.Null(boundNode.EnumeratorInfoOpt);
        }

        [WorkItem(545489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545489")]
        [Fact]
        public void ForEachOnErrorTypesWithoutReferences()
        {
            var text = @"
public class condGenClass<T> { }

public class Test
{
    static void Main()
    {
        Type[] types = {typeof(condGenClass<int>)};
        foreach (Type t in types)
        {
        }
    }
}
";
            var compilation = CreateEmptyCompilation(text);
            Assert.NotEmpty(compilation.GetDiagnostics());
        }

        [WorkItem(545489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545489")]
        [Fact]
        public void ForEachWithoutCorlib_Array()
        {
            var text = @"
public class Test
{
    static void Main()
    {
        Test[] array = new Test[] { new Test() };
        foreach (Test t in array)
        {
        }
    }
}
";
            var compilation = CreateEmptyCompilation(text);
            Assert.NotEmpty(compilation.GetDiagnostics());
        }

        [WorkItem(545489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545489")]
        [Fact]
        public void ForEachWithoutCorlib_String()
        {
            var text = @"
public class Test
{
    static void Main()
    {
        foreach (char ch in ""hello"")
        {
        }
    }
}
";
            var compilation = CreateEmptyCompilation(text);
            Assert.NotEmpty(compilation.GetDiagnostics());
        }

        [WorkItem(545186, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545186")]
        [Fact]
        public void TestForEachWithConditionalMethod()
        {
            string source = @"
using System;
using System.Collections;

namespace ForEachTest
{
    public class BaseEnumerator
    {
        public bool MoveNext() { Console.WriteLine(""BaseEnumerator::MoveNext()""); return false; }
        public int Current { get { Console.WriteLine(""BaseEnumerator::Current""); return 1; } }
    }
    public class BaseEnumeratorImpl : IEnumerator
    {
        public bool MoveNext() { Console.WriteLine(""BaseEnumeratorImpl::MoveNext()""); return false; }
        public Object Current { get { Console.WriteLine(""BaseEnumeratorImpl::Current""); return 1; } }
        public void Reset() { Console.WriteLine(""BaseEnumeratorImpl::Reset""); }
    }
    public class BasePattern
    {
        public BaseEnumerator GetEnumerator() { Console.WriteLine(""BasePattern::GetEnumerator()""); return new BaseEnumerator(); }
    }
    namespace ValidBaseTest
    {
        public class Derived5 : BasePattern, IEnumerable
        {
            IEnumerator IEnumerable.GetEnumerator() { Console.WriteLine(""<Interface> Derived5.GetEnumerator()""); return new BaseEnumeratorImpl(); }
            [System.Diagnostics.Conditional(""CONDITIONAL"")]
            new public void GetEnumerator() { Console.WriteLine(""ERROR: <Conditional Method not in scope> Derived5.GetEnumerator()""); }
        }
    }
    public class Logger
    {
        public static void Main()
        {
            foreach (int i in new ValidBaseTest.Derived5()) { }
        }
    }
}
";
            // Without "CONDITIONAL" defined: Succeed
            string expectedOutput = @"<Interface> Derived5.GetEnumerator()
BaseEnumeratorImpl::MoveNext()";
            CompileAndVerify(source, expectedOutput: expectedOutput);

            // With "CONDITIONAL" defined: Fail

            // (a) Preprocessor symbol defined through command line parse options
            var options = new CSharpParseOptions(preprocessorSymbols: ImmutableArray.Create("CONDITIONAL"), documentationMode: DocumentationMode.None);
            CreateCompilation(source, parseOptions: options).VerifyDiagnostics(
                // (35,31): error CS0117: 'void' does not contain a definition for 'Current'
                //             foreach (int i in new ValidBaseTest.Derived5()) { }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new ValidBaseTest.Derived5()").WithArguments("void", "Current").WithLocation(35, 31),
                // (35,31): error CS0202: foreach requires that the return type 'void' of 'ForEachTest.ValidBaseTest.Derived5.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //             foreach (int i in new ValidBaseTest.Derived5()) { }
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new ValidBaseTest.Derived5()").WithArguments("void", "ForEachTest.ValidBaseTest.Derived5.GetEnumerator()").WithLocation(35, 31));

            // (b) Preprocessor symbol defined in source
            string condDefSource = "#define CONDITIONAL" + source;
            CreateCompilation(condDefSource).VerifyDiagnostics(
                // (35,31): error CS0117: 'void' does not contain a definition for 'Current'
                //             foreach (int i in new ValidBaseTest.Derived5()) { }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new ValidBaseTest.Derived5()").WithArguments("void", "Current").WithLocation(35, 31),
                // (35,31): error CS0202: foreach requires that the return type 'void' of 'ForEachTest.ValidBaseTest.Derived5.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //             foreach (int i in new ValidBaseTest.Derived5()) { }
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new ValidBaseTest.Derived5()").WithArguments("void", "ForEachTest.ValidBaseTest.Derived5.GetEnumerator()").WithLocation(35, 31));
        }

        [WorkItem(649809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649809")]
        [Fact]
        public void ArrayOfNullableOfError()
        {
            var source = @"
class F
{
    void Test()
    {
        E[] a1 = null;
        E?[] a2 = null;
        S<E>[] a3 = null;
        IEnumerable<E> e1 = null;
        IEnumerable<E?> e2 = null;
        IEnumerable<S<E>> e3 = null;

        foreach (E e in a1) { }
        foreach (E e in a2) { }
        foreach (E e in a2) { }
        foreach (E e in e1) { }
        foreach (E e in e2) { }

        foreach (E? e in a1) { }
        foreach (E? e in a2) { } // used to assert
        foreach (E? e in e1) { }
        foreach (E? e in e2) { }

        foreach (S<E> e in a3) { }
        foreach (S<E> e in e3) { }
    }
}

public struct S<T> { }
";

            Assert.NotEmpty(CreateCompilation(source).GetDiagnostics());
        }

        [WorkItem(667616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667616")]
        [Fact]
        public void PortableLibraryStringForEach()
        {
            var source = @"
class C
{
    void Test(string s)
    {
        foreach (var c in s) { }
    }
}
";

            var comp = CreateEmptyCompilation(source, new[] { MscorlibRefPortable });
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var loopSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();

            var loopInfo = model.GetForEachStatementInfo(loopSyntax);
            Assert.Equal<ISymbol>(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator), loopInfo.GetEnumeratorMethod);
            Assert.Equal<ISymbol>(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current), loopInfo.CurrentProperty);
            Assert.Equal<ISymbol>(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), loopInfo.MoveNextMethod);
            Assert.Equal<ISymbol>(comp.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), loopInfo.DisposeMethod);

            // The spec says that the element type is object.
            // Therefore, we should infer object for "var".
            Assert.Equal(SpecialType.System_Object, loopInfo.CurrentProperty.Type.SpecialType);

            // However, to match dev11, we actually infer "char" for "var".
            var typeInfo = model.GetTypeInfo(loopSyntax.Type);
            Assert.Equal(SpecialType.System_Char, typeInfo.Type.SpecialType);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);

            var conv = model.GetConversion(loopSyntax.Type);
            Assert.Equal(ConversionKind.Identity, conv.Kind);
        }

        [WorkItem(529956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529956")]
        [Fact]
        public void CastArrayToIEnumerable()
        {
            var source = @"
using System.Collections;
 
class C
{
    static void Main(string[] args)
    {
        foreach (C x in args) { }
        foreach (C x in (IEnumerable)args) { }
    }
 
    public static implicit operator C(string s)
    {
        return new C();
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var udc = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.ImplicitConversionName);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var loopSyntaxes = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().ToArray();
            Assert.Equal(2, loopSyntaxes.Length);

            var loopInfo0 = model.GetForEachStatementInfo(loopSyntaxes[0]);
            Assert.Equal<ISymbol>(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator), loopInfo0.GetEnumeratorMethod);
            Assert.Equal<ISymbol>(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current), loopInfo0.CurrentProperty);
            Assert.Equal<ISymbol>(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), loopInfo0.MoveNextMethod);
            Assert.Equal<ISymbol>(comp.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), loopInfo0.DisposeMethod);
            Assert.Equal(SpecialType.System_String, loopInfo0.ElementType.SpecialType);
            Assert.Equal(udc, loopInfo0.ElementConversion.Method);
            Assert.Equal(ConversionKind.ExplicitReference, loopInfo0.CurrentConversion.Kind);

            var loopInfo1 = model.GetForEachStatementInfo(loopSyntaxes[1]);
            Assert.Equal(loopInfo0.GetEnumeratorMethod, loopInfo1.GetEnumeratorMethod);
            Assert.Equal(loopInfo0.CurrentProperty, loopInfo1.CurrentProperty);
            Assert.Equal(loopInfo0.MoveNextMethod, loopInfo1.MoveNextMethod);
            Assert.Equal(loopInfo0.DisposeMethod, loopInfo1.DisposeMethod);
            Assert.Equal(SpecialType.System_Object, loopInfo1.ElementType.SpecialType); // No longer string.
            Assert.Null(loopInfo1.ElementConversion.Method); // No longer using UDC.
            Assert.Equal(ConversionKind.Identity, loopInfo1.CurrentConversion.Kind); // Now identity.
        }

        [WorkItem(762179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/762179")]
        [Fact]
        public void MissingObjectType()
        {
            var text = @"
class C
{
    void Goo(Enumerable e)
    {
        foreach (Element x in e) { }
    }
}

struct Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

struct Enumerator
{
    public Element Current { get { return null; } }
    public bool MoveNext() { return false; }
}

class Element
{
}
";
            var comp = CreateEmptyCompilation(text);
            comp.GetDiagnostics();
        }

        [WorkItem(798000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798000")]
        [Fact]
        public void MissingNullableValue()
        {
            var text = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Nullable<T> { }
    public struct Boolean { }
}

namespace System.Collections.Generic
{
    public interface IEnumerable<T>
    {
        IEnumerator<T> GetEnumerator();
    }

    public interface IEnumerator<T>
    {
        T Current { get; }
        bool MoveNext();
    }
}

class C
{
    void Goo(System.Collections.Generic.IEnumerable<C>? e)
    {
        foreach (var c in e) { }
    }
}
";
            var comp = CreateEmptyCompilation(text);
            comp.VerifyDiagnostics(
                // (30,27): error CS0656: Missing compiler required member 'System.Nullable`1.get_Value'
                //         foreach (var c in e) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e").WithArguments("System.Nullable`1", "get_Value"));
        }

        [WorkItem(798000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798000")]
        [Fact]
        public void MissingIEnumerableTGetEnumerator()
        {
            var text = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Boolean { }
}

namespace System.Collections.Generic
{
    public interface IEnumerable<T> { }
}

public class Enumerable<T> : System.Collections.Generic.IEnumerable<T> { }

class C
{
    void Goo(System.Collections.Generic.IEnumerable<C> e1, Enumerable<C> e2)
    {
        foreach (var c in e1) { } // Pattern
        foreach (var c in e2) { } // Interface
    }
}
";
            var comp = CreateEmptyCompilation(text);
            comp.VerifyDiagnostics(
                // For pattern:

                // (22,27): error CS1579: foreach statement cannot operate on variables of type 'System.Collections.Generic.IEnumerable<C>' because 'System.Collections.Generic.IEnumerable<C>' does not contain a public definition for 'GetEnumerator'
                //         foreach (var c in e1) { }
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "e1").WithArguments("System.Collections.Generic.IEnumerable<C>", "GetEnumerator"),

                // For interface:

                // (23,27): error CS0656: Missing compiler required member 'System.Collections.Generic.IEnumerable`1.GetEnumerator'
                //         foreach (var c in e2) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.Generic.IEnumerable`1", "GetEnumerator"),
                // (23,27): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.MoveNext'
                //         foreach (var c in e2) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.IEnumerator", "MoveNext"));
        }

        [WorkItem(798000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798000")]
        [Fact]
        public void MissingIEnumeratorTMoveNext()
        {
            var text = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Boolean { }
}

namespace System.Collections.Generic
{
    public interface IEnumerable<T>
    {
        IEnumerator<T> GetEnumerator();
    }

    public interface IEnumerator<T>
    {
        T Current { get; }
    }
}

public class Enumerable<T> : System.Collections.Generic.IEnumerable<T>
{
    System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
    {
        return null;
    }
}

class C
{
    void Goo(System.Collections.Generic.IEnumerable<C> e1, Enumerable<C> e2)
    {
        foreach (var c in e1) { } // Pattern
        foreach (var c in e2) { } // Interface
    }
}
";
            var comp = CreateEmptyCompilation(text);
            comp.VerifyDiagnostics(
                // For pattern:

                // (37,27): error CS0117: 'System.Collections.Generic.IEnumerator<C>' does not contain a definition for 'MoveNext'
                //         foreach (var c in e1) { } // Pattern
                Diagnostic(ErrorCode.ERR_NoSuchMember, "e1").WithArguments("System.Collections.Generic.IEnumerator<C>", "MoveNext"),

                // For interface:

                // (37,27): error CS0202: foreach requires that the return type 'System.Collections.Generic.IEnumerator<C>' of 'System.Collections.Generic.IEnumerable<C>.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //         foreach (var c in e1) { } // Pattern
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "e1").WithArguments("System.Collections.Generic.IEnumerator<C>", "System.Collections.Generic.IEnumerable<C>.GetEnumerator()"),
                // (38,27): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.MoveNext'
                //         foreach (var c in e2) { } // Interface
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.IEnumerator", "MoveNext"));
        }

        [WorkItem(798000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798000")]
        [Fact]
        public void MissingIEnumeratorTCurrent()
        {
            var text = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Boolean { }
}

namespace System.Collections
{
    public interface IEnumerator
    {
        bool MoveNext();
    }
}

namespace System.Collections.Generic
{
    public interface IEnumerable<T>
    {
        IEnumerator<T> GetEnumerator();
    }

    public interface IEnumerator<T> : IEnumerator
    {
        //T Current { get; }
    }
}

public class Enumerable<T> : System.Collections.Generic.IEnumerable<T>
{
    System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
    {
        return null;
    }
}

class C
{
    void Goo(System.Collections.Generic.IEnumerable<C> e1, Enumerable<C> e2)
    {
        foreach (var c in e1) { } // Pattern
        foreach (var c in e2) { } // Interface
    }
}
";
            var comp = CreateEmptyCompilation(text);
            comp.VerifyDiagnostics(
                // For pattern:

                // (44,27): error CS0117: 'System.Collections.Generic.IEnumerator<C>' does not contain a definition for 'Current'
                //         foreach (var c in e1) { } // Pattern
                Diagnostic(ErrorCode.ERR_NoSuchMember, "e1").WithArguments("System.Collections.Generic.IEnumerator<C>", "Current"),
                // (44,27): error CS0202: foreach requires that the return type 'System.Collections.Generic.IEnumerator<C>' of 'System.Collections.Generic.IEnumerable<C>.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //         foreach (var c in e1) { } // Pattern
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "e1").WithArguments("System.Collections.Generic.IEnumerator<C>", "System.Collections.Generic.IEnumerable<C>.GetEnumerator()"),

                // For interface:

                // (45,27): error CS0656: Missing compiler required member 'System.Collections.Generic.IEnumerator`1.get_Current'
                //         foreach (var c in e2) { } // Interface
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.Generic.IEnumerator`1", "get_Current"));
        }

        [WorkItem(798000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798000")]
        [Fact]
        public void MissingIEnumeratorTCurrentGetter()
        {
            var text = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Boolean { }
}

namespace System.Collections
{
    public interface IEnumerator
    {
        bool MoveNext();
    }
}

namespace System.Collections.Generic
{
    public interface IEnumerable<T>
    {
        IEnumerator<T> GetEnumerator();
    }

    public interface IEnumerator<T> : IEnumerator
    {
        T Current { /* get; */ set; }
    }
}

public class Enumerable<T> : System.Collections.Generic.IEnumerable<T>
{
    System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
    {
        return null;
    }
}

class C
{
    void Goo(System.Collections.Generic.IEnumerable<C> e1, Enumerable<C> e2)
    {
        foreach (var c in e1) { } // Pattern
        foreach (var c in e2) { } // Interface
    }
}
";
            var comp = CreateEmptyCompilation(text);
            comp.VerifyDiagnostics(
                // For pattern:

                // (44,27): error CS0202: foreach requires that the return type 'System.Collections.Generic.IEnumerator<C>' of 'System.Collections.Generic.IEnumerable<C>.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //         foreach (var c in e1) { } // Pattern
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "e1").WithArguments("System.Collections.Generic.IEnumerator<C>", "System.Collections.Generic.IEnumerable<C>.GetEnumerator()"),

                // For interface:

                // (45,27): error CS0656: Missing compiler required member 'System.Collections.Generic.IEnumerator`1.get_Current'
                //         foreach (var c in e2) { } // Interface
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.Generic.IEnumerator`1", "get_Current"));
        }

        [WorkItem(798000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798000")]
        [Fact]
        public void MissingIEnumerableGetEnumerator()
        {
            var text = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Boolean { }
}

namespace System.Collections
{
    public interface IEnumerable { }
}

public class Enumerable : System.Collections.IEnumerable { }

class C
{
    void Goo(System.Collections.IEnumerable e1, Enumerable e2)
    {
        foreach (var c in e1) { } // Pattern
        foreach (var c in e2) { } // Interface
    }
}
";
            var comp = CreateEmptyCompilation(text);
            comp.VerifyDiagnostics(
                // For pattern:

                // (22,27): error CS1579: foreach statement cannot operate on variables of type 'System.Collections.IEnumerable' because 'System.Collections.IEnumerable' does not contain a public definition for 'GetEnumerator'
                //         foreach (var c in e1) { }
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "e1").WithArguments("System.Collections.IEnumerable", "GetEnumerator"),

                // For interface:

                // (23,27): error CS0656: Missing compiler required member 'System.Collections.IEnumerable.GetEnumerator'
                //         foreach (var c in e2) { } // Interface
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.IEnumerable", "GetEnumerator"),
                // (23,27): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.get_Current'
                //         foreach (var c in e2) { } // Interface
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.IEnumerator", "get_Current"),
                // (23,27): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.MoveNext'
                //         foreach (var c in e2) { } // Interface
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.IEnumerator", "MoveNext"));
        }

        [WorkItem(798000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798000")]
        [Fact]
        public void MissingIEnumeratorMoveNext()
        {
            var text = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Boolean { }
}

namespace System.Collections
{
    public interface IEnumerable
    {
        IEnumerator GetEnumerator();
    }

    public interface IEnumerator
    {
        object Current { get; }
    }
}

public class Enumerable : System.Collections.IEnumerable
{
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return null;
    }
}

class C
{
    void Goo(System.Collections.IEnumerable e1, Enumerable e2)
    {
        foreach (var c in e1) { } // Pattern
        foreach (var c in e2) { } // Interface
    }
}
";
            var comp = CreateEmptyCompilation(text);
            comp.VerifyDiagnostics(
                // For pattern:

                // (37,27): error CS0117: 'System.Collections.IEnumerator' does not contain a definition for 'MoveNext'
                //         foreach (var c in e1) { } // Pattern
                Diagnostic(ErrorCode.ERR_NoSuchMember, "e1").WithArguments("System.Collections.IEnumerator", "MoveNext"),

                // For interface:

                // (37,27): error CS0202: foreach requires that the return type 'System.Collections.IEnumerator' of 'System.Collections.IEnumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //         foreach (var c in e1) { } // Pattern
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "e1").WithArguments("System.Collections.IEnumerator", "System.Collections.IEnumerable.GetEnumerator()"),
                // (38,27): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.MoveNext'
                //         foreach (var c in e2) { } // Interface
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.IEnumerator", "MoveNext"));
        }

        [WorkItem(798000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798000")]
        [Fact]
        public void MissingIEnumeratorCurrent()
        {
            var text = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Boolean { }
}

namespace System.Collections
{
    public interface IEnumerable
    {
        IEnumerator GetEnumerator();
    }

    public interface IEnumerator
    {
        bool MoveNext();
        //object Current { get; }
    }
}

public class Enumerable : System.Collections.IEnumerable
{
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return null;
    }
}

class C
{
    void Goo(System.Collections.IEnumerable e1, Enumerable e2)
    {
        foreach (var c in e1) { } // Pattern
        foreach (var c in e2) { } // Interface
    }
}
";
            var comp = CreateEmptyCompilation(text);
            comp.VerifyDiagnostics(
                // For pattern:

                // (44,27): error CS0117: 'System.Collections.IEnumerator' does not contain a definition for 'Current'
                //         foreach (var c in e1) { } // Pattern
                Diagnostic(ErrorCode.ERR_NoSuchMember, "e1").WithArguments("System.Collections.IEnumerator", "Current"),
                // (44,27): error CS0202: foreach requires that the return type 'System.Collections.IEnumerator' of 'System.Collections.IEnumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //         foreach (var c in e1) { } // Pattern
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "e1").WithArguments("System.Collections.IEnumerator", "System.Collections.IEnumerable.GetEnumerator()"),

                // For interface:

                // (45,27): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.get_Current'
                //         foreach (var c in e2) { } // Interface
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.IEnumerator", "get_Current"));
        }

        [WorkItem(798000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/798000")]
        [Fact]
        public void MissingIEnumeratorCurrentGetter()
        {
            var text = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Boolean { }
}

namespace System.Collections
{
    public interface IEnumerable
    {
        IEnumerator GetEnumerator();
    }

    public interface IEnumerator
    {
        bool MoveNext();
        object Current { /* get; */ set; }
    }
}

public class Enumerable : System.Collections.IEnumerable
{
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return null;
    }
}

class C
{
    void Goo(System.Collections.IEnumerable e1, Enumerable e2)
    {
        foreach (var c in e1) { } // Pattern
        foreach (var c in e2) { } // Interface
    }
}
";
            var comp = CreateEmptyCompilation(text);
            comp.VerifyDiagnostics(
                // For pattern:

                // (44,27): error CS0202: foreach requires that the return type 'System.Collections.IEnumerator' of 'System.Collections.IEnumerable.GetEnumerator()' must have a suitable public MoveNext method and public Current property
                //         foreach (var c in e1) { } // Pattern
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "e1").WithArguments("System.Collections.IEnumerator", "System.Collections.IEnumerable.GetEnumerator()"),

                // For interface:

                // (45,27): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.get_Current'
                //         foreach (var c in e2) { } // Interface
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e2").WithArguments("System.Collections.IEnumerator", "get_Current"));
        }

        [WorkItem(530381, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530381")]
        [Fact]
        public void BadCollectionCascadingErrors()
        {
            var source = @"
class Program
{
    static void Main()
    {
        foreach(var x in Goo) { }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,26): error CS0103: The name 'Goo' does not exist in the current context
                //         foreach(var x in Goo) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Goo").WithArguments("Goo").WithLocation(6, 26));
        }

        [WorkItem(847507, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/847507")]
        [Fact]
        public void InferIterationVariableTypeWithErrors()
        {
            var source = @"
class Program
{
    static void Main()
    {
        foreach(var x in new string[1]) { }
    }
}

namespace System
{
    public class Object { }
    public class String : Object { }
}
";
            var comp = CreateEmptyCompilation(source); // Lots of errors, since corlib is missing.
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var foreachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var localSymbol = model.GetDeclaredSymbol(foreachSyntax);

            // Code Path 1: SourceLocalSymbol.Type.
            var localSymbolType = localSymbol.Type;
            Assert.Equal(SpecialType.System_String, localSymbolType.SpecialType);
            Assert.NotEqual(TypeKind.Error, localSymbolType.TypeKind);

            // Code Path 2: SemanticModel.
            var varSyntax = foreachSyntax.Type;
            var info = model.GetSymbolInfo(varSyntax); // Used to assert.
            Assert.Equal(localSymbolType, info.Symbol);
        }

        [WorkItem(667275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667275")]
        [Fact]
        public void Repro667275()
        {
            // This is not the simplest repro, but it is the one from the bug.
            var source = @"
using System.Collections.Generic;

class myClass<T>
{
    public static implicit operator T(myClass<T> m) { return default(T); }
}

class Test
{
    static void Main()
    {
        var myObj = new myClass<List<string>>();
        foreach (var x in myObj) { }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,27): error CS1579: foreach statement cannot operate on variables of type 'myClass<System.Collections.Generic.List<string>>' because 'myClass<System.Collections.Generic.List<string>>' does not contain a public definition for 'GetEnumerator'
                //         foreach (var x in myObj) { }
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "myObj").WithArguments("myClass<System.Collections.Generic.List<string>>", "GetEnumerator").WithLocation(14, 27));
        }

        [WorkItem(667275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667275")]
        [Fact]
        public void Repro667275_Simplified()
        {
            // No generics, no foreach.
            var source = @"
using System.Collections;

class Dummy
{
    public static implicit operator MyEnumerable(Dummy d) 
    { 
        return null; 
    }
}

public class MyEnumerable : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new System.NotImplementedException();
    }
}


class Test
{
    static void Main()
    {
        Dummy d = new Dummy();
        MyEnumerable m = d; // succeeds
        IEnumerable i = d; // fails
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (27,25): error CS0266: Cannot implicitly convert type 'Dummy' to 'System.Collections.IEnumerable'. An explicit conversion exists (are you missing a cast?)
                //         IEnumerable i = d; // fails
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "d").WithArguments("Dummy", "System.Collections.IEnumerable").WithLocation(27, 25));
        }

        [WorkItem(963197, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/963197")]
        [Fact]
        public void Repro963197()
        {
            var source =
@"using System;
 
class Program
{
    public static string B = ""B"";
    public static string C = ""C"";
    static void Main(string[] args)
    {
        foreach (var a in new { B, C })
        {
            Console.WriteLine(a);
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,27): error CS1579: foreach statement cannot operate on variables of type '<anonymous type: string B, string C>' because '<anonymous type: string B, string C>' does not contain a public definition for 'GetEnumerator'
                //         foreach (var a in new { B, C })
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "new { B, C }").WithArguments("<anonymous type: string B, string C>", "GetEnumerator").WithLocation(9, 27)
            );
        }

        [WorkItem(11387, "https://github.com/dotnet/roslyn/issues/11387")]
        [Fact]
        public void StringNotIEnumerable()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public struct Void { }
    public class ValueType { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Char { }

    public class String 
    {
        public int Length => 2;

        [System.Runtime.CompilerServices.IndexerName(""Chars"")]
        public char this[int i] => 'a';
    }
    
    public interface IDisposable
    {
        void Dispose();
    }

    public abstract class Attribute
    {
        protected Attribute() { }
    }
}

namespace System.Runtime.CompilerServices
{
    using System;
 
    public sealed class IndexerNameAttribute: Attribute
    {
        public IndexerNameAttribute(String indexerName)
        {}
    }
}

namespace System.Reflection {
    
    using System;
 
    public sealed class DefaultMemberAttribute : Attribute
    {
        public DefaultMemberAttribute(String memberName) 
        {}
    }
}

namespace System.Collections
{
    public interface IEnumerable
    {
        IEnumerator GetEnumerator();
    }

    public interface IEnumerator
    {
        object Current { get; }
        bool MoveNext();
    }
}";
            var compilation1 = CreateEmptyCompilation(source1, assemblyName: GetUniqueName());
            var reference1 = MetadataReference.CreateFromStream(compilation1.EmitToStream());
            var text =
@"class C
{
    static void M(string s)
    {
        foreach (var c in s)
        {
            // comment
        }
    }
}";

            var comp = CreateEmptyCompilation(text, new[] { reference1 });
            CompileAndVerify(comp, verify: Verification.Fails).
            VerifyIL("C.M", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (string V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  br.s       IL_0012
  IL_0006:  ldloc.0
  IL_0007:  ldloc.1
  IL_0008:  callvirt   ""char string.this[int].get""
  IL_000d:  pop
  IL_000e:  ldloc.1
  IL_000f:  ldc.i4.1
  IL_0010:  add
  IL_0011:  stloc.1
  IL_0012:  ldloc.1
  IL_0013:  ldloc.0
  IL_0014:  callvirt   ""int string.Length.get""
  IL_0019:  blt.s      IL_0006
  IL_001b:  ret
}
");

            var boundNode = GetBoundForEachStatement(text);

            ForEachEnumeratorInfo info = boundNode.EnumeratorInfoOpt;
            Assert.NotNull(info);
            Assert.Equal(SpecialType.System_String, info.CollectionType.SpecialType);
            Assert.Equal(SpecialType.System_Char, info.ElementType.SpecialType);
            Assert.Equal("System.CharEnumerator System.String.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Char System.CharEnumerator.Current.get", info.CurrentPropertyGetter.ToTestDisplayString());
            Assert.Equal("System.Boolean System.CharEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.True(info.NeedsDisposeMethod);
            Assert.Equal(ConversionKind.Identity, info.CollectionConversion.Kind);
            Assert.Equal(ConversionKind.Identity, info.CurrentConversion.Kind);
            Assert.Equal(ConversionKind.ImplicitReference, info.EnumeratorConversion.Kind);

            Assert.Equal(ConversionKind.Identity, boundNode.ElementConversion.Kind);
            Assert.Equal(SpecialType.System_Char, boundNode.IterationVariables.Single().Type.SpecialType);
        }



        private static BoundForEachStatement GetBoundForEachStatement(string text, params DiagnosticDescription[] diagnostics)
        {
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });

            comp.VerifyDiagnostics(diagnostics);

            var syntaxNode =
                (CommonForEachStatementSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.ForEachStatement).AsNode() ??
                (CommonForEachStatementSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.ForEachVariableStatement).AsNode();
            var treeModel = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree);
            var memberModel = treeModel.GetMemberModel(syntaxNode);

            BoundForEachStatement boundNode = (BoundForEachStatement)memberModel.GetUpperBoundNode(syntaxNode);

            // Make sure that the bound node info is exposed correctly in the API
            ForEachEnumeratorInfo enumeratorInfo = boundNode.EnumeratorInfoOpt;
            ForEachStatementInfo statementInfo = treeModel.GetForEachStatementInfo(syntaxNode);

            if (enumeratorInfo == null)
            {
                Assert.Equal(default(ForEachStatementInfo), statementInfo);
            }
            else
            {
                Assert.Equal(enumeratorInfo.GetEnumeratorMethod, statementInfo.GetEnumeratorMethod);
                Assert.Equal(enumeratorInfo.CurrentPropertyGetter, statementInfo.CurrentProperty.GetMethod);
                Assert.Equal(enumeratorInfo.MoveNextMethod, statementInfo.MoveNextMethod);

                if (enumeratorInfo.NeedsDisposeMethod)
                {
                    Assert.Equal("void System.IDisposable.Dispose()", statementInfo.DisposeMethod.ToTestDisplayString());
                }
                else
                {
                    Assert.Null(statementInfo.DisposeMethod);
                }

                Assert.Equal(enumeratorInfo.ElementType, statementInfo.ElementType);
                Assert.Equal(boundNode.ElementConversion, statementInfo.ElementConversion);
                Assert.Equal(enumeratorInfo.CurrentConversion, statementInfo.CurrentConversion);
            }

            return boundNode;
        }

        [WorkItem(1100741, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100741")]
        [Fact]
        public void Bug1100741()
        {
            var source = @"
namespace ImmutableObjectGraph
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using IdentityFieldType = System.UInt32;

    public static class RecursiveTypeExtensions
    {
/// <summary>Gets the recursive parent of the specified value, or <c>null</c> if none could be found.</summary>
internal ParentedRecursiveType<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveTypeFromFamily.TypeName #>> GetParentedNode(<#= templateType.RequiredIdentityField.TypeName #> identity) {
    if (this.Identity == identity) {
        return new ParentedRecursiveType<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveTypeFromFamily.TypeName #>>(this, null);
    }

    if (this.LookupTable != null) {
        System.Collections.Generic.KeyValuePair<<#= templateType.RecursiveType.TypeName #>, <#= templateType.RequiredIdentityField.TypeName #>> lookupValue;
        if (this.LookupTable.TryGetValue(identity, out lookupValue)) {
            var parentIdentity = lookupValue.Value;
            return new ParentedRecursiveType<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveTypeFromFamily.TypeName #>>(this.LookupTable[identity].Key, (<#= templateType.RecursiveParent.TypeName #>)this.Find(parentIdentity));
        }
    } else {
        // No lookup table means we have to aggressively search each child.
        foreach (var child in this.Children) {
            if (child.Identity.Equals(identity)) {
                return new ParentedRecursiveType<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveTypeFromFamily.TypeName #>>(child, this);
            }

            var recursiveChild = child as <#= templateType.RecursiveParent.TypeName #>;
            if (recursiveChild != null) {
                var childResult = recursiveChild.GetParentedNode(identity);
                if (childResult.Value != null) {
                    return childResult;
                }
            } 
        }
    }

    return default(ParentedRecursiveType<<#= templateType.RecursiveParent.TypeName #>, <#= templateType.RecursiveTypeFromFamily.TypeName #>>);
}
    }
}
";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().Where(n => n.Kind() == SyntaxKind.ForEachStatement).OfType<ForEachStatementSyntax>().Single();
            var model = compilation.GetSemanticModel(tree);

            Assert.Null(model.GetDeclaredSymbol(node));
        }

        [Fact, WorkItem(1733, "https://github.com/dotnet/roslyn/issues/1733")]
        public void MissingBaseType()
        {
            var source1 = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}
";

            var comp1 = CreateEmptyCompilation(source1, options: TestOptions.DebugDll, assemblyName: "MissingBaseType1");
            comp1.VerifyDiagnostics();

            var source2 = @"
public class Enumerable  
{
    public Enumerator GetEnumerator()
    {
        return default(Enumerator);
    }
}

public struct Enumerator
{
    public int Current
    {
        get
        {
            return 0;
        }
    }

    public bool MoveNext()
    {
        return false;
    }
}";

            var comp2 = CreateEmptyCompilation(source2, new[] { comp1.ToMetadataReference() }, options: TestOptions.DebugDll);
            comp2.VerifyDiagnostics();

            var source3 = @"
namespace System
{
    public class Object { }

    public class ValueType {}
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}
";

            var comp3 = CreateEmptyCompilation(source3, options: TestOptions.DebugDll, assemblyName: "MissingBaseType2");
            comp3.VerifyDiagnostics();

            var source4 = @"
class Program
{
    static void Main()
    {
        foreach (var x in new Enumerable())
        { }
    }
}";

            var comp4 = CreateEmptyCompilation(source4, new[] { comp2.ToMetadataReference(), comp3.ToMetadataReference() });
            comp4.VerifyDiagnostics(
                // (6,9): error CS0012: The type 'ValueType' is defined in an assembly that is not referenced. You must add a reference to assembly 'MissingBaseType1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         foreach (var x in new Enumerable())
                Diagnostic(ErrorCode.ERR_NoTypeDef, @"foreach (var x in new Enumerable())
        { }").WithArguments("System.ValueType", "MissingBaseType1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 9)
                );
        }

        [Fact]
        [WorkItem(26585, "https://github.com/dotnet/roslyn/issues/26585")]
        public void ForEachIteratorWithCurrentRefKind_Async_Ref()
        {
            CreateCompilation(@"
using System.Threading.Tasks;

class E
{
    public class Enumerator
    {
        public ref int Current => throw null;
        public bool MoveNext() => throw null;
    }

    public Enumerator GetEnumerator() => new Enumerator();
}
class C
{
    public async static Task Test()
    {
        await Task.CompletedTask;

        foreach (ref int x in new E())
        {
            System.Console.Write(x);
        }
    }
}").VerifyDiagnostics(
                // (20,26): error CS8177: Async methods cannot have by-reference locals
                //         foreach (ref int x in new E())
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "x").WithLocation(20, 26));
        }

        [Fact]
        [WorkItem(26585, "https://github.com/dotnet/roslyn/issues/26585")]
        public void ForEachIteratorWithCurrentRefKind_Async_RefReadonly()
        {
            CreateCompilation(@"
using System.Threading.Tasks;

class E
{
    public class Enumerator
    {
        public ref readonly int Current => throw null;
        public bool MoveNext() => throw null;
    }

    public Enumerator GetEnumerator() => new Enumerator();
}
class C
{
    public async static Task Test()
    {
        await Task.CompletedTask;

        foreach (ref readonly int x in new E())
        {
            System.Console.Write(x);
        }
    }
}").VerifyDiagnostics(
                // (20,35): error CS8177: Async methods cannot have by-reference locals
                //         foreach (ref readonly int x in new E())
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "x").WithLocation(20, 35));
        }

        [Fact]
        [WorkItem(26585, "https://github.com/dotnet/roslyn/issues/26585")]
        public void ForEachIteratorWithCurrentRefKind_Iterator_Ref()
        {
            CreateCompilation(@"
using System.Collections.Generic;

class E
{
    public class Enumerator
    {
        public ref int Current => throw null;
        public bool MoveNext() => throw null;
    }

    public Enumerator GetEnumerator() => new Enumerator();
}
class C
{
    public static IEnumerable<int> Test()
    {
        foreach (ref int x in new E())
        {
            yield return x;
        }
    }
}").VerifyDiagnostics(
                // (18,26): error CS8176: Iterators cannot have by-reference locals
                //         foreach (ref int x in new E())
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "x").WithLocation(18, 26));
        }

        [Fact]
        [WorkItem(26585, "https://github.com/dotnet/roslyn/issues/26585")]
        public void ForEachIteratorWithCurrentRefKind_Iterator_RefReadonly()
        {
            CreateCompilation(@"
using System.Collections.Generic;

class E
{
    public class Enumerator
    {
        public ref readonly int Current => throw null;
        public bool MoveNext() => throw null;
    }

    public Enumerator GetEnumerator() => new Enumerator();
}
class C
{
    public static IEnumerable<int> Test()
    {
        foreach (ref readonly int x in new E())
        {
            yield return x;
        }
    }
}").VerifyDiagnostics(
                // (18,35): error CS8176: Iterators cannot have by-reference locals
                //         foreach (ref readonly int x in new E())
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "x").WithLocation(18, 35));
        }
    }
}
