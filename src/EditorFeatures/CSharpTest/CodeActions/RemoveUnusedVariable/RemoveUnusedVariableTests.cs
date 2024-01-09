// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedVariable
{
    public partial class RemoveUnusedVariableTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public RemoveUnusedVariableTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpRemoveUnusedVariableCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariable()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        [|int a = 3;|]
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariable1()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        [|string a;|]
                        string b = ";
                        var c = b;
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                        string b = ";
                        var c = b;
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariable3()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        [|string a;|]
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariableMultipleOnLine()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        [|string a|], b;
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                        string b;
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariableMultipleOnLine1()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        string a, [|b|];
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                        string a;
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariableFixAll()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        {|FixAllInDocument:string a;|}
                        string b;
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariableFixAll1()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        {|FixAllInDocument:string a;|}
                        string b, c;
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariableFixAll2()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        string a, {|FixAllInDocument:b|};
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/20466")]
        public async Task RemoveUnusedCatchVariable()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        try
                        {
                        }
                        catch (System.Exception [|e|])
                        {
                        }
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                        try
                        {
                        }
                        catch (System.Exception)
                        {
                        }
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/20987")]
        public async Task LeadingDirectives()
        {
            await TestInRegularAndScriptAsync(
                """
                #define DIRECTIVE1

                using System;

                namespace ClassLibrary
                {
                    public class Class1
                    {
                        public static string GetText()
                        {
                #if DIRECTIVE1
                        return "Hello from " + Environment.OSVersion;
                #elif DIRECTIVE2
                        return "Hello from .NET Standard";
                #else
                #error Unknown platform 
                #endif
                            int [|blah|] = 5;
                        }
                    }
                }
                """,
                """
                #define DIRECTIVE1

                using System;

                namespace ClassLibrary
                {
                    public class Class1
                    {
                        public static string GetText()
                        {
                #if DIRECTIVE1
                        return "Hello from " + Environment.OSVersion;
                #elif DIRECTIVE2
                        return "Hello from .NET Standard";
                #else
                #error Unknown platform 
                #endif
                        }
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/20942")]
        public async Task TestWhitespaceBetweenStatements1()
        {
            await TestInRegularAndScriptAsync(
                """
                class Test
                {
                    bool TrySomething()
                    {
                        bool used = true;
                        int [|unused|];

                        return used;
                    }
                }
                """,
                """
                class Test
                {
                    bool TrySomething()
                    {
                        bool used = true;

                        return used;
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/20942")]
        public async Task TestWhitespaceBetweenStatements2()
        {
            await TestInRegularAndScriptAsync(
                """
                class Test
                {
                    bool TrySomething()
                    {
                        int [|unused|];

                        return used;
                    }
                }
                """,
                """
                class Test
                {
                    bool TrySomething()
                    {
                        return used;
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task TestWhitespaceBetweenStatementsInSwitchSection1()
        {
            await TestInRegularAndScriptAsync(
                """
                class Test
                {
                    bool TrySomething()
                    {
                        switch (true)
                        {
                            case true:
                                bool used = true;
                                int [|unused|];

                                return used;
                        }
                    }
                }
                """,
                """
                class Test
                {
                    bool TrySomething()
                    {
                        switch (true)
                        {
                            case true:
                                bool used = true;

                                return used;
                        }
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task TestWhitespaceBetweenStatementsInSwitchSection2()
        {
            await TestInRegularAndScriptAsync(
                """
                class Test
                {
                    bool TrySomething()
                    {
                        switch (true)
                        {
                            case true:
                                int [|unused|];

                                return used;
                        }
                    }
                }
                """,
                """
                class Test
                {
                    bool TrySomething()
                    {
                        switch (true)
                        {
                            case true:
                                return used;
                        }
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveVariableAndComment()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        int [|unused|] = 0; // remove also comment
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveVariableAndAssgnment()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        int [|b|] = 0;
                        b = 0;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task JointDeclarationRemoveFirst()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int [|unused|] = 0, used = 0;
                        return used;
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        int used = 0;
                        return used;
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task JointDeclarationRemoveSecond()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int used = 0, [|unused|] = 0;
                        return used;
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        int used = 0;
                        return used;
                    }
                }
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/23322"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task JointAssignmentRemoveFirst()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int [|unused|] = 0;
                        int used = 0;
                        unused = used = 0;
                        return used;
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        int used = 0;
                        used = 0;
                        return used;
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task JointAssignmentRemoveSecond()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int used = 0;
                        int [|unused|] = 0;
                        used = unused = 0;
                        return used;
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        int used = 0;
                        used = 0;
                        return used;
                    }
                }
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/22921"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedLambda()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        Func<int> [|unused|] = () =>
                        {
                            return 0;
                        };
                        return 1;
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        return 1;
                    }
                }
                """);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task JointDeclarationRemoveBoth()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>
                class C
                {
                    int M()
                    {
                        int {|FixAllInDocument:a|} = 0, b = 0;
                        return 0;
                    }
                }
                        </Document>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>
                class C
                {
                    int M()
                    {
                        return 0;
                    }
                }
                        </Document>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task JointAssignment()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>
                class C
                {
                    int M()
                    {
                        int a = 0;
                        int {|FixAllInDocument:b|} = 0;
                        a = b = 0;
                        return 0;
                    }
                }
                        </Document>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>
                class C
                {
                    int M()
                    {
                        int a = 0;
                        a = 0;
                        return 0;
                    }
                }
                        </Document>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/40336")]
        public async Task RemoveUnusedVariableDeclaredInForStatement()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        for([|int i = 0|]; ; )
                        {

                        }
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                        for(; ; )
                        {

                        }
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/40336")]
        public async Task RemoveUnusedVariableJointDeclaredInForStatement()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        for(int i = 0[|, j = 0|]; i < 1; i++)
                        {

                        }
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                        for(int i = 0; i < 1; i++)
                        {

                        }
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/44273")]
        public async Task TopLevelStatement()
        {
            await TestAsync("""
                [|int i = 0|];
                """,
                """

                """, TestOptions.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/49827")]
        public async Task RemoveUnusedVariableJointDeclaredInForStatementInsideIfStatement()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method()
                    {
                        if (true)
                            for(int i = 0[|, j = 0|]; i < 1; i++)
                            {

                            }
                    }
                }
                """,
                """
                class Class
                {
                    void Method()
                    {
                        if (true)
                            for(int i = 0; i < 1; i++)
                            {

                            }
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/49827")]
        public async Task DoNotCrashOnDeclarationInsideIfStatement()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method(bool test)
                    {
                        if (test [|and test|])
                        {

                        }
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/56924")]
        public async Task RemoveUnusedVariableInCatchInsideBadLocalDeclaration()
        {
            await TestInRegularAndScriptAsync(
                """
                class Class
                {
                    void Method(bool test)
                    {
                        if (test) var x = () => {
                            try { }
                            catch (Exception [|ex|]) { }
                        };
                    }
                }
                """,
                """
                class Class
                {
                    void Method(bool test)
                    {
                        if (test) var x = () => {
                            try { }
                            catch (Exception) { }
                        };
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/51737")]
        public async Task RemoveUnusedVariableTopLevel()
        {
            await TestAsync(
                """
                [|int i = 1|];
                i = 2;
                """,
                """

                """, CSharpParseOptions.Default);
        }
    }
}
