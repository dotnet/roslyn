// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
@"
#define NET461

using System;

namespace ClassLibrary
{
    public class Class1
    {
        public static string GetText()
        {
#if NET461
        return ""Hello from "" + Environment.OSVersion;
#elif NETSTANDARD1_4
        return ""Hello from .NET Standard"";
#else
#error Unknown platform 
#endif
            int [|blah|] = 5;
        }
    }
}",
@"
#define NET461

using System;

namespace ClassLibrary
{
    public class Class1
    {
        public static string GetText()
        {
#if NET461
        return ""Hello from "" + Environment.OSVersion;
#elif NETSTANDARD1_4
        return ""Hello from .NET Standard"";
#else
#error Unknown platform 
#endif
        }
    }
}", ignoreTrivia: false);
        }

        [WorkItem(20942, "https://github.com/dotnet/roslyn/issues/20942")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task TestWhitespaceBetweenStatements1()
        {
            await TestInRegularAndScriptAsync(
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
}", ignoreTrivia: false);
        }

        [WorkItem(20942, "https://github.com/dotnet/roslyn/issues/20942")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)]
        public async Task TestWhitespaceBetweenStatements2()
        {
            await TestInRegularAndScriptAsync(
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
}", ignoreTrivia: false);
        }
    }
}
