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
public class MakeTypeAbstractTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public MakeTypeAbstractTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpMakeTypeAbstractCodeFixProvider());

    [Fact]
    public async Task TestMethod()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestMethodEnclosingClassWithoutAccessibility()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestMethodEnclosingClassDocumentationComment()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestPropertyGetter()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestPropertySetter()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestIndexerGetter()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestIndexerSetter()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54218")]
    public async Task TestPartialClass()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestEventAdd()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract event System.EventHandler E { [|add|]; }
            }
            """);
    }

    [Fact]
    public async Task TestEventRemove()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract event System.EventHandler E { [|remove|]; }
            }
            """);
    }

    [Fact]
    public async Task TestMethodWithBody()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract int [|M|]() => 3;
            }
            """);
    }

    [Fact]
    public async Task TestPropertyGetterWithArrowBody()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract int [|P|] => 3;
            }
            """);
    }

    [Fact]
    public async Task TestPropertyGetterWithBody()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            public class Goo
            {
                public abstract int P
                {
                    [|get|] { return 1; }
                }
            }
            """);
    }

    [Fact]
    public async Task TestStructNestedInClass()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            public class C
            {
                public struct S
                {
                    public abstract void [|Goo|]();
                }
            }
            """);
    }

    [Fact]
    public async Task TestMethodEnclosingClassStatic()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            public static class Goo
            {
                public abstract void [|M|]();
            }
            """);
    }

    [Fact]
    public async Task TestRecord()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestRecordClass()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestRecordStruct()
    {
        await TestMissingInRegularAndScriptAsync("""
            public record struct Goo
            {
                public abstract void [|M|]();
            }
            """);
    }

    [Fact]
    public async Task FixAll()
    {
        await TestInRegularAndScript1Async(
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
}
