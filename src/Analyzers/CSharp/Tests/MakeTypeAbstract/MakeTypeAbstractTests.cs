// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeTypeAbstract;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeTypeAbstract;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeTypeAbstract)]
public sealed class MakeTypeAbstractTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public MakeTypeAbstractTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpMakeTypeAbstractCodeFixProvider());

    [Fact]
    public Task TestMethod()
        => TestInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract void [|M|]();
            }
            """,
            """
            public abstract class Goo
            {
                public abstract void M();
            }
            """);

    [Fact]
    public Task TestMethodEnclosingClassWithoutAccessibility()
        => TestInRegularAndScriptAsync(
            """
            class Goo
            {
                public abstract void [|M|]();
            }
            """,
            """
            abstract class Goo
            {
                public abstract void M();
            }
            """);

    [Fact]
    public Task TestMethodEnclosingClassDocumentationComment()
        => TestInRegularAndScriptAsync(
            """
            /// <summary>
            /// Some class comment.
            /// </summary>
            public class Goo
            {
                public abstract void [|M|]();
            }
            """,
            """
            /// <summary>
            /// Some class comment.
            /// </summary>
            public abstract class Goo
            {
                public abstract void M();
            }
            """);

    [Fact]
    public Task TestPropertyGetter()
        => TestInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract object P { [|get|]; }
            }
            """,
            """
            public abstract class Goo
            {
                public abstract object P { get; }
            }
            """);

    [Fact]
    public Task TestPropertySetter()
        => TestInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract object P { [|set|]; }
            }
            """,
            """
            public abstract class Goo
            {
                public abstract object P { set; }
            }
            """);

    [Fact]
    public Task TestIndexerGetter()
        => TestInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract object this[object o] { [|get|]; }
            }
            """,
            """
            public abstract class Goo
            {
                public abstract object this[object o] { get; }
            }
            """);

    [Fact]
    public Task TestIndexerSetter()
        => TestInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract object this[object o] { [|set|]; }
            }
            """,
            """
            public abstract class Goo
            {
                public abstract object this[object o] { set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54218")]
    public Task TestPartialClass()
        => TestInRegularAndScriptAsync(
            """
            public partial class Goo
            {
                public abstract void [|M|]();
            }

            public partial class Goo
            {
            }
            """,
            """
            public abstract partial class Goo
            {
                public abstract void M();
            }

            public partial class Goo
            {
            }
            """);

    [Fact]
    public Task TestEventAdd()
        => TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract event System.EventHandler E { [|add|]; }
            }
            """);

    [Fact]
    public Task TestEventRemove()
        => TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract event System.EventHandler E { [|remove|]; }
            }
            """);

    [Fact]
    public Task TestMethodWithBody()
        => TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract int [|M|]() => 3;
            }
            """);

    [Fact]
    public Task TestPropertyGetterWithArrowBody()
        => TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract int [|P|] => 3;
            }
            """);

    [Fact]
    public Task TestPropertyGetterWithBody()
        => TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract int P
                {
                    [|get|] { return 1; }
                }
            }
            """);

    [Fact]
    public Task TestStructNestedInClass()
        => TestMissingInRegularAndScriptAsync(
            """
            public class C
            {
                public struct S
                {
                    public abstract void [|Goo|]();
                }
            }
            """);

    [Fact]
    public Task TestMethodEnclosingClassStatic()
        => TestMissingInRegularAndScriptAsync(
            """
            public static class Goo
            {
                public abstract void [|M|]();
            }
            """);

    [Fact]
    public Task TestRecord()
        => TestInRegularAndScriptAsync(
            """
            public record Goo
            {
                public abstract void [|M|]();
            }
            """,
            """
            public abstract record Goo
            {
                public abstract void M();
            }
            """);

    [Fact]
    public Task TestRecordClass()
        => TestInRegularAndScriptAsync(
            """
            public record class Goo
            {
                public abstract void [|M|]();
            }
            """,
            """
            public abstract record class Goo
            {
                public abstract void M();
            }
            """);

    [Fact]
    public Task TestRecordStruct()
        => TestMissingInRegularAndScriptAsync("""
            public record struct Goo
            {
                public abstract void [|M|]();
            }
            """);

    [Fact]
    public Task FixAll()
        => TestInRegularAndScriptAsync(
            """
            namespace NS
            {
                using System;

                public class C1
                {
                    public abstract void {|FixAllInDocument:|}M();
                    public abstract object P { get; set; }
                    public abstract object this[object o] { get; set; }
                }

                public class C2
                {
                    public abstract void M();
                }

                public class C3
                {
                    public class InnerClass
                    {
                        public abstract void M();
                    }
                }
            }
            """,
            """
            namespace NS
            {
                using System;

                public abstract class C1
                {
                    public abstract void M();
                    public abstract object P { get; set; }
                    public abstract object this[object o] { get; set; }
                }

                public abstract class C2
                {
                    public abstract void M();
                }

                public class C3
                {
                    public abstract class InnerClass
                    {
                        public abstract void M();
                    }
                }
            }
            """);
}
