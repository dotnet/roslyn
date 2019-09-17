// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeLocalFunctionStatic
{
    public class PassInCapturedVariablesAsArgumentsCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new PassInCapturedVariablesAsArgumentsCodeFixProvider());

        private static ParseOptions CSharp72ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2);
        private static ParseOptions CSharp8ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestMissingInCSharp7()
        {
            await TestMissingAsync(
@"class C
{
    int N(int x)
    {
        return AddLocal();

        static int AddLocal()
        {
            return [||]x + 1;
        }        
    }
}", parameters: new TestParameters(parseOptions: CSharp72ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestMissingIfNoDiagnostic()
        {
            await TestMissingAsync(
@"class C
{
    int N(int x)
    {
        return AddLocal();

        int AddLocal()
        {
            return [||]x + 1;
        }        
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestMissingIfCapturesThisParameter()
        {
            await TestMissingAsync(
@"class C
{
    int y = 0;

    int N(int x)
    {
        return AddLocal();

        static int AddLocal()
        {
            return [||]x + y;
        }        
    }
}", parameters: new TestParameters(parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task ShouldTriggerForCSharp8()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int N(int x)
    {
        return AddLocal();

        static int AddLocal()
        {
            return [||]x + 1;
        }
    }  
}",
@"class C
{
    int N(int x)
    {
        return AddLocal(x);

        static int AddLocal(int x)
        {
            return x + 1;
        }
    }  
}",
parseOptions: CSharp8ParseOptions);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMultipleVariables()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int N(int x)
    {
        int y = 10;
        return AddLocal();

        static int AddLocal()
        {
            return x + [||]y;
        }
    }
}",
@"class C
{
    int N(int x)
    {
        int y = 10;
        return AddLocal(x, y);

        static int AddLocal(int x, int y)
        {
            return x + y;
        }
    }
}", parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMultipleCalls()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int N(int x)
    {
        int y = 10;
        return AddLocal() + AddLocal();

        static int AddLocal()
        {
            return [||]x + y;
        }
    }
}",
@"class C
{
    int N(int x)
    {
        int y = 10;
        return AddLocal(x, y) + AddLocal(x, y);

        static int AddLocal(int x, int y)
        {
            return x + y;
        }
    }
}"
, parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMultipleCallsWithExistingParameters()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int N(int x)
    {
        int y = 10;
        var m = AddLocal(1, 2);
        return AddLocal(m, m);

        static int AddLocal(int a, int b)
        {
            return a + b + [||]x + y;
        }
    }
}",
@"class C
{
    int N(int x)
    {
        int y = 10;
        var m = AddLocal(1, 2, x, y);
        return AddLocal(m, m, x, y);

        static int AddLocal(int a, int b, int x, int y)
        {
            return a + b + x + y;
        }
    }
}"
, parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestRecursiveCall()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int N(int x)
    {
        int y = 10;
        var m = AddLocal(1, 2);
        return AddLocal(m, m);

        static int AddLocal(int a, int b)
        {
            return AddLocal(a, b) + [||]x + y;
        }
    }
}",
@"class C
{
    int N(int x)
    {
        int y = 10;
        var m = AddLocal(1, 2, x, y);
        return AddLocal(m, m, x, y);

        static int AddLocal(int a, int b, int x, int y)
        {
            return AddLocal(a, b, x, y) + x + y;
        }
    }
}", parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestCallInArgumentList()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int N(int x)
    {
        int y = 10;
        return AddLocal(AddLocal(1, 2), AddLocal(3, 4));

        static int AddLocal(int a, int b)
        {
            return AddLocal(a, b) + [||]x + y;
        }
    }
}",
@"class C
{
    int N(int x)
    {
        int y = 10;
        return AddLocal(AddLocal(1, 2, x, y), AddLocal(3, 4, x, y), x, y);

        static int AddLocal(int a, int b, int x, int y)
        {
            return AddLocal(a, b, x, y) + x + y;
        }
    }
}", parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestCallsWithNamedArguments()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int N(int x)
    {
        int y = 10;
        var m = AddLocal(1, b: 2);
        return AddLocal(b: m, a: m);

        static int AddLocal(int a, int b)
        {
            return a + b + [||]x + y;
        }
    }
}",
@"class C
{
    int N(int x)
    {
        int y = 10;
        var m = AddLocal(1, b: 2, x: x, y: y);
        return AddLocal(b: m, a: m, x: x, y: y);

        static int AddLocal(int a, int b, int x, int y)
        {
            return a + b + x + y;
        }
    }
}"
, parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestCallsWithDafaultValue()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int N(int x)
    {
        string y = "";
        var m = AddLocal(1);
        return AddLocal(b: m);

        static int AddLocal(int a = 0, int b = 0)
        {
            return a + b + x + [||]y.Length;
        }
    }
}",
@"class C
{
    int N(int x)
    {
        string y = "";
        var m = AddLocal(1, x: x, y: y);
        return AddLocal(b: m, x: x, y: y);

        static int AddLocal(int a = 0, int b = 0, int x = 0, string y = null)
        {
            return a + b + x + y.Length;
        }
    }
}"
, parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestWarningAnnotation()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void N(int x)
    {
        Func<int> del = AddLocal;

        static int AddLocal()
        {
            return [||]x + 1;
        }
    }  
}",
@"class C
{
    void N(int x)
    {
        Func<int> del = AddLocal;

        {|Warning:static int AddLocal(int x)
        {
            return x + 1;
        }|}
    }  
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestNonCamelCaseCapture()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int N(int x)
    {
        int Static = 0;
        return AddLocal();

        static int AddLocal()
        {
            return [||]Static + 1;
        }
    }  
}",
@"class C
{
    int N(int x)
    {
        int Static = 0;
        return AddLocal(Static);

        static int AddLocal(int @static)
        {
            return @static + 1;
        }
    }  
}",
parseOptions: CSharp8ParseOptions);
        }
    }
}

