// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedVariable
{
    public partial class RemoveUnusedVariableTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpRemoveUnusedVariableCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariable()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        [|int a = 3;|]
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariable1()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        [|string a;|]
        string b = "";
        var c = b;
    }
}",
@"class Class
{
    void Method()
    {
        string b = "";
        var c = b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariable3()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        [|string a;|]
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariableMultipleOnLine()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        [|string a|], b;
    }
}",
@"class Class
{
    void Method()
    {
        string b;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariableMultipleOnLine1()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        string a, [|b|];
    }
}",
@"class Class
{
    void Method()
    {
        string a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariableFixAll()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        {|FixAllInDocument:string a;|}
        string b;
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariableFixAll1()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        {|FixAllInDocument:string a;|}
        string b, c;
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedVariableFixAll2()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
{
    void Method()
    {
        string a, {|FixAllInDocument:b|};
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }

        [WorkItem(20466, "https://github.com/dotnet/roslyn/issues/20466")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedCatchVariable()
        {
            await TestInRegular73AndScriptAsync(
@"class Class
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
}",
@"class Class
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
}");
        }

        [WorkItem(20987, "https://github.com/dotnet/roslyn/issues/20987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task LeadingDirectives()
        {
            await TestInRegular73AndScriptAsync(
@"
#define DIRECTIVE1

using System;

namespace ClassLibrary
{
    public class Class1
    {
        public static string GetText()
        {
#if DIRECTIVE1
        return ""Hello from "" + Environment.OSVersion;
#elif DIRECTIVE2
        return ""Hello from .NET Standard"";
#else
#error Unknown platform 
#endif
            int [|blah|] = 5;
        }
    }
}",
@"
#define DIRECTIVE1

using System;

namespace ClassLibrary
{
    public class Class1
    {
        public static string GetText()
        {
#if DIRECTIVE1
        return ""Hello from "" + Environment.OSVersion;
#elif DIRECTIVE2
        return ""Hello from .NET Standard"";
#else
#error Unknown platform 
#endif
        }
    }
}");
        }

        [WorkItem(20942, "https://github.com/dotnet/roslyn/issues/20942")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task TestWhitespaceBetweenStatements1()
        {
            await TestInRegular73AndScriptAsync(
@"
class Test
{
    bool TrySomething()
    {
        bool used = true;
        int [|unused|];

        return used;
    }
}",
@"
class Test
{
    bool TrySomething()
    {
        bool used = true;

        return used;
    }
}");
        }

        [WorkItem(20942, "https://github.com/dotnet/roslyn/issues/20942")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task TestWhitespaceBetweenStatements2()
        {
            await TestInRegular73AndScriptAsync(
@"
class Test
{
    bool TrySomething()
    {
        int [|unused|];

        return used;
    }
}",
@"
class Test
{
    bool TrySomething()
    {
        return used;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task TestWhitespaceBetweenStatementsInSwitchSection1()
        {
            await TestInRegular73AndScriptAsync(
@"
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
}",
@"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task TestWhitespaceBetweenStatementsInSwitchSection2()
        {
            await TestInRegular73AndScriptAsync(
@"
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
}",
@"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveVariableAndComment()
        {
            await TestInRegular73AndScriptAsync(
@"
class C
{
    void M()
    {
        int [|unused|] = 0; // remove also comment
    }
}
",
@"
class C
{
    void M()
    {
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveVariableAndAssgnment()
        {
            await TestInRegular73AndScriptAsync(
@"
class C
{
    void M()
    {
        int [|b|] = 0;
        b = 0;
    }
}
",
@"
class C
{
    void M()
    {
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task JointDeclarationRemoveFirst()
        {
            await TestInRegular73AndScriptAsync(
@"
class C
{
    int M()
    {
        int [|unused|] = 0, used = 0;
        return used;
    }
}
",
@"
class C
{
    int M()
    {
        int used = 0;
        return used;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task JointDeclarationRemoveSecond()
        {
            await TestInRegular73AndScriptAsync(
@"
class C
{
    int M()
    {
        int used = 0, [|unused|] = 0;
        return used;
    }
}
",
@"
class C
{
    int M()
    {
        int used = 0;
        return used;
    }
}
");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/23322"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task JointAssignmentRemoveFirst()
        {
            await TestInRegular73AndScriptAsync(
@"
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
",
@"
class C
{
    int M()
    {
        int used = 0;
        used = 0;
        return used;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task JointAssignmentRemoveSecond()
        {
            await TestInRegular73AndScriptAsync(
@"
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
",
@"
class C
{
    int M()
    {
        int used = 0;
        used = 0;
        return used;
    }
}
");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/22921"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task RemoveUnusedLambda()
        {
            await TestInRegular73AndScriptAsync(
@"
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
",
@"
class C
{
    int M()
    {
        return 1;
    }
}
");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task JointDeclarationRemoveBoth()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
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
";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
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
";

            await TestInRegular73AndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task JointAssignment()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
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
";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
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
";

            await TestInRegular73AndScriptAsync(input, expected);
        }
    }
}
