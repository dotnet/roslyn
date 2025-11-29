// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddImport;

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpAddImportCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
public sealed class AddImportCodeRefactoringTests
{
    [Fact]
    public Task TestSimpleQualifiedTypeName()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]System.Threading.Tasks.Task M() => null;
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                Task M() => null;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InReturnType()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                System.Threading.Tasks.[||]Task M() => null;
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                Task M() => null;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_GenericType()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]System.Collections.Generic.List<int> M() => null;
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                List<int> M() => null;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InParameter()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M([||]System.Threading.Tasks.Task t) { }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                void M(Task t) { }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InField()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                private [||]System.Threading.Tasks.Task _task;
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                private Task _task;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InLocalVariable()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    [||]System.Threading.Tasks.Task task = null;
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Task task = null;
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InNewExpression()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    var list = new [||]System.Collections.Generic.List<int>();
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = new List<int>();
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InTypeof()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    var type = typeof([||]System.Threading.Tasks.Task);
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    var type = typeof(Task);
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InBaseType()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C : [||]System.Exception
            {
            }
            """,
            """
            using System;

            class C : Exception
            {
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InInterface()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C : [||]System.IDisposable
            {
                public void Dispose() { }
            }
            """,
            """
            using System;

            class C : IDisposable
            {
                public void Dispose() { }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InGenericConstraint()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C<T> where T : [||]System.IDisposable
            {
            }
            """,
            """
            using System;

            class C<T> where T : IDisposable
            {
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InCastExpression()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    var e = ([||]System.Exception)o;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var e = (Exception)o;
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InIsExpression()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    var b = o is [||]System.Exception;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var b = o is Exception;
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InAsExpression()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M(object o)
                {
                    var e = o as [||]System.Exception;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var e = o as Exception;
                }
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_NestedNamespace()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]System.Threading.Tasks.Task<int> M() => null;
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                Task<int> M() => null;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_InAttribute()
        => VerifyCS.VerifyRefactoringAsync(
            """
            [[||]System.Obsolete]
            class C
            {
            }
            """,
            """
            using System;

            [Obsolete]
            class C
            {
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_NotOfferedWhenUsingAlreadyExists()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                [||]System.Threading.Tasks.Task M() => null;
            }
            """);

    [Fact]
    public Task TestQualifiedTypeName_NotOfferedOnNamespace()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([||]System.Threading);
                }
            }
            """);

    [Fact]
    public Task TestStaticMemberAccess()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    [||]System.Console.WriteLine();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task TestStaticMemberAccess_InMiddle()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    System.[||]Console.WriteLine();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task TestStaticMemberAccess_WithExistingUsing()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [||]System.Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task TestGlobalQualifiedName()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]global::System.Threading.Tasks.Task M() => null;
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                Task M() => null;
            }
            """);

    [Fact]
    public Task TestAmbiguity_TypeWithSameNameInScope1()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class Task { }

            class C
            {
                [||]System.Threading.Tasks.Task M() => null;
            }
            """,
            """
            using System.Threading.Tasks;

            class Task { }

            class C
            {
                System.Threading.Tasks.Task M() => null;
            }
            """);

    [Fact]
    public Task TestAmbiguity_TypeWithSameNameInScope2()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using N;

            namespace N
            {
                class Task { }
            }

            class C
            {
                [||]System.Threading.Tasks.Task M() => null;
            }

            class D
            {
                Task M() => null;
            }
            """,
            """
            using System.Threading.Tasks;
            using N;

            namespace N
            {
                class Task { }
            }

            class C
            {
                System.Threading.Tasks.Task M() => null;
            }
            
            class D
            {
                N.Task M() => null;
            }
            """);

    [Fact]
    public Task TestSimplifyAllOccurrences_MultipleUsages()
    {
        return new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    [||]System.Threading.Tasks.Task M1() => null;
                    System.Threading.Tasks.Task M2() => null;
                    System.Threading.Tasks.Task<int> M3() => null;
                }
                """,
            FixedCode = """
                using System.Threading.Tasks;

                class C
                {
                    Task M1() => null;
                    Task M2() => null;
                    Task<int> M3() => null;
                }
                """,
            CodeActionIndex = 1,
        }.RunAsync();
    }

    [Fact]
    public Task TestSimplifyAllOccurrences_MixedUsages()
    {
        return new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    [||]System.Threading.Tasks.Task M1() => null;
                    void M2(System.Threading.Tasks.Task t) { }
                    System.Threading.Tasks.Task<string> _field;
                }
                """,
            FixedCode = """
                using System.Threading.Tasks;

                class C
                {
                    Task M1() => null;
                    void M2(Task t) { }
                    Task<string> _field;
                }
                """,
            CodeActionIndex = 1,
        }.RunAsync();
    }

    [Fact]
    public Task TestSimplifyAllOccurrences_WithOtherNamespaces()
    {
        return new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    [||]System.Threading.Tasks.Task M1() => null;
                    System.Collections.Generic.List<int> M2() => null;
                }
                """,
            FixedCode = """
                using System.Threading.Tasks;

                class C
                {
                    Task M1() => null;
                    System.Collections.Generic.List<int> M2() => null;
                }
                """,
            CodeActionIndex = 1,
        }.RunAsync();
    }

    [Fact]
    public Task TestSimplifyOnlyCurrentOccurrence()
    {
        return new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    [||]System.Threading.Tasks.Task M1() => null;
                    System.Threading.Tasks.Task M2() => null;
                }
                """,
            FixedCode = """
                using System.Threading.Tasks;

                class C
                {
                    Task M1() => null;
                    System.Threading.Tasks.Task M2() => null;
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task TestNotOfferedOnBuiltInType()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]int M() => 0;
            }
            """);

    [Fact]
    public Task TestNotOfferedOnGlobalAloneType()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class TopLevel { }
            
            class C
            {
                [||]global::TopLevel M() => null;
            }
            """);

    [Fact]
    public Task TestGlobalQualifiedName_OnGlobal()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]global::System.DateTime M() => default;
            }
            """,
            """
            using System;

            class C
            {
                DateTime M() => default;
            }
            """);

    [Fact]
    public Task TestGlobalQualifiedName_OnSystem()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                global::[||]System.DateTime M() => default;
            }
            """,
            """
            using System;

            class C
            {
                DateTime M() => default;
            }
            """);

    [Fact]
    public Task TestGlobalQualifiedName_OnTypeName()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                global::System.[||]DateTime M() => default;
            }
            """,
            """
            using System;

            class C
            {
                DateTime M() => default;
            }
            """);

    [Fact]
    public Task TestNotOfferedOnMethod_GlobalQualified()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    global::System.Console.[||]WriteLine();
                }
            }
            """);

    [Fact]
    public Task TestNotOfferedOnMethod_Qualified()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                void M()
                {
                    System.Console.[||]WriteLine();
                }
            }
            """);

    [Fact]
    public Task TestNotOfferedOnMethod_Simple()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    Console.[||]WriteLine();
                }
            }
            """);

    [Fact]
    public Task TestNotOfferedOnSimpleConsole()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [||]Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task TestNotOfferedInUsingAliasDirective()
        => VerifyCS.VerifyRefactoringAsync(
            """
            using X = [||]System.Console;

            class C
            {
                void M() { X.WriteLine(); }
            }
            """);

    [Fact]
    public Task TestNestedType_OfferOnOuterType()
        => VerifyCS.VerifyRefactoringAsync(
            """
            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            }

            class C
            {
                [||]NS1.NS2.T1.T2 M() => null;
            }
            """,
            """
            using NS1.NS2;

            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            }

            class C
            {
                T1.T2 M() => null;
            }
            """);

    [Fact]
    public Task TestNestedType_OfferOnOuterType2()
        => VerifyCS.VerifyRefactoringAsync(
            """
            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            
                class C
                {
                    [||]T1.T2 M() => null;
                }
            }
            """,
            """
            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }

                class C
                {
                    T1.T2 M() => null;
                }
            }
            """);

    [Fact]
    public Task TestNestedType_OfferOnOuterType3()
        => VerifyCS.VerifyRefactoringAsync(
            """
            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            
                class C
                {
                    T1.[||]T2 M() => null;
                }
            }
            """,
            """
            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }

                class C
                {
                    T1.T2 M() => null;
                }
            }
            """);

    [Fact]
    public Task TestNestedType_OfferOnNS1()
        => VerifyCS.VerifyRefactoringAsync(
            """
            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            }

            class C
            {
                [||]NS1.NS2.T1.T2 M() => null;
            }
            """,
            """
            using NS1.NS2;

            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            }

            class C
            {
                T1.T2 M() => null;
            }
            """);

    [Fact]
    public Task TestNestedType_OfferOnNS2()
        => VerifyCS.VerifyRefactoringAsync(
            """
            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            }

            class C
            {
                NS1.[||]NS2.T1.T2 M() => null;
            }
            """,
            """
            using NS1.NS2;

            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            }

            class C
            {
                T1.T2 M() => null;
            }
            """);

    [Fact]
    public Task TestNestedType_OfferOnT1()
        => VerifyCS.VerifyRefactoringAsync(
            """
            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            }

            class C
            {
                NS1.NS2.[||]T1.T2 M() => null;
            }
            """,
            """
            using NS1.NS2;

            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            }

            class C
            {
                T1.T2 M() => null;
            }
            """);

    [Fact]
    public Task TestNestedType_NotOfferedOnT2()
        => VerifyCS.VerifyRefactoringAsync(
            """
            namespace NS1.NS2
            {
                class T1
                {
                    public class T2 { }
                }
            }

            class C
            {
                NS1.NS2.T1.[||]T2 M() => null;
            }
            """);

    [Fact]
    public Task TestNestedGeneric_OuterName_SimplifiesBoth()
        => VerifyCS.VerifyRefactoringAsync(
            """
            class C
            {
                [||]System.Collections.Generic.List<System.Collections.Generic.List<int>> M() => null;
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                List<List<int>> M() => null;
            }
            """);

    [Fact]
    public Task TestNestedGeneric_InnerName_SimplifiesOnlyInner()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    System.Collections.Generic.List<[||]System.Collections.Generic.List<int>> M() => null;
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    System.Collections.Generic.List<List<int>> M() => null;
                }
                """,
            CodeActionIndex = 0,
        }.RunAsync();

    [Fact]
    public Task TestNestedGeneric_InnerName_SimplifyAll_SimplifiesBoth()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    System.Collections.Generic.List<[||]System.Collections.Generic.List<int>> M() => null;
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    List<List<int>> M() => null;
                }
                """,
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestNotOnAliasQualifiedName()
        => new VerifyCS.Test
        {
            TestCode = """
                using SysTasks = System.Threading.Tasks;

                class C
                {
                    [||]SysTasks.Task M() => null;
                }
                """,
            FixedCode = """
                using SysTasks = System.Threading.Tasks;
                
                class C
                {
                    SysTasks.Task M() => null;
                }
                """,
        }.RunAsync();
}
