// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

// Binding tests for the mixed object/collection initializer feature (dotnet/csharplang#10185).
// A single `{ ... }` initializer following `new T(...)` may contain both member-shaped initializer
// elements (`Name = value`, `Name op= value`, `[args] = value`) and bare-expression element
// initializers (`Add` targets). Feature gated to `LanguageVersion.Preview`.
public sealed class MixedInitializerBindingTests : CSharpTestBase
{
    #region Happy path: well-formed mixed lists

    [Fact]
    public void Mixed_MembersThenElements_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public int Y { get; set; }
                public List<int> Items { get; } = new();
                public void Add(int item) => Items.Add(item);
                public IEnumerator<int> GetEnumerator() => Items.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, Y = 2, 10, 20, 30 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_ElementsThenMembers_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { 10, 20, X = 1 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_Interleaved_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public int Y { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { 1, X = 2, 3, Y = 4, 5 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_IndexerMemberAndElements_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public string this[int i] { get => null; set { } }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { [0] = "a", 1, [1] = "b", 2 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_CompoundMemberAndElements_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 10, X += 5, 1, 2, 3 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_EventMemberAndElements_Binds()
    {
        var source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public event EventHandler E;
                public int X { get; set; }
                public void Add(int item) { }
                public void Raise() => E?.Invoke(this, EventArgs.Empty);
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void OnE(object o, EventArgs e) { }
                static void Main()
                {
                    var c = new C { X = 1, E += OnE, 10, 20 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_BraceListElement_Binds()
    {
        // The brace-list `{ a, b }` element initializer form is the multi-arg `Add(a, b)` shape.
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int a, int b) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, { 2, 3 }, { 4, 5 } };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_ExtensionAdd_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            static class Ext
            {
                public static void Add(this C c, int item) { }
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, 10, 20 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_TargetTypedNew_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    C c = new() { X = 1, 10, 20 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_NewWithConstructorArgs_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public C(string name) { Name = name; }
                public string Name { get; }
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C("alpha") { X = 1, 10, 20 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Type requirements: IEnumerable and Add

    [Fact]
    public void Mixed_TypeWithoutIEnumerable_ReportsIEnumerableRequired()
    {
        var source = """
            class C
            {
                public int X { get; set; }
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, 10 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (10,23): error CS1922: Cannot initialize type 'C' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
            //         var c = new C { X = 1, 10 };
            Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ X = 1, 10 }").WithArguments("C").WithLocation(10, 23));
    }

    [Fact]
    public void Mixed_TypeWithoutAdd_ReportsNoApplicableAdd()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, 10 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (15,32): error CS1061: 'C' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
            //         var c = new C { X = 1, 10 };
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "10").WithArguments("C", "Add").WithLocation(15, 32));
    }

    [Fact]
    public void PureMembers_NoIEnumerableRequired_Binds()
    {
        // The mixed-init feature gate must not fire on a pure-member object initializer, even when
        // the target type does not implement IEnumerable.
        var source = """
            class C
            {
                public int X { get; set; }
                public int Y { get; set; }
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, Y = 2 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Required members

    [Fact]
    public void Mixed_RequiredMember_NotDischargedByElementInitializer()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public required int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { 1, 2, 3 };
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics(
            // (16,21): error CS9035: Required member 'C.X' must be set in the object initializer or attribute constructor.
            //         var c = new C { 1, 2, 3 };
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "C").WithArguments("C.X").WithLocation(16, 21));
    }

    [Fact]
    public void Mixed_RequiredMember_DischargedByEqualsMemberInitializer()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public required int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, 2, 3 };
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics();
    }

    #endregion

    #region Language version gating

    [Fact]
    public void Mixed_PreFeatureLanguageVersion_FallsBackToInvalidInitializerElement()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, 10 };
                }
            }
            """;
        // At the previous shipped language version, the pre-feature behavior is preserved: the
        // bare element `10` is rejected by the object-initializer member-binding path with
        // CS0747, exactly as it was before the feature.
        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (16,32): error CS0747: Invalid initializer member declarator
            //         var c = new C { X = 1, 10 };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "10").WithLocation(16, 32));
    }

    [Fact]
    public void Mixed_PreFeatureLanguageVersion_FallsBackToInvalidInitializerElement_ElementFirst()
    {
        // Counterpart to the test above with the element ordered before the member: the wrapper
        // still classifies as `ObjectInitializerExpression` (because of the trailing `X = 1`), so
        // the pre-feature rejection still fires on the bare `10` regardless of its position in
        // the list.
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { 10, X = 1 };
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (16,25): error CS0747: Invalid initializer member declarator
            //         var c = new C { 10, X = 1 };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "10").WithLocation(16, 25));
    }

    #endregion

    #region `with` expressions remain out of scope

    [Fact]
    public void With_Mixed_RemainsRejected()
    {
        // The mixed object/collection initializer feature does not extend to `with` expressions:
        // a bare element inside a `with { ... }` body continues to be rejected by the
        // member-initializer binding path.
        var source = """
            record R(int X)
            {
                public static R Make(R r) => r with { X = 1, 10 };
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics(
            // (3,50): error CS0747: Invalid initializer member declarator
            //     public static R Make(R r) => r with { X = 1, 10 };
            Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "10").WithLocation(3, 50));
    }

    #endregion

    #region Expression trees: rejected (out of scope)

    [Fact]
    public void Mixed_InExpressionTree_Rejected()
    {
        // The Expression API's `MemberInit` shape can carry only member bindings; it has no
        // node for "Add this value to the collection alongside the member assignments". A mixed
        // initializer inside `Expression<Func<...>>` therefore reports
        // `ERR_ExpressionTreeContainsMixedObjectAndCollectionInitializer` per element.
        var source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Linq.Expressions;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static Expression<Func<C>> E1 = () => new C { X = 1, 10 };
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (16,58): error CS9379: An expression tree may not contain a mixed object and collection initializer.
            //     static Expression<Func<C>> E1 = () => new C { X = 1, 10 };
            Diagnostic(ErrorCode.ERR_ExpressionTreeContainsMixedObjectAndCollectionInitializer, "10").WithLocation(16, 58));
    }

    #endregion

    #region Init-only members

    [Fact]
    public void Mixed_InitOnlyMember_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; init; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, 10, 20 };
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net100).VerifyDiagnostics();
    }

    #endregion

    #region `??=` member initializer + elements

    [Fact]
    public void Mixed_NullCoalescingMemberAndElements_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public string Name { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { Name = "alpha", Name ??= "beta", 1, 2, 3 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Dynamic

    [Fact]
    public void Mixed_DynamicAddArgument_Binds()
    {
        // The bare argument expression is dispatched to the dynamic `Add` resolution path. The
        // expression is not a bare `IdentifierName` (which would route through the existing
        // member-initializer recovery), so the mixed-init dispatch routes it to the element
        // path as intended.
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void M(dynamic d)
                {
                    var c = new C { X = 1, d + 0 };
                }
            }
            """;
        CreateCompilation(source, references: new[] { CSharpRef }).VerifyDiagnostics();
    }

    #endregion

    #region Dictionary-style indexer + bare elements

    [Fact]
    public void Mixed_DictionaryIndexerAndBareElements_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                private readonly Dictionary<string, string> _map = new();
                public string this[string key] { get => _map[key]; set => _map[key] = value; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { ["a"] = "x", 1, ["b"] = "y", 2 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Generic Add overloads

    [Fact]
    public void Mixed_GenericAddOverload_Binds()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add<T>(T item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, "hello", 42, 3.14 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics();
    }

    #endregion

    #region Duplicate member rules continue to apply

    [Fact]
    public void Mixed_DuplicateEqualsMember_StillReported()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { X = 1, 10, X = 2 };
                }
            }
            """;
        CreateCompilation(source).VerifyDiagnostics(
            // (16,36): error CS1912: Duplicate initialization of member 'X'
            //         var c = new C { X = 1, 10, X = 2 };
            Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "X").WithArguments("X").WithLocation(16, 36));
    }

    #endregion
}
