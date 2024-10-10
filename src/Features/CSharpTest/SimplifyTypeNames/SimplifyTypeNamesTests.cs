// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyTypeNames;

[Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
public sealed partial class SimplifyTypeNamesTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpSimplifyTypeNamesDiagnosticAnalyzer(), new SimplifyTypeNamesCodeFixProvider());

    [Fact]
    public async Task SimplifyGenericName()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static T Goo<T>(T x, T y)
                {
                    return default(T);
                }

                static void M()
                {
                    var c = [|Goo<int>|](1, 1);
                }
            }
            """,
            """
            using System;

            class C
            {
                static T Goo<T>(T x, T y)
                {
                    return default(T);
                }

                static void M()
                {
                    var c = Goo(1, 1);
                }
            }
            """);
    }

    [Fact]
    public async Task UseAlias0()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            using Goo = System;

            namespace Root
            {
                class A
                {
                }

                class B
                {
                    public [|Goo::Int32|] a;
                }
            }
            """,
            """
            using Goo = System;

            namespace Root
            {
                class A
                {
                }

                class B
                {
                    public int a;
                }
            }
            """);
    }

    [Fact]
    public async Task UseAlias00()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace Root
            {
                using MyType = System.IO.File;

                class A
                {
                    [|System.IO.File|] c;
                }
            }
            """,
            """
            namespace Root
            {
                using MyType = System.IO.File;

                class A
                {
                    MyType c;
                }
            }
            """);
    }

    [Fact]
    public async Task UseGlobalAlias00()
    {
        await TestInRegularAndScriptAsync(
            """
            global using MyType = System.IO.File;

            namespace Root
            {
                class A
                {
                    [|System.IO.File|] c;
                }
            }
            """,
            """
            global using MyType = System.IO.File;

            namespace Root
            {
                class A
                {
                    MyType c;
                }
            }
            """);
    }

    [Fact]
    public async Task UseGlobalAlias01()
    {
        await TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>
            global using MyType = System.IO.File;
                    </Document>
                    <Document>
            namespace Root
            {
                class A
                {
                    [|System.IO.File|] c;
                }
            }</Document>
                </Project>
            </Workspace>
            """,
            """

            namespace Root
            {
                class A
                {
                    MyType c;
                }
            }
            """);
    }

    [Fact]
    public async Task UseAlias00_FileScopedNamespace()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace Root;

            using MyType = System.IO.File;

            class A
            {
                [|System.IO.File|] c;
            }
            """,
            """
            namespace Root;

            using MyType = System.IO.File;

            class A
            {
                MyType c;
            }
            """);
    }

    [Fact]
    public async Task UseAlias()
    {
        var source =
            """
            using MyType = System.Exception;

            class A
            {
                [|System.Exception|] c;
            }
            """;

        await TestInRegularAndScriptAsync(source,
            """
            using MyType = System.Exception;

            class A
            {
                MyType c;
            }
            """);

        await TestActionCountAsync(source, 1);
        await TestSpansAsync(
            """
            using MyType = System.Exception;

            class A
            {
                [|System.Exception|] c;
            }
            """);
    }

    [Fact]
    public async Task UseAlias1()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace Root
            {
                using MyType = System.Exception;

                class A
                {
                    [|System.Exception|] c;
                }
            }
            """,
            """
            namespace Root
            {
                using MyType = System.Exception;

                class A
                {
                    MyType c;
                }
            }
            """);
    }

    [Fact]
    public async Task UseAlias2()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = System.Exception;

            namespace Root
            {
                class A
                {
                    [|System.Exception|] c;
                }
            }
            """,
            """
            using MyType = System.Exception;

            namespace Root
            {
                class A
                {
                    MyType c;
                }
            }
            """);
    }

    [Fact]
    public async Task UseAlias3()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = System.Exception;

            namespace Root
            {
                namespace Nested
                {
                    class A
                    {
                        [|System.Exception|] c;
                    }
                }
            }
            """,
            """
            using MyType = System.Exception;

            namespace Root
            {
                namespace Nested
                {
                    class A
                    {
                        MyType c;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task UseAlias4()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = System.Exception;

            class A
            {
                [|System.Exception|] c;
            }
            """,
            """
            using MyType = System.Exception;

            class A
            {
                MyType c;
            }
            """);
    }

    [Fact]
    public async Task UseAlias5()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace Root
            {
                using MyType = System.Exception;

                class A
                {
                    [|System.Exception|] c;
                }
            }
            """,
            """
            namespace Root
            {
                using MyType = System.Exception;

                class A
                {
                    MyType c;
                }
            }
            """);
    }

    [Fact]
    public async Task UseAlias6()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = System.Exception;

            namespace Root
            {
                class A
                {
                    [|System.Exception|] c;
                }
            }
            """,
            """
            using MyType = System.Exception;

            namespace Root
            {
                class A
                {
                    MyType c;
                }
            }
            """);
    }

    [Fact]
    public async Task UseAlias7()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = System.Exception;

            namespace Root
            {
                namespace Nested
                {
                    class A
                    {
                        [|System.Exception|] c;
                    }
                }
            }
            """,
            """
            using MyType = System.Exception;

            namespace Root
            {
                namespace Nested
                {
                    class A
                    {
                        MyType c;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task UseAlias8()
    {
        await TestInRegularAndScriptAsync(
            """
            using Goo = System.Int32;

            namespace Root
            {
                namespace Nested
                {
                    class A
                    {
                        var c = [|System.Int32|].MaxValue;
                    }
                }
            }
            """,
            """
            using Goo = System.Int32;

            namespace Root
            {
                namespace Nested
                {
                    class A
                    {
                        var c = Goo.MaxValue;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21449")]
    public async Task DoNotChangeToAliasInNameOfIfItChangesNameOfName()
    {
        await TestInRegularAndScript1Async(
            """
            using System;
            using Foo = SimplifyInsideNameof.Program;

            namespace SimplifyInsideNameof
            {
              class Program
              {
                static void Main(string[] args)
                {
                  Console.WriteLine(nameof([|SimplifyInsideNameof.Program|]));
                }
              }
            }
            """,
            """
            using System;
            using Foo = SimplifyInsideNameof.Program;

            namespace SimplifyInsideNameof
            {
              class Program
              {
                static void Main(string[] args)
                {
                  Console.WriteLine(nameof(Program));
                }
              }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21449")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/40972")]
    public async Task DoChangeToAliasInNameOfIfItDoesNotAffectName1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using Goo = SimplifyInsideNameof.Program;

            namespace SimplifyInsideNameof
            {
              class Program
              {
                static void Main(string[] args)
                {
                  Console.WriteLine(nameof([|SimplifyInsideNameof.Program|].Main));
                }
              }
            }
            """,
            """
            using System;
            using Goo = SimplifyInsideNameof.Program;

            namespace SimplifyInsideNameof
            {
              class Program
              {
                static void Main(string[] args)
                {
                  Console.WriteLine(nameof(Main));
                }
              }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21449")]
    public async Task DoChangeToAliasInNameOfIfItDoesNotAffectName2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using Goo = N.Goo;

            namespace N {
                class Goo { }
            }

            namespace SimplifyInsideNameof
            {
              class Program
              {
                static void Main(string[] args)
                {
                  Console.WriteLine(nameof([|N.Goo|]));
                }
              }
            }
            """,
            """
            using System;
            using Goo = N.Goo;

            namespace N {
                class Goo { }
            }

            namespace SimplifyInsideNameof
            {
              class Program
              {
                static void Main(string[] args)
                {
                  Console.WriteLine(nameof(Goo));
                }
              }
            }
            """);
    }

    [Fact]
    public async Task TwoAliases()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType1 = System.Exception;

            namespace Root
            {
                using MyType2 = Exception;

                class A
                {
                    [|System.Exception|] c;
                }
            }
            """,
            """
            using MyType1 = System.Exception;

            namespace Root
            {
                using MyType2 = Exception;

                class A
                {
                    MyType1 c;
                }
            }
            """);
    }

    [Fact]
    public async Task TwoAliases2()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType1 = System.Exception;

            namespace Root
            {
                using MyType2 = [|System.Exception|];

                class A
                {
                    System.Exception c;
                }
            }
            """,
            """
            using MyType1 = System.Exception;

            namespace Root
            {
                using MyType2 = MyType1;

                class A
                {
                    System.Exception c;
                }
            }
            """);
    }

    [Fact]
    public async Task TwoAliasesConflict()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using MyType = System.Exception;

            namespace Root
            {
                using MyType = Exception;

                class A
                {
                    [|System.Exception|] c;
                }
            }
            """);
    }

    [Fact]
    public async Task DoNotChangeToAliasIfConflict1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using MyType = System.ConsoleColor;

            namespace Root
            {
                class A
                {
                    public int MyType => 3;

                    void M()
                    {
                        var x = [|System.ConsoleColor|].Red;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task DoNotChangeToAliasIfConflict2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using MyType = System.ConsoleColor;

            namespace Root
            {
                class A
                {
                    public System.ConsoleColor MyType() => 3;

                    void M()
                    {
                        var x = [|System.ConsoleColor|].Red;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task DoChangeToAliasIfTypesMatch1()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = System.ConsoleColor;

            namespace Root
            {
                class A
                {
                    public System.ConsoleColor MyType => 3;

                    void M()
                    {
                        var x = [|System.ConsoleColor|].Red;
                    }
                }
            }
            """,
            """
            using MyType = System.ConsoleColor;

            namespace Root
            {
                class A
                {
                    public System.ConsoleColor MyType => 3;

                    void M()
                    {
                        var x = MyType.Red;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task DoChangeToAliasIfTypesMatch2()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = System.ConsoleColor;

            namespace Root
            {
                class A
                {
                    public System.ConsoleColor MyType = 3;

                    void M()
                    {
                        var x = [|System.ConsoleColor|].Red;
                    }
                }
            }
            """,
            """
            using MyType = System.ConsoleColor;

            namespace Root
            {
                class A
                {
                    public System.ConsoleColor MyType = 3;

                    void M()
                    {
                        var x = MyType.Red;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task DoChangeToAliasIfTypesMatch3()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = System.ConsoleColor;

            namespace Root
            {
                class A
                {
                    void M(System.ConsoleColor MyType)
                    {
                        var x = [|System.ConsoleColor|].Red;
                    }
                }
            }
            """,
            """
            using MyType = System.ConsoleColor;

            namespace Root
            {
                class A
                {
                    void M(System.ConsoleColor MyType)
                    {
                        var x = MyType.Red;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task DoNotChangeToNamespaceAliasIfConflict()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = Root.Inner.Inner2;

            namespace Root
            {
                namespace Inner
                {
                    namespace Inner2
                    {
                        public class Red
                        {
                            public static void Goo()
                            {
                            }
                        }
                    }
                }

                class A
                {
                    public int MyType => 3;

                    void M()
                    {
                        [|Root.Inner.Inner2|].Red.Goo();
                    }
                }
            }
            """,
            """
            using MyType = Root.Inner.Inner2;

            namespace Root
            {
                namespace Inner
                {
                    namespace Inner2
                    {
                        public class Red
                        {
                            public static void Goo()
                            {
                            }
                        }
                    }
                }

                class A
                {
                    public int MyType => 3;

                    void M()
                    {
                        Inner.Inner2.Red.Goo();
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task DoChangeToNamespaceAliasIfNoConflict()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = Root.Inner.Inner2;

            namespace Root
            {
                namespace Inner
                {
                    namespace Inner2
                    {
                        public class Red
                        {
                            public static void Goo()
                            {
                            }
                        }
                    }
                }

                class A
                {
                    void M()
                    {
                        [|Root.Inner.Inner2|].Red.Goo();
                    }
                }
            }
            """,
            """
            using MyType = Root.Inner.Inner2;

            namespace Root
            {
                namespace Inner
                {
                    namespace Inner2
                    {
                        public class Red
                        {
                            public static void Goo()
                            {
                            }
                        }
                    }
                }

                class A
                {
                    void M()
                    {
                        MyType.Red.Goo();
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task DoChangeToAliasIfConflictIsntType()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = System.ConsoleColor;

            namespace Root
            {
                class A
                {
                    public int MyType => 3;

                    [|System.ConsoleColor|] x;
                }
            }
            """,
            """
            using MyType = System.ConsoleColor;

            namespace Root
            {
                class A
                {
                    public int MyType => 3;

                    MyType x;
                }
            }
            """);
    }

    [Fact]
    public async Task TwoMissingOnAmbiguousCref1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Example
            {
                /// <summary>
                /// <see cref="[|Example|].ToString"/>
                /// </summary>
                void Method()
                {
                }

                public override string ToString() => throw null;
                public string ToString(string format, IFormatProvider formatProvider) => throw null;
            }
            """);
    }

    [Fact]
    public async Task TwoMissingOnAmbiguousCref2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Example
            {
                /// <summary>
                /// <see cref="[|Example.ToString|]"/>
                /// </summary>
                void Method()
                {
                }

                public override string ToString() => throw null;
                public string ToString(string format, IFormatProvider formatProvider) => throw null;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40972")]
    public async Task TwoMissingInNameofMemberGroup()
    {
        await TestInRegularAndScript1Async(
            """
            class Example
            {
                void Method()
                {
                    _ = nameof([|Example|].Goo);
                }

                public static void Goo() { }
                public static void Goo(int i) { }
            }
            """,
            """
            class Example
            {
                void Method()
                {
                    _ = nameof(Goo);
                }

                public static void Goo() { }
                public static void Goo(int i) { }
            }
            """);
    }

    [Fact]
    public async Task TwoAliasesConflict2()
    {
        await TestInRegularAndScriptAsync(
            """
            using MyType = System.Exception;

            namespace Root
            {
                using MyType = [|System.Exception|];

                class A
                {
                    System.Exception c;
                }
            }
            """,
            """
            using MyType = System.Exception;

            namespace Root
            {
                using MyType = MyType;

                class A
                {
                    System.Exception c;
                }
            }
            """);
    }

    [Fact]
    public async Task AliasInSiblingNamespace()
    {
        var content =
            """
            [|namespace Root 
            {
                namespace Sibling
                {
                    using MyType = System.Exception;
                }

                class A 
                {
                    System.Exception c;
                }
            }|]
            """;
        await TestMissingInRegularAndScriptAsync(content);
    }

    [Fact]
    public async Task KeywordInt32()
    {
        var source =
            """
            class A
            {
                [|System.Int32|] i;
            }
            """;
        var featureOptions = PreferIntrinsicTypeEverywhere;
        await TestInRegularAndScriptAsync(source,
            """
            class A
            {
                int i;
            }
            """, options: featureOptions);
        await TestActionCountAsync(
            source, count: 1, parameters: new TestParameters(options: featureOptions));
        await TestSpansAsync(
            """
            class A
            {
                [|System.Int32|] i;
            }
            """, parameters: new TestParameters(options: featureOptions));
    }

    [Fact]
    public async Task Keywords()
    {
        var builtInTypeMap = new Dictionary<string, string>()
        {
            { "System.Boolean", "bool" },
            { "System.SByte", "sbyte" },
            { "System.Byte", "byte" },
            { "System.Decimal", "decimal" },
            { "System.Single", "float" },
            { "System.Double", "double" },
            { "System.Int16", "short" },
            { "System.Int32", "int" },
            { "System.Int64", "long" },
            { "System.Char", "char" },
            { "System.String", "string" },
            { "System.UInt16", "ushort" },
            { "System.UInt32", "uint" },
            { "System.UInt64", "ulong" }
        };

        var content =
            """
            class A
            {
                [|[||]|] i;
            }
            """;

        foreach (var pair in builtInTypeMap)
        {
            var newContent = content.Replace(@"[||]", pair.Key);
            var expected = content.Replace(@"[||]", pair.Value);
            await TestWithPredefinedTypeOptionsAsync(newContent, expected);
        }
    }

    [Fact]
    public async Task SimplifyTypeName()
    {
        var content =
            """
            namespace Root 
            {
                class A 
                {
                    [|System.Exception|] c;
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(content);
    }

    [Fact]
    public async Task SimplifyTypeName1()
    {
        var source =
            """
            using System;

            namespace Root
            {
                class A
                {
                    [|System.Exception|] c;
                }
            }
            """;

        await TestInRegularAndScriptAsync(source,
            """
            using System;

            namespace Root
            {
                class A
                {
                    Exception c;
                }
            }
            """);
        await TestActionCountAsync(source, 1);
        await TestSpansAsync(
            """
            using System;

            namespace Root
            {
                class A
                {
                    [|System|].Exception c;
                }
            }
            """);
    }

    [Fact]
    public async Task SimplifyTypeName1_FileScopedNamespace()
    {
        var source =
            """
            using System;

            namespace Root;

            class A
            {
                [|System.Exception|] c;
            }
            """;

        await TestInRegularAndScriptAsync(source,
            """
            using System;

            namespace Root;

            class A
            {
                Exception c;
            }
            """);
        await TestActionCountAsync(source, 1);
        await TestSpansAsync(
            """
            using System;

            namespace Root;

            class A
            {
                [|System|].Exception c;
            }
            """);
    }

    [Fact]
    public async Task SimplifyTypeName2()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace System
            {
                class A
                {
                    [|System.Exception|] c;
                }
            }
            """,
            """
            namespace System
            {
                class A
                {
                    Exception c;
                }
            }
            """);
    }

    [Fact]
    public async Task SimplifyTypeName3()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace N1
            {
                public class A1
                {
                }

                namespace N2
                {
                    public class A2
                    {
                        [|N1.A1|] a;
                    }
                }
            }
            """,
            """
            namespace N1
            {
                public class A1
                {
                }

                namespace N2
                {
                    public class A2
                    {
                        A1 a;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task SimplifyTypeName4()
    {
        // this is failing since we can't speculatively bind namespace yet
        await TestInRegularAndScriptAsync(
            """
            namespace N1
            {
                namespace N2
                {
                    public class A1
                    {
                    }
                }

                public class A2
                {
                    [|N1.N2.A1|] a;
                }
            }
            """,
            """
            namespace N1
            {
                namespace N2
                {
                    public class A1
                    {
                    }
                }

                public class A2
                {
                    N2.A1 a;
                }
            }
            """);
    }

    [Fact]
    public async Task SimplifyTypeName5()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace N1
            {
                class NC1
                {
                    public class A1
                    {
                    }
                }

                public class A2
                {
                    [|N1.NC1.A1|] a;
                }
            }
            """,
            """
            namespace N1
            {
                class NC1
                {
                    public class A1
                    {
                    }
                }

                public class A2
                {
                    NC1.A1 a;
                }
            }
            """);
    }

    [Fact]
    public async Task SimplifyTypeName6()
    {
        var content =
            """
            namespace N1
            {
                public class A1 { }

                namespace N2
                {
                    public class A1 { }

                    public class A2
                    {
                        [|N1.A1|] a;
                    }
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(content);
    }

    [Fact]
    public async Task SimplifyTypeName7()
    {
        var source =
            """
            namespace N1
            {
                namespace N2
                {
                    public class A2
                    {
                        public class A1 { }

                        [|N1.N2|].A2.A1 a;
                    }
                }
            }
            """;

        await TestInRegularAndScriptAsync(source,
            """
            namespace N1
            {
                namespace N2
                {
                    public class A2
                    {
                        public class A1 { }

                        A1 a;
                    }
                }
            }
            """);

        await TestActionCountAsync(source, 1);
    }

    [Fact]
    public async Task SimplifyGenericTypeName1()
    {
        var content =
            """
            namespace N1
            {
                public class A1
                {
                    [|System.EventHandler<System.EventArgs>|] a;
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(content);
    }

    [Fact]
    public async Task SimplifyGenericTypeName2()
    {
        var source =
            """
            using System;

            namespace N1
            {
                public class A1
                {
                    [|System.EventHandler<System.EventArgs>|] a;
                }
            }
            """;

        await TestInRegularAndScriptAsync(source,
            """
            using System;

            namespace N1
            {
                public class A1
                {
                    EventHandler<EventArgs> a;
                }
            }
            """);

        await TestActionCountAsync(source, 1);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9877")]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task SimplifyGenericTypeName3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            namespace N1
            {
                public class A1
                {
                    {|FixAllInDocument:System.Action|}<System.Action<System.Action<System.EventArgs>, System.Action<System.Action<System.EventArgs, System.Action<System.EventArgs>, System.Action<System.Action<System.Action<System.Action<System.EventArgs>, System.Action<System.EventArgs>>>>>>>> a;
                }
            }
            """,
            """
            using System;

            namespace N1
            {
                public class A1
                {
                    Action<Action<Action<EventArgs>, Action<Action<EventArgs, Action<EventArgs>, Action<Action<Action<Action<EventArgs>, Action<EventArgs>>>>>>>> a;
                }
            }
            """);
    }

    [Fact]
    public async Task SimplifyGenericTypeName4()
    {
        var content =
            """
            using MyHandler = System.EventHandler;

            namespace N1
            {
                public class A1
                {
                    [|System.EventHandler<System.EventHandler<System.EventArgs>>|] a;
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(content);
    }

    [Fact]
    public async Task SimplifyGenericTypeName5()
    {
        var source =
            """
            using MyHandler = System.EventHandler<System.EventArgs>;

            namespace N1
            {
                public class A1
                {
                    System.EventHandler<[|System.EventHandler<System.EventArgs>|]> a;
                }
            }
            """;

        await TestInRegularAndScriptAsync(source,
            """
            using MyHandler = System.EventHandler<System.EventArgs>;

            namespace N1
            {
                public class A1
                {
                    System.EventHandler<MyHandler> a;
                }
            }
            """);
        await TestActionCountAsync(source, 1);
        await TestSpansAsync(
            """
            using MyHandler = System.EventHandler<System.EventArgs>;

            namespace N1
            {
                public class A1
                {
                    System.EventHandler<[|System.EventHandler<System.EventArgs>|]> a;
                }
            }
            """);
    }

    [Fact]
    public async Task SimplifyGenericTypeName6()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            namespace N1
            {
                using MyType = N2.A1<Exception>;

                namespace N2
                {
                    public class A1<T>
                    {
                    }
                }

                class Test
                {
                    [|N1.N2.A1<System.Exception>|] a;
                }
            }
            """,
            """
            using System;

            namespace N1
            {
                using MyType = N2.A1<Exception>;

                namespace N2
                {
                    public class A1<T>
                    {
                    }
                }

                class Test
                {
                    MyType a;
                }
            }
            """);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9877")]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task SimplifyGenericTypeName7()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            namespace N1
            {
                using MyType = Exception;

                namespace N2
                {
                    public class A1<T>
                    {
                    }
                }

                class Test
                {
                    N1.N2.A1<[|System.Exception|]> a;
                }
            }
            """,
            """
            using System;

            namespace N1
            {
                using MyType = Exception;

                namespace N2
                {
                    public class A1<T>
                    {
                    }
                }

                class Test
                {
                    N2.A1<MyType> a;
                }
            }
            """);
    }

    [Fact]
    public async Task Array1()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            using System.Collections.Generic;

            namespace N1
            {
                class Test
                {
                    [|System.Collections.Generic.List<System.String[]>|] a;
                }
            }
            """,
            """
            using System.Collections.Generic;

            namespace N1
            {
                class Test
                {
                    List<string[]> a;
                }
            }
            """);

        // TODO: The below test is currently disabled due to restrictions of the test framework, this needs to be fixed.

        ////            Test(
        ////    @"using System.Collections.Generic;

        ////namespace N1
        ////{
        ////    class Test
        ////    {
        ////        System.Collections.Generic.List<[|System.String|][]> a;
        ////    }
        ////}", @"
        ////using System.Collections.Generic;

        ////namespace N1
        ////{
        ////    class Test
        ////    {
        ////        System.Collections.Generic.List<string[]> a;
        ////    }
        ////}");
    }

    [Fact]
    public async Task Array2()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            using System.Collections.Generic;

            namespace N1
            {
                class Test
                {
                    [|System.Collections.Generic.List<System.String[][,][,,,]>|] a;
                }
            }
            """,
            """
            using System.Collections.Generic;

            namespace N1
            {
                class Test
                {
                    List<string[][,][,,,]> a;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073099")]
    public async Task SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = nameof([|Int32|]);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168")]
    public async Task SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var x = nameof([|System.Int32|]);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073099")]
    public async Task SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = nameof([|Int32|].MaxValue);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073099")]
    public async Task SimplifyToPredefinedTypeNameShouldBeOfferedInsideFunctionCalledNameOf()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = nameof(typeof([|Int32|]));
                }

                static string nameof(Type t)
                {
                    return string.Empty;
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = nameof(typeof(int));
                }

                static string nameof(Type t)
                {
                    return string.Empty;
                }
            }
            """);
    }

    [Fact]
    public async Task SimplifyTypeNameInsideNameOf()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = nameof([|System.Int32|]);
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = nameof(Int32);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168")]
    public async Task SimplifyCrefAliasPredefinedType()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace N1
            {
                public class C1
                {
                    /// <see cref="[|System.Int32|]"/>
                    public C1()
                    {
                    }
                }
            }
            """,
            """
            namespace N1
            {
                public class C1
                {
                    /// <see cref="int"/>
                    public C1()
                    {
                    }
                }
            }
            """, options: PreferIntrinsicTypeEverywhere);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
    public async Task SimplifyAlias1()
    {
        var content =
            """
            using I64 = [|System.Int64|];

            namespace N1
            {
                class Test
                {
                }
            }
            """;

        await TestMissingInRegularAndScriptAsync(content);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
    public async Task SimplifyAlias2()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            using I64 = System.Int64;
            using Goo = System.Collections.Generic.IList<[|System.Int64|]>;

            namespace N1
            {
                class Test
                {
                }
            }
            """,
            """
            using I64 = System.Int64;
            using Goo = System.Collections.Generic.IList<long>;

            namespace N1
            {
                class Test
                {
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
    public async Task SimplifyAlias3()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            namespace Outer
            {
                using I64 = System.Int64;
                using Goo = System.Collections.Generic.IList<[|System.Int64|]>;

                namespace N1
                {
                    class Test
                    {
                    }
                }
            }
            """,
            """
            namespace Outer
            {
                using I64 = System.Int64;
                using Goo = System.Collections.Generic.IList<long>;

                namespace N1
                {
                    class Test
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538727")]
    public async Task SimplifyAlias4()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            using I64 = System.Int64;

            namespace Outer
            {
                using Goo = System.Collections.Generic.IList<[|System.Int64|]>;

                namespace N1
                {
                    class Test
                    {
                    }
                }
            }
            """,
            """
            using I64 = System.Int64;

            namespace Outer
            {
                using Goo = System.Collections.Generic.IList<long>;

                namespace N1
                {
                    class Test
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544631")]
    public async Task SimplifyAlias5()
    {
        var content =
            """
            using System;

            namespace N
            {
                using X = [|System.Nullable<int>|];
            }
            """;

        var result =
            """
            using System;

            namespace N
            {
                using X = Nullable<int>;
            }
            """;
        await TestInRegularAndScriptAsync(content, result);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/919815")]
    public async Task SimplifyReturnTypeOnMethodCallToAlias()
    {
        await TestInRegularAndScriptAsync(
            """
            using alias1 = A;

            class A
            {
                public [|A|] M()
                {
                    return null;
                }
            }
            """,
            """
            using alias1 = A;

            class A
            {
                public alias1 M()
                {
                    return null;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538949")]
    public async Task SimplifyComplexGeneric1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class A<T>
            {
                class B : A<B>
                {
                }

                class C : I<B>, I<[|B.B|]>
                {
                }
            }

            interface I<T>
            {
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538949")]
    public async Task SimplifyComplexGeneric2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class A<T>
            {
                class B : A<B>
                {
                }

                class C : I<B>, [|B.B|]
                {
                }
            }

            interface I<T>
            {
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538991")]
    public async Task SimplifyMissingOnGeneric()
    {
        var content =
            """
            class A<T, S>
            {
                class B : [|A<B, B>|] { }
            }
            """;

        await TestMissingInRegularAndScriptAsync(content);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539000")]
    public async Task SimplifyMissingOnUnmentionableTypeParameter1()
    {
        var content =
            """
            class A<T>
            {
                class D : A<T[]> { }
                class B { }

                class C<T>
                {
                    D.B x = new [|D.B|]();
                }
            }
            """;

        await TestMissingInRegularAndScriptAsync(content);
    }

    [Fact]
    public async Task SimplifyErrorTypeParameter()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using M = System.Collections.Generic.IList<[|System.Collections.Generic.IList<>|]>;

            class C
            {
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539000")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/838109")]
    public async Task SimplifyUnmentionableTypeParameter2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class A<T>
            {
                class D : A<T[]>
                {
                }

                class B
                {
                }

                class C<Y>
                {
                    D.B x = new [|D.B|]();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539000")]
    public async Task SimplifyUnmentionableTypeParameter2_1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class A<T>
            {
                class D : A<T[]>
                {
                }

                class B
                {
                }

                class C<T>
                {
                    D.B x = new [|D.B|]();
                }
            }
            """);
    }

    [Fact]
    public async Task TestGlobalAlias()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    [|global::System|].String s;
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    string s;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541748")]
    public async Task TestOnErrorInScript()
    {
        await TestMissingAsync(
@"[|Console.WrieLine();|]",
new TestParameters(Options.Script));
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9877")]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestConflicts()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace OuterNamespace
            {
                namespace InnerNamespace
                {
                    class InnerClass1
                    {
                    }
                }

                class OuterClass1
                {
                    OuterNamespace.OuterClass1 M1()
                    {
                        [|OuterNamespace.OuterClass1|] c1;
                        OuterNamespace.OuterClass1.Equals(1, 2);
                    }

                    OuterNamespace.OuterClass2 M2()
                    {
                        OuterNamespace.OuterClass2 c1;
                        OuterNamespace.OuterClass2.Equals(1, 2);
                    }

                    OuterNamespace.InnerNamespace.InnerClass1 M3()
                    {
                        OuterNamespace.InnerNamespace.InnerClass1 c1;
                        OuterNamespace.InnerNamespace.InnerClass1.Equals(1, 2);
                    }

                    InnerNamespace.InnerClass1 M3()
                    {
                        InnerNamespace.InnerClass1 c1;
                        global::OuterNamespace.InnerNamespace.InnerClass1.Equals(1, 2);
                    }

                    void OuterClass2()
                    {
                    }

                    void InnerClass1()
                    {
                    }

                    void InnerNamespace()
                    {
                    }
                }

                class OuterClass2
                {
                    OuterNamespace.OuterClass1 M1()
                    {
                        OuterNamespace.OuterClass1 c1;
                        OuterNamespace.OuterClass1.Equals(1, 2);
                    }

                    OuterNamespace.OuterClass2 M2()
                    {
                        OuterNamespace.OuterClass2 c1;
                        OuterNamespace.OuterClass2.Equals(1, 2);
                    }

                    OuterNamespace.InnerNamespace.InnerClass1 M3()
                    {
                        OuterNamespace.InnerNamespace.InnerClass1 c1;
                        OuterNamespace.InnerNamespace.InnerClass1.Equals(1, 2);
                    }

                    InnerNamespace.InnerClass1 M3()
                    {
                        InnerNamespace.InnerClass1 c1;
                        InnerNamespace.InnerClass1.Equals(1, 2);
                    }
                }
            }
            """,
            """
            namespace OuterNamespace
            {
                namespace InnerNamespace
                {
                    class InnerClass1
                    {
                    }
                }

                class OuterClass1
                {
                    OuterClass1 M1()
                    {
                        OuterClass1 c1;
                        Equals(1, 2);
                    }

                    OuterClass2 M2()
                    {
                        OuterClass2 c1;
                        Equals(1, 2);
                    }

                    InnerNamespace.InnerClass1 M3()
                    {
                        InnerNamespace.InnerClass1 c1;
                        Equals(1, 2);
                    }

                    InnerNamespace.InnerClass1 M3()
                    {
                        InnerNamespace.InnerClass1 c1;
                        Equals(1, 2);
                    }

                    void OuterClass2()
                    {
                    }

                    void InnerClass1()
                    {
                    }

                    void InnerNamespace()
                    {
                    }
                }

                class OuterClass2
                {
                    OuterClass1 M1()
                    {
                        OuterClass1 c1;
                        Equals(1, 2);
                    }

                    OuterClass2 M2()
                    {
                        OuterClass2 c1;
                        Equals(1, 2);
                    }

                    InnerNamespace.InnerClass1 M3()
                    {
                        InnerNamespace.InnerClass1 c1;
                        Equals(1, 2);
                    }

                    InnerNamespace.InnerClass1 M3()
                    {
                        InnerNamespace.InnerClass1 c1;
                        Equals(1, 2);
                    }
                }
            }
            """,
index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40633")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542100")]
    public async Task TestPreventSimplificationToNameInCurrentScope()
    {
        await TestInRegularAndScript1Async(
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main()
                    {
                        [|N.Program.Goo.Bar|]();
                        int Goo;
                    }
                }
            }
            """,

            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main()
                    {
                        Program.Goo.Bar();
                        int Goo;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40633")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542100")]
    public async Task TestPreventSimplificationToNameInCurrentScope2()
    {
        await TestInRegularAndScript1Async(
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main(int Goo)
                    {
                        [|N.Program.Goo.Bar|]();
                    }
                }
            }
            """,

            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main(int Goo)
                    {
                        Program.Goo.Bar();
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40633")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542100")]
    public async Task TestAllowSimplificationToNameInNestedScope()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main()
                    {
                        [|N.Program.Goo.Bar|]();
                        {
                            int Goo;
                        }
                    }
                }
            }
            """,
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main()
                    {
                        Goo.Bar();
                        {
                            int Goo;
                        }
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40633")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542100")]
    public async Task TestAllowSimplificationToNameInNestedScope1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Linq;

            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main(int[] args)
                    {
                        [|N.Program.Goo.Bar|]();
                        var q = from Goo in args select Goo;
                    }
                }
            }
            """,
            """
            using System.Linq;

            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main(int[] args)
                    {
                        Goo.Bar();
                        var q = from Goo in args select Goo;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnOpenType1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program<T>
            {
                public class Inner
                {
                    [Bar(typeof([|Program<>.Inner|]))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnOpenType2()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                public class Inner<T>
                {
                    [Bar(typeof([|Program.Inner<>|]))]
                    void Goo()
                    {
                    }
                }
            }
            """,
            """
            class Program
            {
                public class Inner<T>
                {
                    [Bar(typeof(Inner<>))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnOpenType3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program<X>
            {
                public class Inner<Y>
                {
                    [Bar(typeof([|Program<>.Inner<>|]))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnOpenType4()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program<X>
            {
                public class Inner<Y>
                {
                    [Bar(typeof([|Program<X>.Inner<>|]))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnOpenType5()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program<X>
            {
                public class Inner<Y>
                {
                    [Bar(typeof([|Program<>.Inner<Y>|]))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnOpenType6()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program<X>
            {
                public class Inner<Y>
                {
                    [Bar(typeof([|Program<Y>.Inner<X>|]))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnNonOpenType1()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                public class Inner
                {
                    [Bar(typeof([|Program.Inner|]))]
                    void Goo()
                    {
                    }
                }
            }
            """,
            """
            class Program
            {
                public class Inner
                {
                    [Bar(typeof(Inner))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnNonOpenType2()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program<T>
            {
                public class Inner
                {
                    [Bar(typeof([|Program<T>.Inner|]))]
                    void Goo()
                    {
                    }
                }
            }
            """,
            """
            class Program<T>
            {
                public class Inner
                {
                    [Bar(typeof(Inner))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnNonOpenType3()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                public class Inner<T>
                {
                    [Bar(typeof([|Program.Inner<>|]))]
                    void Goo()
                    {
                    }
                }
            }
            """,
            """
            class Program
            {
                public class Inner<T>
                {
                    [Bar(typeof(Inner<>))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnNonOpenType4()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program<X>
            {
                public class Inner<Y>
                {
                    [Bar(typeof([|Program<X>.Inner<Y>|]))]
                    void Goo()
                    {
                    }
                }
            }
            """,
            """
            class Program<X>
            {
                public class Inner<Y>
                {
                    [Bar(typeof(Inner<Y>))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnNonOpenType5()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program<X>
            {
                public class Inner<Y>
                {
                    [Bar(typeof([|Program<X>.Inner<X>|]))]
                    void Goo()
                    {
                    }
                }
            }
            """,
            """
            class Program<X>
            {
                public class Inner<Y>
                {
                    [Bar(typeof(Inner<X>))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541929")]
    public async Task TestOnNonOpenType6()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program<X>
            {
                public class Inner<Y>
                {
                    [Bar(typeof([|Program<Y>.Inner<Y>|]))]
                    void Goo()
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542650")]
    public async Task TestWithInterleavedDirective1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            #if true
            class A
            #else
            class B
            #endif
            {
                class C
                {
                }

                static void Main()
                {
            #if true
                    [|A.
            #else
                    B.
            #endif
                        C|] x;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542719")]
    public async Task TestGlobalMissing1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                class System
                {
                }

                int Console = 7;

                void Main()
                {
                    string v = null;
                    [|global::System.Console.WriteLine(v)|];
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544615")]
    public async Task TestMissingOnAmbiguousCast()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            enum E
            {
            }

            class C
            {
                void Main()
                {
                    var x = ([|global::E|])-1;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544616")]
    public async Task ParenthesizeIfParseChanges()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                void M()
                {
                    object x = 1;
                    var y = [|x as System.Nullable<int>|] + 1;
                }
            }
            """,
            """
            using System;
            class C
            {
                void M()
                {
                    object x = 1;
                    var y = (x as int?) + 1;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544974")]
    public async Task TestNullableSimplification1()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                static void Main()
                {
                    [|System.Nullable<int>.Equals|](1, 1);
                }
            }
            """,
            """
            class C
            {
                static void Main()
                {
                    Equals(1, 1);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544974")]
    public async Task TestNullableSimplification3()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                static void Main([|System.Nullable<int>|] i)
                {
                }
            }
            """,
            """
            class C
            {
                static void Main(int? i)
                {
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544974")]
    public async Task TestNullableSimplification4()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            class C
            {
                static void Main([|System.Nullable<System.Int32>|] i)
                {
                }
            }
            """,
            """
            class C
            {
                static void Main(int? i)
                {
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544977")]
    public async Task TestNullableSimplification5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    var x = [|1 is System.Nullable<int>|]? 2 : 3;
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    var x = 1 is int? ? 2 : 3;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestMissingNullableSimplificationInsideCref()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            /// <summary>
            /// <see cref="[|Nullable{T}|]"/>
            /// </summary>
            class A
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestMissingNullableSimplificationInsideCref2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            /// <summary>
            /// <see cref="[|System.Nullable{T}|]"/>
            /// </summary>
            class A
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestMissingNullableSimplificationInsideCref3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            /// <summary>
            /// <see cref="[|System.Nullable{T}|].Value"/>
            /// </summary>
            class A
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestNullableInsideCref_AllowedIfReferencingActualTypeParameter()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            /// <summary>
            /// <see cref="C{[|Nullable{T}|]}"/>
            /// </summary>
            class C<T>
            {
            }
            """,
            """
            using System;
            /// <summary>
            /// <see cref="C{T?}"/>
            /// </summary>
            class C<T>
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestMissingNullableSimplificationInsideCref5()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            /// <summary>
            /// <see cref="A.M{[|Nullable{T}|]}()"/>
            /// </summary>
            class A
            {
                public void M<U>() where U : struct
                {
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/40664")]
    public async Task TestNullableInsideCref_NotAllowedAtTopLevel()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            /// <summary>
            /// <see cref="[|Nullable{int}|]"/>
            /// </summary>
            class A
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/40664")]
    public async Task TestNullableInsideCref_TopLevel2()
    {
        await TestInRegularAndScript1Async(
            """
            using System;
            /// <summary>
            /// <see cref="[|System.Nullable{int}|]"/>
            /// </summary>
            class A
            {
            }
            """,
            """
            using System;
            /// <summary>
            /// <see cref="Nullable{int}"/>
            /// </summary>
            class A
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestNullableInsideCref_AllowedIfReferencingActualType_AsTypeArgument()
    {
        // Both the 'original' and 'fixed' code here are incorrect as doc comments do not allow
        // actual type-references in a type-arg list.
        await TestInRegularAndScriptAsync(
            """
            using System;
            /// <summary>
            /// <see cref="C{[|Nullable{int}|]}"/>
            /// </summary>
            class C<T>
            {
            }
            """,
            """
            using System;
            /// <summary>
            /// <see cref="C{int?}"/>
            /// </summary>
            class C<T>
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestNullableInsideCref_AllowedIfReferencingActualType_InParameterList()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            /// <summary>
            /// <see cref="Goo([|Nullable{int}|])"/>
            /// </summary>
            class C
            {
                void Goo(int? i) { }
            }
            """,
            """
            using System;
            /// <summary>
            /// <see cref="Goo(int?)"/>
            /// </summary>
            class C
            {
                void Goo(int? i) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestMissingNullableSimplificationInsideCref8()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            /// <summary>
            /// <see cref="A.M{[|Nullable{int}|]}()"/>
            /// </summary>
            class A
            {
                public void M<U>() where U : struct
                {
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestNullableSimplificationInsideCref_Indirect()
    {
        await TestInRegularAndScriptAsync(
            """
            /// <summary>
            /// <see cref="A.M([|System.Nullable{A}|])"/>
            /// </summary>
            struct A
            {
                public void M(A? x)
                {
                }
            }
            """,
            """
            /// <summary>
            /// <see cref="A.M(A?)"/>
            /// </summary>
            struct A
            {
                public void M(A? x)
                {
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestNullableSimplificationInsideCref_Direct()
    {
        await TestInRegularAndScriptAsync(
            """
            /// <summary>
            /// <see cref="M([|System.Nullable{A}|])"/>
            /// </summary>
            struct A
            {
                public void M(A? x)
                {
                }
            }
            """,
            """
            /// <summary>
            /// <see cref="M(A?)"/>
            /// </summary>
            struct A
            {
                public void M(A? x)
                {
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestNullableSimplificationInsideCref2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            /// <summary>
            /// <see cref="A.M(List{[|Nullable{int}|]})"/>
            /// </summary>
            class A
            {
                public void M(List<int?> x)
                {
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            /// <summary>
            /// <see cref="A.M(List{int?})"/>
            /// </summary>
            class A
            {
                public void M(List<int?> x)
                {
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestNullableSimplificationInsideCref3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            /// <summary>
            /// <see cref="A.M{U}(List{[|Nullable{U}|]})"/>
            /// </summary>
            class A
            {
                public void M<U>(List<U?> x) where U : struct
                {
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            /// <summary>
            /// <see cref="A.M{U}(List{U?})"/>
            /// </summary>
            class A
            {
                public void M<U>(List<U?> x) where U : struct
                {
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29")]
    public async Task TestNullableSimplificationInsideCref4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            /// <summary>
            /// <see cref="A.M{T}(List{Nullable{T}}, [|Nullable{T}|])"/>
            /// </summary>
            class A
            {
                public void M<U>(List<U?> x, U? y) where U : struct
                {
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            /// <summary>
            /// <see cref="A.M{T}(List{Nullable{T}}, T?)"/>
            /// </summary>
            class A
            {
                public void M<U>(List<U?> x, U? y) where U : struct
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestColorColorCase1()
    {
        await TestInRegularAndScriptAsync(
            """
            using N;

            namespace N
            {
                class Color
                {
                    public static void Goo()
                    {
                    }

                    public void Bar()
                    {
                    }
                }
            }

            class Program
            {
                Color Color;

                void Main()
                {
                    [|N.Color|].Goo();
                }
            }
            """,
            """
            using N;

            namespace N
            {
                class Color
                {
                    public static void Goo()
                    {
                    }

                    public void Bar()
                    {
                    }
                }
            }

            class Program
            {
                Color Color;

                void Main()
                {
                    Color.Goo();
                }
            }
            """);
    }

    [Fact]
    public async Task TestColorColorCase2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using N;

            namespace N
            {
                class Color
                {
                    public static void Goo()
                    {
                    }

                    public void Bar()
                    {
                    }
                }
            }

            class Program
            {
                Color Color;

                void Main()
                {
                    [|Color.Goo|]();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40632")]
    public async Task TestColorColorCase3()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace N
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }

                /// <summary>
                /// <see cref="[|N|].Goo.Bar"/>
                /// </summary>
                class Program
                {
                    public Goo Goo;
                }
            }
            """,
            """
            namespace N
            {
                class Goo
                {
                    public static void Bar()
                    {
                    }
                }

                /// <summary>
                /// <see cref="Goo.Bar"/>
                /// </summary>
                class Program
                {
                    public Goo Goo;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40632")]
    public async Task TestColorColorCase4()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace N
            {
                class Goo
                {
                    public class Bar
                    {
                        public class Baz { }
                    }
                }

                /// <summary>
                /// <see cref="[|N|].Goo.Bar.Baz"/>
                /// </summary>
                class Program
                {
                    public Goo Goo;
                }
            }
            """,
            """
            namespace N
            {
                class Goo
                {
                    public class Bar
                    {
                        public class Baz { }
                    }
                }

                /// <summary>
                /// <see cref="Goo.Bar.Baz"/>
                /// </summary>
                class Program
                {
                    public Goo Goo;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40632")]
    public async Task TestColorColorCase5()
    {
        await TestMissingAsync(
            """
            using A;

            namespace A
            {
                public struct Goo { }
            }

            namespace N
            {
                /// <summary><see cref="[|A|].Goo"/></summary
                class Color
                {
                    public Goo Goo;
                }
            }
            """);
    }

    [Fact]
    public async Task TestColorColorCase6()
    {
        await TestMissingAsync(
            """
            using System.Reflection.Metadata;
            using Microsoft.Cci;

            namespace System.Reflection.Metadata
            {
                public enum PrimitiveTypeCode
                {
                    Void = 1,
                }
            }

            namespace Microsoft.Cci
            {
                internal enum PrimitiveTypeCode
                {
                    NotPrimitive,
                    Pointer,
                }
            }

            namespace Microsoft.CodeAnalysis.CSharp.Symbols
            {
                internal class TypeSymbol
                {
                    internal Cci.PrimitiveTypeCode PrimitiveTypeCode => Cci.PrimitiveTypeCode.Pointer;
                }

                internal partial class NamedTypeSymbol : TypeSymbol
                {
                    Cci.PrimitiveTypeCode TypeCode
                        => [|Cci|].PrimitiveTypeCode.NotPrimitive;
                }
            }
            """);
    }

    [Fact]
    public async Task TestAliasQualifiedType()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    [|global::Program|] a = null; 
                }
            }
            """;
        await TestAsync(source,
            """
            class Program
            {
                static void Main()
                {
                    Program a = null; 
                }
            }
            """, parseOptions: null);

        await TestMissingAsync(source, new TestParameters(GetScriptOptions()));
    }

    [Fact]
    public async Task TestSimplifyExpression()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    int x = [|System.Console.Read|]() + System.Console.Read();
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    int x = Console.Read() + System.Console.Read();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551040")]
    public async Task TestSimplifyStaticMemberAccess()
    {
        var source =
            """
            class Preserve
            {
            	public static int Y;
            }

            class Z<T> : Preserve
            {
            }

            static class M
            {
            	public static void Main()
            	{
            		int k = [|Z<float>.Y|];
            	}
            }
            """;
        await TestInRegularAndScriptAsync(source,
            """
            class Preserve
            {
            	public static int Y;
            }

            class Z<T> : Preserve
            {
            }

            static class M
            {
            	public static void Main()
            	{
            		int k = Preserve.Y;
            	}
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551040")]
    public async Task TestSimplifyNestedType()
    {
        var source =
            """
            class Preserve
            {
            	public class X
            	{
            		public static int Y;
            	}
            }

            class Z<T> : Preserve
            {
            }

            class M
            {
            	public static void Main()
            	{
            		int k = [|Z<float>.X|].Y;
            	}
            }
            """;
        await TestInRegularAndScriptAsync(source,
            """
            class Preserve
            {
            	public class X
            	{
            		public static int Y;
            	}
            }

            class Z<T> : Preserve
            {
            }

            class M
            {
            	public static void Main()
            	{
            		int k = Preserve.X.Y;
            	}
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568043")]
    public async Task DoNotSimplifyNamesWhenThereAreParseErrors()
    {
        var markup =
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.[||]
                }
            }
            """;

        await TestMissingInRegularAndScriptAsync(markup);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566749")]
    public async Task TestMethodGroups1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action a = [|Console.WriteLine|];
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566749")]
    public async Task TestMethodGroups2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action a = [|Console.Blah|];
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554010")]
    public async Task TestMethodGroups3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action a = [|System.Console.WriteLine|];
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action a = Console.WriteLine;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578686")]
    public async Task FixAllOccurrences1()
    {
        await TestInRegularAndScriptAsync(
            """
            using goo = A.B;
            using bar = C.D;

            class Program
            {
                static void Main(string[] args)
                {
                    var s = [|new C.D().prop|];
                }
            }

            namespace A
            {
                class B
                {
                }
            }

            namespace C
            {
                class D
                {
                    public A.B prop { get; set; }
                }
            }
            """,
            """
            using goo = A.B;
            using bar = C.D;

            class Program
            {
                static void Main(string[] args)
                {
                    var s = new bar().prop;
                }
            }

            namespace A
            {
                class B
                {
                }
            }

            namespace C
            {
                class D
                {
                    public A.B prop { get; set; }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578686")]
    public async Task DoNotUseAlias1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            namespace NSA
            {
                class DuplicateClassName
                {
                }
            }

            namespace NSB
            {
                class DuplicateClassName
                {
                }
            }

            namespace Test
            {
                using AliasA = NSA.DuplicateClassName;
                using AliasB = NSB.DuplicateClassName;

                class TestClass
                {
                    static void Main(string[] args)
                    {
                        var localA = new NSA.DuplicateClassName();
                        var localB = new NSB.DuplicateClassName();
                        new List<NoAlias.Goo>().Where(m => [|m.InnocentProperty|] == null);
                    }
                }
            }

            namespace NoAlias
            {
                class Goo
                {
                    public NSB.DuplicateClassName InnocentProperty { get; set; }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577169")]
    public async Task SuitablyReplaceNullables1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var w = new [|Nullable<>|].
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577169")]
    public async Task SuitablyReplaceNullables2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = typeof([|Nullable<>|]);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608190")]
    public async Task Bugfix_608190()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                }
            }

            struct S
            {
                int x;

                S(dynamic y)
                {
                    [|S.Equals|](y, 0);
                    x = y;
                }

                static bool Equals(S s, object y) => false;
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608190")]
    public async Task Bugfix_608190_1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                }
            }

            struct S
            {
                int x;

                S(dynamic y)
                {
                    x = y;
                    [|this.Equals|](y, 0);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608932")]
    public async Task Bugfix_608932()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using S = X;

            class Program
            {
                static void Main(string[] args)
                {
                }
            }

            namespace X
            {
                using S = System;

                enum E
                {
                }

                class C<E>
                {
                    [|X|].E e; // Simplify type name as suggested
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635933")]
    public async Task Bugfix_635933()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class B
            {
                public static void Goo(int x, object y) {}

                public static void Goo(long x, object y) {}
            
                static void Main()
                {
                    C<string>.D.Goo(0);
                }
            }

            class C<T> : B
            {
                public class D : C<T> // Start rename session and try to rename D to T
                {
                    public static void Goo(dynamic x)
                    {
                        Console.WriteLine([|D.Goo(x, ")|]);
                    }
                }

                public static string Goo(int x, T y)
                {
                    string s = null;
                    return s;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547246")]
    public async Task CodeIssueAtRightSpan()
    {
        await TestSpansAsync("""
            using goo = System.Console;
            class Program
            {
                static void Main(string[] args)
                {
                    [|System.Console|].Read();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579172")]
    public async Task Bugfix_579172()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C<T, S>
            {
                class D : C<[|D.D|], D.D.D>
                {
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633182")]
    public async Task Bugfix_633182()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void Goo()
                {
                    ([|this.Goo|])();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627102")]
    public async Task Bugfix_627102()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class B
            {
                static void Goo(int x, object y) {}
                
                static void Goo(long x, object y) {}
            
                static void Goo<T>(dynamic x)
                {
                    Console.WriteLine([|C<T>.Goo|](x, "));
                }

                static void Main()
                {
                    Goo<string>(0);
                }
            }

            class C<T> : B
            {
                public static string Goo(int x, T y)
                {
                    return "Hello world";
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629572")]
    public async Task DoNotIncludeAliasNameIfLastTargetNameIsTheSame_1()
    {
        await TestSpansAsync("""
            using Generic = System.Collections.Generic;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = new [|System.Collections|].Generic.List<int>();
                }
            }
            """);

        await TestInRegularAndScriptAsync(
            """
            using Generic = System.Collections.Generic;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = new [|System.Collections|].Generic.List<int>();
                }
            }
            """,
            """
            using Generic = System.Collections.Generic;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Generic.List<int>();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629572")]
    public async Task DoNotIncludeAliasNameIfLastTargetNameIsTheSame_2()
    {
        await TestSpansAsync("""
            using Console = System.Console;
            class Program
            {
                static void Main(string[] args)
                {
                    [|System|].Console.WriteLine("goo");
                }
            }
            """);

        await TestInRegularAndScriptAsync(
            """
            using Console = System.Console;
            class Program
            {
                static void Main(string[] args)
                {
                    [|System|].Console.WriteLine("goo");
                }
            }
            """,
            """
            using Console = System.Console;
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine("goo");
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/736377")]
    public async Task DoNotSimplifyTypeNameBrokenCode()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                public static void GetA

                [[|System.Diagnostics|].CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
                public static ISet<string> GetAllFilesInSolution()
                {
                    return null;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813385")]
    public async Task DoNotSimplifyAliases()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using Goo = System.Int32;

            class C
            {
                [|Goo|] f;
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825541")]
    public async Task ShowOnlyRelevantSpanForReductionOfGenericName()
    {
        await TestSpansAsync("""
            namespace A
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = A.B.OtherClass.Test[|<int>|](5);
                    }
                }

                namespace B
                {
                    class OtherClass
                    {
                        public static int Test<T>(T t) { return 5; }
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/878773")]
    public async Task DoNotSimplifyAttributeNameWithJustAttribute()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            [[|Attribute|]]
            class Attribute : System.Attribute
            {
            }
            """);
    }

    [Fact]
    public async Task ThisQualificationOnFieldOption()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int x;

                public void z()
                {
                    [|this|].x = 4;
                }
            }
            """, new TestParameters(options: Option(CodeStyleOptions2.QualifyFieldAccess, true, NotificationOption2.Error)));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInLocalDeclarationDefaultValue1()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            class C
            {
                [|System.Int32|] x;

                public void z()
                {
                }
            }
            """,
            """
            class C
            {
                int x;

                public void z()
                {
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInLocalDeclarationDefaultValue2()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                [|System.Int32|]? x;
                public void z()
                {
                }
            }
            """, """
            class C
            {
                int? x;
                public void z()
                {
                }
            }
            """, options: PreferIntrinsicTypeEverywhere);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInsideCref_Default_1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                /// <see cref="[|Int32|]"/>
                public void z()
                {
                }
            }
            """, """
            using System;
            class C
            {
                /// <see cref="int"/>
                public void z()
                {
                }
            }
            """, options: PreferIntrinsicTypeInMemberAccess);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInsideCref_Default_2()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// <see cref="[|System.Int32|]"/>
                public void z()
                {
                }
            }
            """, """
            class C
            {
                /// <see cref="int"/>
                public void z()
                {
                }
            }
            """, options: PreferIntrinsicTypeEverywhere);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInsideCref_Default_3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                /// <see cref="[|Int32|].MaxValue"/>
                public void z()
                {
                }
            }
            """, """
            using System;
            class C
            {
                /// <see cref="int.MaxValue"/>
                public void z()
                {
                }
            }
            """, options: PreferIntrinsicTypeEverywhere);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
    public async Task TestIntrinsicTypesInsideCref_NonDefault_1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                /// <see cref="[|Int32|]"/>
                public void z()
                {
                }
            }
            """, new TestParameters(options: Option(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false, NotificationOption2.Error)));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
    public async Task TestIntrinsicTypesInsideCref_NonDefault_2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                /// <see cref="[|Int32|]"/>
                public void z()
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                /// <see cref="int"/>
                public void z()
                {
                }
            }
            """, options: PreferIntrinsicTypeInMemberAccess);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
    public async Task TestIntrinsicTypesInsideCref_NonDefault_3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                /// <see cref="[|Int32|].MaxValue"/>
                public void z()
                {
                }
            }
            """, new TestParameters(options: Option(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false, NotificationOption2.Error)));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
    public async Task TestIntrinsicTypesInsideCref_NonDefault_4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                /// <see cref="[|Int32|].MaxValue"/>
                public void z()
                {
                }
            }
            """,
            """
            using System;

            class C
            {
                /// <see cref="int.MaxValue"/>
                public void z()
                {
                }
            }
            """,
options: PreferIntrinsicTypeInMemberAccess);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
    public async Task TestIntrinsicTypesInsideCref_NonDefault_5()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                /// <see cref="System.Collections.Generic.List{T}.CopyTo([|System.Int32|], T[], int, int)"/>
                public void z()
                {
                }
            }
            """, new TestParameters(options: Option(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false, NotificationOption2.Error)));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
    public async Task TestIntrinsicTypesInsideCref_NonDefault_6_PreferMemberAccess()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// <see cref="System.Collections.Generic.List{T}.CopyTo([|System.Int32|], T[], int, int)"/>
                public void z()
                {
                }
            }
            """,
            """
            class C
            {
                /// <see cref="System.Collections.Generic.List{T}.CopyTo(int, T[], int, int)"/>
                public void z()
                {
                }
            }
            """,
options: PreferIntrinsicTypeInMemberAccess);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")]
    public async Task TestIntrinsicTypesInsideCref_NonDefault_6_PreferDeclaration()
    {
        await TestMissingAsync(
            """
            class C
            {
                /// <see cref="System.Collections.Generic.List{T}.CopyTo([|System.Int32|], T[], int, int)"/>
                public void z()
                {
                }
            }
            """, new TestParameters(options: PreferIntrinsicTypeInDeclaration));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInLocalDeclarationNonDefaultValue_1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [|System.Int32|] x;

                public void z(System.Int32 y)
                {
                    System.Int32 z = 9;
                }
            }
            """, new TestParameters(options: Option(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption2.Error)));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInLocalDeclarationNonDefaultValue_2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Int32 x;

                public void z([|System.Int32|] y)
                {
                    System.Int32 z = 9;
                }
            }
            """, new TestParameters(options: Option(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption2.Error)));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInLocalDeclarationNonDefaultValue_3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Int32 x;

                public void z(System.Int32 y)
                {
                    [|System.Int32|] z = 9;
                }
            }
            """, new TestParameters(options: Option(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, false, NotificationOption2.Error)));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInMemberAccess_Default_1()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                public void z()
                {
                    var sss = [|System.Int32|].MaxValue;
                }
            }
            """,
            """
            class C
            {
                public void z()
                {
                    var sss = int.MaxValue;
                }
            }
            """, options: PreferIntrinsicTypeInMemberAccess);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInMemberAccess_Default_2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void z()
                {
                    var sss = [|Int32|].MaxValue;
                }
            }
            """,
            """
            using System;

            class C
            {
                public void z()
                {
                    var sss = int.MaxValue;
                }
            }
            """, options: PreferIntrinsicTypeInMemberAccess);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/956667")]
    public async Task TestIntrinsicTypesInMemberAccess_Default_3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C1
            {
                public static void z()
                {
                    var sss = [|C2.Memb|].ToString();
                }
            }

            class C2
            {
                public static int Memb;
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInMemberAccess_NonDefault_1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void z()
                {
                    var sss = [|Int32|].MaxValue;
                }
            }
            """, new TestParameters(options: Option(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false, NotificationOption2.Error)));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")]
    public async Task TestIntrinsicTypesInMemberAccess_NonDefault_2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public void z()
                {
                    var sss = [|System.Int32|].MaxValue;
                }
            }
            """, new TestParameters(options: Option(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, false, NotificationOption2.Error)));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965208")]
    public async Task TestSimplifyDiagnosticId()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void z()
                {
                    [|System.Console.WriteLine|](");
                }
            }
            """,
            """
            using System;

            class C
            {
                public void z()
                {
                    Console.WriteLine(");
                }
            }
            """);

        await TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                public void z()
                {
                    [|System.Int32|] a;
                }
            }
            """,
            """
            using System;

            class C
            {
                public void z()
                {
                    int a;
                }
            }
            """, parameters: new TestParameters(options: PreferIntrinsicTypeEverywhere));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1019276")]
    public async Task TestSimplifyTypeNameDoesNotAddUnnecessaryParens()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            using System;

            class Program
            {
                static void F()
                {
                    object o = null;
                    if (![|(o is Byte)|])
                    {
                    }
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void F()
                {
                    object o = null;
                    if (!(o is byte))
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068445")]
    public async Task TestSimplifyTypeNameInPropertyLambda()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            namespace ClassLibrary2
            {
                public class Class1
                {
                    public object X => ([|System.Int32|])0;
                }
            }
            """,
            """
            namespace ClassLibrary2
            {
                public class Class1
                {
                    public object X => (int)0;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068445")]
    public async Task TestSimplifyTypeNameInMethodLambda()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            class C
            {
                public string Goo() => ([|System.String|])";
            }
            """,
            """
            class C
            {
                public string Goo() => (string)";
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1068445")]
    public async Task TestSimplifyTypeNameInIndexerLambda()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            class C
            {
                public int this[int index] => ([|System.Int32|])0;
            }
            """,
            """
            class C
            {
                public int this[int index] => (int)0;
            }
            """);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=388744")]
    public async Task SimplifyTypeNameWithOutDiscard()
    {
        await TestAsync(
            """
            class C
            {
                static void F()
                {
                    [|C.G|](out _);
                }
                static void G(out object o)
                {
                    o = null;
                }
            }
            """,
            """
            class C
            {
                static void F()
                {
                    G(out _);
                }
                static void G(out object o)
                {
                    o = null;
                }
            }
            """,
            parseOptions: CSharpParseOptions.Default);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=388744")]
    public async Task SimplifyTypeNameWithOutDiscard_FeatureDisabled()
    {
        await TestAsync(
            """
            class C
            {
                static void F()
                {
                    [|C.G|](out _);
                }
                static void G(out object o)
                {
                    o = null;
                }
            }
            """,
            """
            class C
            {
                static void F()
                {
                    G(out _);
                }
                static void G(out object o)
                {
                    o = null;
                }
            }
            """,
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15996")]
    public async Task TestMemberOfBuiltInType1()
    {
        await TestAsync(
            """
            using System;
            class C
            {
                void Main()
                {
                    [|UInt32|] value = UInt32.MaxValue;
                }
            }
            """,
            """
            using System;
            class C
            {
                void Main()
                {
                    uint value = UInt32.MaxValue;
                }
            }
            """,
            parseOptions: CSharpParseOptions.Default,
            options: PreferIntrinsicTypeInDeclaration);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15996")]
    public async Task TestMemberOfBuiltInType2()
    {
        await TestAsync(
            """
            using System;
            class C
            {
                void Main()
                {
                    UInt32 value = [|UInt32|].MaxValue;
                }
            }
            """,
            """
            using System;
            class C
            {
                void Main()
                {
                    UInt32 value = uint.MaxValue;
                }
            }
            """,
            parseOptions: CSharpParseOptions.Default,
            options: PreferIntrinsicTypeInMemberAccess);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15996")]
    public async Task TestMemberOfBuiltInType3()
    {
        await TestAsync(
            """
            using System;
            class C
            {
                void Main()
                {
                    [|UInt32|].Parse("goo");
                }
            }
            """,
            """
            using System;
            class C
            {
                void Main()
                {
                    uint.Parse("goo");
                }
            }
            """,
            parseOptions: CSharpParseOptions.Default,
            options: PreferIntrinsicTypeInMemberAccess);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26923")]
    public async Task NoSuggestionOnForeachCollectionExpression()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string[] args)
                {
                    foreach (string arg in [|args|])
                    {

                    }
                }
            }
            """, new TestParameters(options: PreferImplicitTypeEverywhere));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26923")]
    public async Task NoSuggestionOnForeachType()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string[] args)
                {
                    foreach ([|string|] arg in args)
                    {

                    }
                }
            }
            """, new TestParameters(options: PreferImplicitTypeEverywhere));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31849")]
    public async Task NoSuggestionOnNestedNullabilityRequired()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            #nullable enable
            using System.Threading.Tasks;

            class C
            {
                Task<string?> FooAsync()
                {
                    return Task.FromResult<[|string?|]>("something");
                }
            }
            """);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/20377")]
    public async Task TestWarningLevel(int warningLevel)
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            namespace Root
            {
                class A
                {
                    [|System.Exception|] c;
                }
            }
            """,
            """
            using System;

            namespace Root
            {
                class A
                {
                    Exception c;
                }
            }
            """, compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, warningLevel: warningLevel));
    }

    [Fact]
    public async Task TestGlobalAliasSimplifiesInUsingDirective()
    {
        await TestInRegularAndScriptAsync(
            "using [|global::System.IO|];",
            "using System.IO;");
    }

    [Theory]
    [InlineData("Boolean")]
    [InlineData("Char")]
    [InlineData("String")]
    [InlineData("Int8")]
    [InlineData("UInt8")]
    [InlineData("Int16")]
    [InlineData("UInt16")]
    [InlineData("Int32")]
    [InlineData("UInt32")]
    [InlineData("Int64")]
    [InlineData("UInt64")]
    [InlineData("Float32")]
    [InlineData("Float64")]
    public async Task TestGlobalAliasSimplifiesInUsingAliasDirective(string typeName)
    {
        await TestInRegularAndScriptAsync(
            $"using My{typeName} = [|global::System.{typeName}|];",
            $"using My{typeName} = System.{typeName};");
    }

    [Fact]
    public async Task TestGlobalAliasSimplifiesInUsingStaticDirective()
    {
        await TestInRegularAndScriptAsync(
            "using static [|global::System.Math|];",
            "using static System.Math;");
    }

    [Fact]
    public async Task TestGlobalAliasSimplifiesInUsingDirectiveInNamespace()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            namespace N
            {
                using [|global::System.IO|];
            }
            """,
            """
            using System;
            namespace N
            {
                using System.IO;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40639")]
    public async Task TestCrefIdAtTopLevel()
    {
        await TestDiagnosticInfoAsync(
            """
            /// <summary>
            /// <see cref="[|System.String|]"/>
            /// </summary>
            class Base
            {
            }
            """, IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId, DiagnosticSeverity.Hidden);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40639")]
    public async Task TestCrefIdAtNestedLevel()
    {
        await TestDiagnosticInfoAsync(
            """
            /// <summary>
            /// <see cref="Foo([|System.String|])"/>
            /// </summary>
            class Base
            {
                public void Foo(string s) { }
            }
            """, IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId, DiagnosticSeverity.Hidden);
    }

    [Theory]
    [InlineData("Boolean")]
    [InlineData("Char")]
    [InlineData("String")]
    [InlineData("Int16")]
    [InlineData("UInt16")]
    [InlineData("Int32")]
    [InlineData("UInt32")]
    [InlineData("Int64")]
    [InlineData("UInt64")]
    public async Task TestGlobalAliasSimplifiesInUsingAliasDirectiveWithinNamespace(string typeName)
    {
        await TestInRegularAndScriptAsync(
$@"using System;
namespace N
{{
    using My{typeName} = [|global::System.{typeName}|];
}}",
$@"using System;
namespace N
{{
    using My{typeName} = {typeName};
}}");
    }

    [Theory]
    [InlineData("Int8")]
    [InlineData("UInt8")]
    [InlineData("Float32")]
    [InlineData("Float64")]
    public async Task TestGlobalAliasSimplifiesInUsingAliasDirectiveWithinNamespace_UnboundName(string typeName)
    {
        await TestInRegularAndScriptAsync(
$@"using System;
namespace N
{{
    using My{typeName} = [|global::System.{typeName}|];
}}",
$@"using System;
namespace N
{{
    using My{typeName} = System.{typeName};
}}");
    }

    [Fact]
    public async Task TestGlobalAliasSimplifiesInUsingStaticDirectiveInNamespace()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            namespace N
            {
                using static [|global::System.Math|];
            }
            """,
            """
            using System;
            namespace N
            {
                using static System.Math;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27819")]
    public async Task DoNotSimplifyToVar_EvenIfVarIsPreferred()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void Goo()
                {
                    [|System.Int32|] i = 0;
                }
            }
            """,
            """
            class C
            {
                void Goo()
                {
                    int i = 0;
                }
            }
            """, options: PreferImplicitTypeEverywhere);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27819")]
    public async Task DoNotSimplifyToVar_EvenIfVarIsPreferred_2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void Goo()
                {
                    [|int|] i = 0;
                }
            }
            """, new TestParameters(options: PreferImplicitTypeEverywhere));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40647")]
    public async Task SimplifyMemberAccessOverPredefinedType()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class Base
            {
                public void Goo(object o1, object o2) => [|Object|].ReferenceEquals(o1, o2);
            }
            """,
            """
            using System;

            class Base
            {
                public void Goo(object o1, object o2) => ReferenceEquals(o1, o2);
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40649")]
    public async Task SimplifyAliasToGeneric1()
    {
        await TestInRegularAndScript1Async(
            """
            using System.Collections.Generic;
            using MyList = System.Collections.Generic.List<int>;

            class Base
            {
                public [|System.Collections.Generic.List<int>|] Goo;
            }
            """,
            """
            using System.Collections.Generic;
            using MyList = System.Collections.Generic.List<int>;

            class Base
            {
                public MyList Goo;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40649")]
    public async Task SimplifyAliasToGeneric2()
    {
        await TestInRegularAndScript1Async(
            """
            using System.Collections.Generic;
            using MyList = System.Collections.Generic.List<int>;

            class Base
            {
                public [|List<int>|] Goo;
            }
            """,
            """
            using System.Collections.Generic;
            using MyList = System.Collections.Generic.List<int>;

            class Base
            {
                public MyList Goo;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40649")]
    public async Task SimplifyAliasToGeneric3()
    {
        await TestInRegularAndScript1Async(
            """
            using System.Collections.Generic;
            using MyList = System.Collections.Generic.List<int>;

            class Base
            {
                public [|List<System.Int32>|] Goo;
            }
            """,
            """
            using System.Collections.Generic;
            using MyList = System.Collections.Generic.List<int>;

            class Base
            {
                public MyList Goo;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40649")]
    public async Task DoNotSimplifyIncorrectInstantiation()
    {
        await TestMissingAsync(
            """
            using System.Collections.Generic;
            using MyList = System.Collections.Generic.List<int>;

            class Base
            {
                public [|List<string>|] Goo;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40663")]
    public async Task SimplifyInTypeOf()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    var v = typeof([|Object|]);
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    var v = typeof(object);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40876")]
    public async Task SimplifyPredefinedTypeInUsingDirective1()
    {
        await TestWithPredefinedTypeOptionsAsync(
            """
            using System;

            namespace N
            {
                using Alias1 = [|System|].Object;

                class C
                {
                    Alias1 a1;
                }
            }
            """,

            """
            using System;

            namespace N
            {
                using Alias1 = Object;

                class C
                {
                    Alias1 a1;
                }
            }
            """);
    }

    [Fact]
    public async Task TestSimplifyTopLevelOfCrefOnly1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            namespace A.B.C
            {
                /// <summary>
                /// <see cref="[|A.B.C|].X"/>
                /// </summary>
                class X
                {
                }
            }
            """,
            """
            using System;

            namespace A.B.C
            {
                /// <summary>
                /// <see cref="X"/>
                /// </summary>
                class X
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestSimplifyTopLevelOfCrefOnly2()
    {
        await TestSpansAsync(
            """
            using System;

            namespace A.B.C
            {
                /// <summary>
                /// <see cref="[|A.B.C|].X"/>
                /// </summary>
                class X
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestSimplifyTopLevelOfCrefOnly4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            namespace A.B.C
            {
                /// <summary>
                /// <see cref="[|A.B.C.X|].Y(A.B.C.X)"/>
                /// </summary>
                class X
                {
                    void Y(X x) { }
                }
            }
            """,
            """
            using System;

            namespace A.B.C
            {
                /// <summary>
                /// <see cref="Y(X)"/>
                /// </summary>
                class X
                {
                    void Y(X x) { }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSimplifyTopLevelOfCrefOnly5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            namespace A.B.C
            {
                /// <summary>
                /// <see cref="A.B.C.X.Y([|A.B.C|].X)"/>
                /// </summary>
                class X
                {
                    void Y(X x) { }
                }
            }
            """,
            """
            using System;

            namespace A.B.C
            {
                /// <summary>
                /// <see cref="A.B.C.X.Y(X)"/>
                /// </summary>
                class X
                {
                    void Y(X x) { }
                }
            }
            """);
    }

    [Theory]
    [InlineData("Boolean")]
    [InlineData("Char")]
    [InlineData("String")]
    [InlineData("Int8")]
    [InlineData("UInt8")]
    [InlineData("Int16")]
    [InlineData("UInt16")]
    [InlineData("Int32")]
    [InlineData("UInt32")]
    [InlineData("Int64")]
    [InlineData("UInt64")]
    [InlineData("Float32")]
    [InlineData("Float64")]
    public async Task TestDoesNotSimplifyUsingAliasDirectiveToPrimitiveType(string typeName)
    {
        await TestMissingAsync(
$@"using System;
namespace N
{{
    using My{typeName} = [|{typeName}|];
}}");
    }

    [Theory]
    [InlineData("Boolean")]
    [InlineData("Char")]
    [InlineData("String")]
    [InlineData("Int16")]
    [InlineData("UInt16")]
    [InlineData("Int32")]
    [InlineData("UInt32")]
    [InlineData("Int64")]
    [InlineData("UInt64")]
    public async Task TestSimplifyUsingAliasDirectiveToQualifiedBuiltInType(string typeName)
    {
        await TestInRegularAndScript1Async(
$@"using System;
namespace N
{{
    using My{typeName} = [|System.{typeName}|];
}}",
$@"using System;
namespace N
{{
    using My{typeName} = {typeName};
}}");
    }

    [Theory]
    [InlineData("Int8")]
    [InlineData("UInt8")]
    [InlineData("Float32")]
    [InlineData("Float64")]
    public async Task TestDoesNotSimplifyUsingAliasWithUnboundTypes(string typeName)
    {
        await TestMissingInRegularAndScriptAsync(
$@"using System;
namespace N
{{
    using My{typeName} = [|System.{typeName}|];
}}");
    }

    [Fact]
    public async Task SimplifyMemberAccessOffOfObjectKeyword()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                bool Goo()
                {
                    return [|object|].Equals(null, null);
                }
            }
            """,
            """
            using System;

            class C
            {
                bool Goo()
                {
                    return Equals(null, null);
                }
            }
            """);
    }

    [Fact]
    public async Task DoNotSimplifyBaseCallToVirtualInNonSealedClass()
    {
        await TestMissingAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    var v = [|base|].GetHashCode();
                }
            }
            """);
    }

    [Fact]
    public async Task DoSimplifyBaseCallToVirtualInSealedClass()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            sealed class C
            {
                void Goo()
                {
                    var v = [|base|].GetHashCode();
                }
            }
            """,
            """
            using System;

            sealed class C
            {
                void Goo()
                {
                    var v = GetHashCode();
                }
            }
            """);
    }

    [Fact]
    public async Task DoSimplifyBaseCallToVirtualInStruct()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            struct C
            {
                void Goo()
                {
                    var v = [|base|].GetHashCode();
                }
            }
            """,
            """
            using System;

            struct C
            {
                void Goo()
                {
                    var v = GetHashCode();
                }
            }
            """);
    }

    [Fact]
    public async Task DoNotSimplifyBaseCallToVirtualWithOverride()
    {
        await TestMissingAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    var v = [|base|].GetHashCode();
                }

                public override int GetHashCode() => 0;
            }
            """);
    }

    [Fact]
    public async Task DoSimplifyBaseCallToNonVirtual()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class Base
            {
                public int Baz() => 0;
            }

            class C : Base
            {
                void Goo()
                {
                    var v = [|base|].Baz();
                }
            }
            """,
            """
            using System;

            class Base
            {
                public int Baz() => 0;
            }

            class C : Base
            {
                void Goo()
                {
                    var v = Baz();
                }
            }
            """);
    }

    [Fact]
    public async Task DoNotSimplifyBaseCallIfOverloadChanges()
    {
        await TestMissingAsync(
            """
            using System;

            class Base
            {
                public int Baz(object o) => 0;
            }

            class C : Base
            {
                void Goo()
                {
                    var v = [|base|].Baz(0);
                }

                public int Baz(int o) => 0;
            }
            """);
    }

    [Fact]
    public async Task DoNotSimplifyInsideNameof()
    {
        await TestMissingAsync(
            """
            using System;

            class Base
            {
                public int Baz(string type)
                    => type switch
                    {
                        nameof([|Int32|]) => 0,
                    };
            }
            """);
    }

    [Fact]
    public async Task DoSimplifyInferrableTypeArgumentList()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class Base
            {
                public void Goo() => Bar[|<int>|](0);
                public void Bar<T>(T t) => default;
            }
            """,
            """
            using System;

            class Base
            {
                public void Goo() => Bar(0);
                public void Bar<T>(T t) => default;
            }
            """);
    }

    [Fact]
    public async Task DoNotSimplifyNonInferrableTypeArgumentList()
    {
        await TestMissingAsync(
            """
            using System;

            class Base
            {
                public void Goo() => Bar[|<int>|](0);
                public void Bar<T>() => default;
            }
            """);
    }

    [Fact]
    public async Task SimplifyEnumMemberReferenceInsideEnum()
    {
        await TestInRegularAndScript1Async(
            """
            enum E
            {
                Goo = 1,
                Bar = [|E|].Goo,
            }
            """,
            """
            enum E
            {
                Goo = 1,
                Bar = Goo,
            }
            """);
    }

    [Fact]
    public async Task SimplifyEnumMemberReferenceInsideEnumDocComment()
    {
        await TestInRegularAndScript1Async(
            """
            /// <summary>
            /// <see cref="[|E|].Goo"/>
            /// </summary>
            enum E
            {
                Goo = 1,
            }
            """,
            """
            /// <summary>
            /// <see cref="Goo"/>
            /// </summary>
            enum E
            {
                Goo = 1,
            }
            """);
    }

    [Fact]
    public async Task TestInstanceMemberReferenceInCref1()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// <see cref="[|C.z|]"/>
                public void z()
                {
                }
            }
            """, """
            class C
            {
                /// <see cref="z"/>
                public void z()
                {
                }
            }
            """);
    }

    [Fact]
    public async Task SimplifyAttributeReference1()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class GooAttribute : Attribute
            {
            }

            [Goo[|Attribute|]]
            class Bar
            {
            }
            """,
            """
            using System;

            class GooAttribute : Attribute
            {
            }

            [Goo]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task SimplifyAttributeReference2()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class GooAttribute : Attribute
            {
            }

            [Goo[|Attribute|]()]
            class Bar
            {
            }
            """,
            """
            using System;

            class GooAttribute : Attribute
            {
            }

            [Goo()]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task SimplifyGenericAttributeReference1()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class GooAttribute<T> : Attribute
            {
            }

            [Goo[|Attribute|]<string>]
            class Bar
            {
            }
            """,
            """
            using System;
            
            class GooAttribute<T> : Attribute
            {
            }
            
            [Goo<string>]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task SimplifyGenericAttributeReference2()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class GooAttribute<T> : Attribute
            {
            }

            [Goo[|Attribute|]<string>()]
            class Bar
            {
            }
            """,
            """
            using System;
            
            class GooAttribute<T> : Attribute
            {
            }
            
            [Goo<string>()]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task SimplifyGenericAttributeReference3()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class GooAttribute<T> : Attribute
            {
                public class AnotherAttribute<U> : Attribute;
            }

            [GooAttribute<string>.Another[|Attribute|]<string>()]
            class Bar
            {
            }
            """,
            """
            using System;
            
            class GooAttribute<T> : Attribute
            {
                public class AnotherAttribute<U> : Attribute;
            }
            
            [GooAttribute<string>.Another<string>()]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task SimplifyGenericAttributeReference4()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class GooAttribute<T> : Attribute
            {
                public class AnotherAttribute<U> : Attribute;
            }

            [GooAttribute<string>.Another[|Attribute|]<string>]
            class Bar
            {
            }
            """,
            """
            using System;
            
            class GooAttribute<T> : Attribute
            {
                public class AnotherAttribute<U> : Attribute;
            }
            
            [GooAttribute<string>.Another<string>]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task DoNotSimplifyNestedInsideGenericAttributeReference1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class GooAttribute<T> : Attribute
            {
                public class AnotherAttribute : Attribute;
            }

            [Goo[|Attribute|]<string>.Another()]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task DoNotSimplifyNestedInsideGenericAttributeReference2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class GooAttribute<T> : Attribute
            {
                public class AnotherAttribute : Attribute;
            }

            [Goo[|Attribute|]<string>.Another]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task DoNotSimplifyNestedInsideGenericAttributeReference3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class GooAttribute<T> : Attribute
            {
                public class AnotherAttribute : Attribute;
            }

            [Goo[|Attribute|]<string>.AnotherAttribute]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task DoNotSimplifyNestedInsideGenericAttributeReference4()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class GooAttribute<T> : Attribute
            {
                public class AnotherAttribute : Attribute;
            }

            [Goo[|Attribute|]<string>.AnotherAttribute()]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task SimplifyAttributeReference3()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            namespace N
            {
                class GooAttribute : Attribute
                {
                }
            }

            [N.Goo[|Attribute|]()]
            class Bar
            {
            }
            """,
            """
            using System;

            namespace N
            {
                class GooAttribute : Attribute
                {
                }
            }

            [N.Goo()]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task SimplifyAttributeReference4()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            namespace N
            {
                class GooAttribute : Attribute
                {
                }
            }

            [N.GooAttribute([|typeof(System.Int32)|])]
            class Bar
            {
            }
            """,
            """
            using System;

            namespace N
            {
                class GooAttribute : Attribute
                {
                }
            }

            [N.GooAttribute(typeof(int))]
            class Bar
            {
            }
            """);
    }

    [Fact]
    public async Task SimplifySystemAttribute()
    {
        await TestInRegularAndScript1Async(
            """
            using System;
            using System.Runtime.Serialization;

            namespace Microsoft
            {
                [[|System|].Serializable]
                public struct ClassifiedToken
                {
                }
            }
            """,
            """
            using System;
            using System.Runtime.Serialization;

            namespace Microsoft
            {
                [Serializable]
                public struct ClassifiedToken
                {
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40633")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542100")]
    public async Task TestAllowSimplificationThatWouldNotCauseConflict1()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main()
                    {
                        [|N.Program|].Goo.Bar();
                        {
                            int Goo;
                        }
                    }
                }
            }
            """,
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main()
                    {
                        Goo.Bar();
                        {
                            int Goo;
                        }
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542100")]
    public async Task TestAllowSimplificationThatWouldNotCauseConflict2()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main()
                    {
                        [|Program|].Goo.Bar();
                        {
                            int Goo;
                        }
                    }
                }
            }
            """,
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main()
                    {
                        Goo.Bar();
                        {
                            int Goo;
                        }
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542100")]
    public async Task TestPreventSimplificationThatWouldCauseConflict1()
    {
        await TestInRegularAndScript1Async(
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main()
                    {
                        [|N|].Program.Goo.Bar();
                        int Goo;
                    }
                }
            }
            """,
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main()
                    {
                        Program.Goo.Bar();
                        int Goo;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542100")]
    public async Task TestPreventSimplificationThatWouldCauseConflict2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            namespace N
            {
                class Program
                {
                    class Goo
                    {
                        public static void Bar()
                        {
                        }
                    }

                    static void Main(int[] args)
                    {
                        [|Program|].Goo.Bar();
                        int Goo;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSimplifyPredefinedTypeMemberAccessThatIsInScope()
    {
        await TestInRegularAndScript1Async(
            """
            using static System.Int32;

            class Goo
            {
                public void Bar(string a)
                {
                    var v = [|int|].Parse(a);
                }
            }
            """,
            """
            using static System.Int32;

            class Goo
            {
                public void Bar(string a)
                {
                    var v = Parse(a);
                }
            }
            """);
    }

    [Theory]
    [InlineData("Boolean")]
    [InlineData("Char")]
    [InlineData("String")]
    [InlineData("Int16")]
    [InlineData("UInt16")]
    [InlineData("Int32")]
    [InlineData("UInt32")]
    [InlineData("Int64")]
    [InlineData("UInt64")]
    public async Task TestDoesNotSimplifyUsingAliasDirectiveToBuiltInType(string typeName)
    {
        await TestInRegularAndScript1Async(
$@"using System;
namespace N
{{
    using My{typeName} = [|System.{typeName}|];
}}",
$@"using System;
namespace N
{{
    using My{typeName} = {typeName};
}}");
    }

    [Fact]
    public async Task TestDoNotSimplifyIfItWouldIntroduceAmbiguity()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using A;
            using B;

            namespace A
            {
                class Goo { }
            }

            namespace B
            {
                class Goo { }
            }

            class C
            {
                void Bar(object o)
                {
                    var x = ([|A|].Goo)o;
                }
            }
            """);
    }

    [Fact]
    public async Task TestDoNotSimplifyIfItWouldIntroduceAmbiguity2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using A;

            namespace A
            {
                class Goo { }
            }

            namespace B
            {
                class Goo { }
            }

            namespace N
            {
                using B;

                class C
                {
                    void Bar(object o)
                    {
                        var x = ([|A|].Goo)o;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestAllowSimplificationWithoutAmbiguity2()
    {
        await TestInRegularAndScript1Async(
            """
            using A;

            namespace A
            {
                class Goo { }
            }

            namespace B
            {
                class Goo { }
            }

            namespace N
            {
                using A;

                class C
                {
                    void Bar(object o)
                    {
                        var x = ([|A|].Goo)o;
                    }
                }
            }
            """,
            """
            using A;

            namespace A
            {
                class Goo { }
            }

            namespace B
            {
                class Goo { }
            }

            namespace N
            {
                using A;

                class C
                {
                    void Bar(object o)
                    {
                        var x = (Goo)o;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168")]
    public async Task SimplifyCrefAliasPredefinedType_OnClass()
    {
        await TestInRegularAndScriptAsync(
            """
            namespace N1
            {
                /// <see cref="[|System.Int32|]"/>
                public class C1
                {
                    public C1()
                    {
                    }
                }
            }
            """,
            """
            namespace N1
            {
                /// <see cref="int"/>
                public class C1
                {
                    public C1()
                    {
                    }
                }
            }
            """, options: PreferIntrinsicTypeEverywhere);
    }

    [Fact]
    public async Task TestMissingOnInstanceMemberAccessOfOtherValue()
    {
        var content =
            """
            using System;

            internal struct BitVector : IEquatable<BitVector>
            {
                public bool Equals(BitVector other)
                {
                }

                public override bool Equals(object obj)
                {
                    return obj is BitVector other && Equals(other);
                }

                public static bool operator ==(BitVector left, BitVector right)
                {
                    return [|left|].Equals(right);
                }
            }
            """;

        await TestMissingInRegularAndScriptAsync(content);
    }

    [Fact]
    public async Task TestSimplifyStaticMemberAccessThroughDerivedType()
    {
        var source =
            """
            class Base
            {
                public static int Y;
            }

            class Derived : Base
            {
            }

            static class M
            {
                public static void Main()
                {
                    int k = [|Derived|].Y;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source,
            """
            class Base
            {
                public static int Y;
            }

            class Derived : Base
            {
            }

            static class M
            {
                public static void Main()
                {
                    int k = Base.Y;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22493")]
    public async Task TestSimplifyCallWithDynamicArg()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class P
            {
                public static void Main()
                {
                    dynamic y = null;
                    [|System|].Console.WriteLine(y);
                }
            }
            """,
            """
            using System;

            class P
            {
                public static void Main()
                {
                    dynamic y = null;
                    Console.WriteLine(y);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22493")]
    public async Task TestDoSimplifyCallWithDynamicArgWhenCallingThroughDerivedClass()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Base
            {
                public static void Goo(int i) { }
            }

            class Derived
            {
            }

            class P
            {
                public static void Main()
                {
                    dynamic y = null;
                    [|Derived|].Goo(y);
                }
            }
            """);
    }

    [Fact]
    public async Task TestNameofReportsSimplifyMemberAccess()
    {
        await TestDiagnosticInfoAsync(
            """
            using System;

            class Base
            {
                void Goo()
                {
                    var v = nameof([|System|].Int32);
                }
            }
            """, IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId, DiagnosticSeverity.Hidden);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40972")]
    public async Task TestNameofReportsSimplifyMemberAccessForMemberGroup1()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class Base
            {
                void Goo()
                {
                    var v = nameof([|Base|].Goo);
                }
            }
            """,
            """
            using System;

            class Base
            {
                void Goo()
                {
                    var v = nameof(Goo);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40972")]
    public async Task TestNameofReportsSimplifyMemberAccessForMemberGroup2()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class Base
            {
                static void Goo()
                {
                    var v = nameof([|Base|].Goo);
                }
            }
            """,
            """
            using System;

            class Base
            {
                static void Goo()
                {
                    var v = nameof(Goo);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40972")]
    public async Task TestNameofReportsSimplifyMemberAccessForMemberGroup3()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class Base
            {
                void Goo()
                {
                    var v = nameof([|Base|].Goo);
                }

                void Goo(int i) { }
            }
            """,
            """
            using System;

            class Base
            {
                void Goo()
                {
                    var v = nameof(Goo);
                }
            
                void Goo(int i) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40972")]
    public async Task TestNameofReportsSimplifyMemberAccessForMemberGroup4()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class Base
            {
                static void Goo()
                {
                    var v = nameof([|Base|].Goo);
                }
            
                static void Goo(int i) { }
            }
            """,
            """
            using System;

            class Base
            {
                static void Goo()
                {
                    var v = nameof(Goo);
                }

                static void Goo(int i) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11380")]
    public async Task TestNotOnIllegalInstanceCall()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    [|Console.Equals|]("");
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57767")]
    public async Task TestInvocationOffOfFunctionPointerInvocationResult1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Runtime.CompilerServices;

            public ref struct A
            {
                private void Goo()
                {
                }

                public readonly unsafe ref struct B
                {
                    private readonly void* a;

                    public void Dispose()
                    {
                        [|((delegate*<ref byte, ref A>)(delegate*<ref byte, ref byte>)&Unsafe.As<byte, byte>)(ref *(byte*)a)|].Goo();
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57767")]
    public async Task TestInvocationOffOfFunctionPointerInvocationResult2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            public struct A
            {
                private void Goo()
                {
                }

                public readonly unsafe struct B
                {
                    private readonly void* a;

                    public void Dispose()
                    {
                        [|((delegate*<ref byte, ref A>)&Unsafe.As<byte, A>)(ref *(byte*)a)|].Goo();
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestNint1_NoNumericIntPtr()
    {
        var source =
            """
            class A
            {
                [|System.IntPtr|] i;
            }
            """;
        var featureOptions = PreferIntrinsicTypeEverywhere;
        await TestMissingInRegularAndScriptAsync(source, new TestParameters(options: featureOptions));
    }

    [Fact]
    public async Task TestNint1_WithNumericIntPtr_CSharp11()
    {
        var featureOptions = PreferIntrinsicTypeEverywhere;
        await TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" CommonReferencesNet7="true">
                    <Document>class A
            {
                [|System.IntPtr|] i;
            }</Document>
                </Project>
            </Workspace>
            """,
            """
            class A
            {
                nint i;
            }
            """, options: featureOptions);
    }

    [Fact]
    public async Task TestNint1_WithNumericIntPtr_CSharp8()
    {
        var featureOptions = PreferIntrinsicTypeEverywhere;
        await TestMissingInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" CommonReferencesNet7="true" LanguageVersion="8">
                    <Document>class A
            {
                [|System.IntPtr|] i;
            }</Document>
                </Project>
            </Workspace>
            """, new TestParameters(options: featureOptions));
    }

    [Fact]
    public async Task TestNUint1_NoNumericIntPtr()
    {
        var source =
            """
            class A
            {
                [|System.UIntPtr|] i;
            }
            """;
        var featureOptions = PreferIntrinsicTypeEverywhere;
        await TestMissingInRegularAndScriptAsync(source, new TestParameters(options: featureOptions));
    }

    [Fact]
    public async Task TestNUint1_WithNumericIntPtr_CSharp11()
    {
        var featureOptions = PreferIntrinsicTypeEverywhere;
        await TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" CommonReferencesNet7="true">
                    <Document>class A
            {
                [|System.UIntPtr|] i;
            }</Document>
                </Project>
            </Workspace>
            """,
            """
            class A
            {
                nuint i;
            }
            """, options: featureOptions);
    }

    [Fact]
    public async Task TestNUint1_WithNumericIntPtr_CSharp8()
    {
        var featureOptions = PreferIntrinsicTypeEverywhere;
        await TestMissingInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" CommonReferencesNet7="true" LanguageVersion="8">
                    <Document>class A
            {
                [|System.UIntPtr|] i;
            }</Document>
                </Project>
            </Workspace>
            """, new TestParameters(options: featureOptions));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75162")]
    public async Task TestEditorBrowsable1()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System.ComponentModel;

            [EditorBrowsable(EditorBrowsableState.Never)]
            class Base
            {
                public static void Method() { }
            }

            class Derived : Base { }

            class Test
            {
                void M()
                {
                    [|Derived|].Method();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75026")]
    public async Task TestPrimaryConstructorWithReferenceToNestedType()
    {
        await TestMissingAsync(
            """
            public abstract class Base<TOther>();

            public sealed class Concrete() : Base<[|Concrete.Impl|]>()
            {
                public class Impl;
            }
            """);
    }

    private async Task TestWithPredefinedTypeOptionsAsync(string code, string expected, int index = 0)
        => await TestInRegularAndScript1Async(code, expected, index, new TestParameters(options: PreferIntrinsicTypeEverywhere));

    private OptionsCollection PreferIntrinsicTypeEverywhere
        => new(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, true, NotificationOption2.Error },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, this.onWithError },
        };

    private OptionsCollection PreferIntrinsicTypeInDeclaration
        => new(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, true, NotificationOption2.Error },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, this.offWithSilent },
        };

    private OptionsCollection PreferIntrinsicTypeInMemberAccess
        => new(GetLanguage())
        {
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, true, NotificationOption2.Error },
            { CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, this.offWithSilent },
        };

    private OptionsCollection PreferImplicitTypeEverywhere
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
        };

    private readonly CodeStyleOption2<bool> offWithSilent = new(false, NotificationOption2.Silent);
    private readonly CodeStyleOption2<bool> onWithInfo = new(true, NotificationOption2.Suggestion);
    private readonly CodeStyleOption2<bool> onWithError = new(true, NotificationOption2.Error);
}
