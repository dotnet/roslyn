// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeLocalFunctionStatic
{
    public partial class PassVariableExplicitlyInLocalStaticFunctionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new PassVariableExplicitlyInLocalStaticFunctionCodeFixProvider());

        private static ParseOptions CSharp72ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2);
        private static ParseOptions CSharp8ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestStandard()//TestAboveCSharp8()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
    }

    int N(int x){

        var y = AddLocal();

        static int AddLocal()
            {
                return [|x|] += 1;
            }
        }  
}",

@"using System;

class C
{
    void M()
    {
    }

    int N(int x){

        var y = AddLocal(x);

        static int AddLocal(int x)
            {
                return x += 1;
            }
        }  
}",
parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestVariableAlreadyDefinedInStaticLocalFunction()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void M()
    {
    }

    int N(int x){
        x = AddLocal();

        static int AddLocal()
            {
                int x = 1;
                return [|x|] += 1;
            }
        }
    }
}", parameters: new TestParameters(parseOptions: CSharp72ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMultipleVariables1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
    }

    int N(int x, int y){

        var z = AddLocal();

        static int AddLocal()
            {
                return [|x|] += y;
            }
        }
}",
@"using System;

class C
{
    void M()
    {
    }

    int N(int x, int y){

        var z = AddLocal(x, y);

        static int AddLocal(int x, int y)
            {
                return x += y;
            }
        } 
    }
}", parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMultipleVariables2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
    }

    int N(int x, int y){

        x = AddLocal();

        return x + y;

        static int AddLocal()
            {
                return x += y[||];
            }
        }  
}",
@"using System;

class C
{
    void M()
    {
    }

    int N(int x, int y){   

        x = AddLocal(x,y);

        return x + y;

        static int AddLocal(int x, int y)
            {
                return x += y;
            }
        }  
    }
}", parseOptions: CSharp8ParseOptions);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestMultipleCalls()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
    }

    int N(int x){

        x = AddLocal();
        int x2 = AddLocal();

        return x + x2;

        static int AddLocal()
            {
                return x[||] += 1;
            }
        }  
}",
@"using System;

class C
{
    void M()
    {
    }

    int N(int x){   

        x = AddLocal(x);
        int x2 = AddLocal(x);

        static int AddLocal(int x)
            {
                return x += 1;
            }
        }  
    }
}", parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestParametersAlreadyAddedToCallAndDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
class C
        {
            void M()
            {
            }

            int N(int x)
            {

                x = AddLocal(x);

                static int AddLocal(int x)
                {
                    return x += 1;
                }
            }
        }
    }", new TestParameters(
    parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestOneOfTwoVariablesDefinedinLocalStaticFunction()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
    }

    int N(int x, int y){

        x = AddLocal();

        return x + y;

        static int AddLocal()
            {
                int y = 5;
                return x[||] += y;
            }
        }  
}",
@"using System;

class C
{
    void M()
    {
    }

    int N(int x, int y){   

        x = AddLocal(x,y);

        return x + y;

        static int AddLocal(int x, int y)
            {
                int y = 5;
                return x += y;
            }
        }  
    }
}", parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestOneVariableAlreadyInParametersOfDeclarationAndCall1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
    }

    int N(int x, int y){

        x = AddLocal(int x);

        return x + y;

        static int AddLocal(int x)
            {
                return x += y[||];
            }
        }  
}",
@"using System;

class C
{
    void M()
    {
    }

    int N(int x, int y){   

        x = AddLocal(x,y);

        return x;

        static int AddLocal(int x, int y)
            {
                return x += y;
            }
        }  
    }
}", parseOptions: CSharp8ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestOneVariableAlreadyInParametersOfDeclarationAndCall2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
    }

    int N(int x, int y){

        x = AddLocal(int x);

        return x;

        static int AddLocal(int x)
            {
                return x[||] += y;
            }
        }  
}", new TestParameters(
    parseOptions: CSharp8ParseOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeLocalFunctionStatic)]
        public async Task TestRecursiveCall()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M()
    {
    }

    int N(int x){

        x = AddLocal();
        return x;

        static int AddLocal()
            {
                x[||] = AddLocal();
                return x += 1;
            }
        }  
}",
@"using System;

class C
{
    void M()
    {
    }

    int N(int x){   

        x = AddLocal(x);

        static int AddLocal(int x)
            {
                x = AddLocal(int x);
                return x += 1;
            }
        }  
    }
}", parseOptions: CSharp8ParseOptions);
        }
    }
}

