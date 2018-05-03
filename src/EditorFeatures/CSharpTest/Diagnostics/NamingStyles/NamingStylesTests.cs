// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.NamingStyles;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles
{
    public partial class NamingStylesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpNamingStyleDiagnosticAnalyzer(), new NamingStyleCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseClass_CorrectName()
        {
            await TestMissingInRegularAndScriptAsync(
@"class [|C|]
{
}", new TestParameters(options: ClassNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseClass_NameGetsCapitalized()
        {
            await TestInRegularAndScriptAsync(
@"class [|c|]
{
}",
@"class C
{
}",
                options: ClassNamesArePascalCase);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        [InlineData("M_bar", "bar")]
        [InlineData("S_bar", "bar")]
        [InlineData("T_bar", "bar")]
        [InlineData("_Bar", "bar", Skip = "https://github.com/dotnet/roslyn/issues/26588")]
        [InlineData("__Bar", "bar", Skip = "https://github.com/dotnet/roslyn/issues/26588")]
        [InlineData("M_s__t_Bar", "bar")]
        [InlineData("m_bar", "bar", Skip = "https://github.com/dotnet/roslyn/issues/26588")]
        [InlineData("s_bar", "bar", Skip = "https://github.com/dotnet/roslyn/issues/26588")]
        [InlineData("t_bar", "bar", Skip = "https://github.com/dotnet/roslyn/issues/26588")]
        [InlineData("_bar", "bar", Skip = "https://github.com/dotnet/roslyn/issues/26588")]
        [InlineData("__bar", "bar", Skip = "https://github.com/dotnet/roslyn/issues/26588")]
        [InlineData("m_s__t_Bar", "bar", Skip = "https://github.com/dotnet/roslyn/issues/26588")]
        // Special cases to ensure empty identifiers are not produced
        [InlineData("M_", "m_")]
        [InlineData("M__", "_")]
        [InlineData("S_", "s_")]
        [InlineData("T_", "t_")]
        [InlineData("M_S__T_", "t_")]
        public async Task TestCamelCaseField_PrefixGetsStripped(string fieldName, string correctedName)
        {
            await TestInRegularAndScriptAsync(
$@"class C
{{
    int [|{fieldName}|];
}}",
$@"class C
{{
    int [|{correctedName}|];
}}",
                options: FieldNamesAreCamelCase);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        [InlineData("M_bar", "_bar")]
        [InlineData("S_bar", "_bar")]
        [InlineData("T_bar", "_bar")]
        [InlineData("_Bar", "_bar")]
        [InlineData("__Bar", "_bar", Skip = "https://github.com/dotnet/roslyn/issues/26588")]
        [InlineData("M_s__t_Bar", "_bar")]
        [InlineData("m_bar", "_bar")]
        [InlineData("s_bar", "_bar")]
        [InlineData("t_bar", "_bar")]
        [InlineData("bar", "_bar")]
        [InlineData("__bar", "_bar", Skip = "https://github.com/dotnet/roslyn/issues/26588")]
        [InlineData("m_s__t_Bar", "_bar")]
        // Special cases to ensure empty identifiers are not produced
        [InlineData("M_", "_m_")]
        [InlineData("M__", "_")]
        [InlineData("S_", "_s_")]
        [InlineData("T_", "_t_")]
        [InlineData("M_S__T_", "_t_")]
        public async Task TestCamelCaseField_PrefixGetsStrippedBeforeAddition(string fieldName, string correctedName)
        {
            await TestInRegularAndScriptAsync(
$@"class C
{{
    int [|{fieldName}|];
}}",
$@"class C
{{
    int [|{correctedName}|];
}}",
                options: FieldNamesAreCamelCaseWithUnderscore);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_CorrectName()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void [|M|]()
    {
    }
}", new TestParameters(options: MethodNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_NameGetsCapitalized()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void [|m|]()
    {
    }
}",
@"class C
{
    void M()
    {
    }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_ConstructorsAreIgnored()
        {
            await TestMissingInRegularAndScriptAsync(
@"class c
{
    public [|c|]()
    {
    }
}", new TestParameters(options: MethodNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_PropertyAccessorsAreIgnored()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    public int P { [|get|]; set; }
}", new TestParameters(options: MethodNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_IndexerNameIsIgnored()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    public int [|this|][int index]
    {
        get
        {
            return 1;
        }
    }
}", new TestParameters(options: MethodNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestCamelCaseParameters()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public void M(int [|X|])
    {
    }
}",
@"class C
{
    public void M(int x)
    {
    }
}",
                options: ParameterNamesAreCamelCase);
		}
		
        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_InInterfaceWithImplicitImplementation()
        {
            await TestInRegularAndScriptAsync(
@"interface I
{
    void [|m|]();
}

class C : I
{
    public void m() { }
}",
@"interface I
{
    void M();
}

class C : I
{
    public void M() { }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_InInterfaceWithExplicitImplementation()
        {
            await TestInRegularAndScriptAsync(
@"interface I
{
    void [|m|]();
}

class C : I
{
    void I.m() { }
}",
@"interface I
{
    void M();
}

class C : I
{
    void I.M() { }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_NotInImplicitInterfaceImplementation()
        {
            await TestMissingInRegularAndScriptAsync(
@"interface I
{
    void m();
}

class C : I
{
    public void [|m|]() { }
}", new TestParameters(options: MethodNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_NotInExplicitInterfaceImplementation()
        {
            await TestMissingInRegularAndScriptAsync(
@"interface I
{
    void m();
}

class C : I
{
    void I.[|m|]() { }
}", new TestParameters(options: MethodNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_InAbstractType()
        {
            await TestInRegularAndScriptAsync(
@"
abstract class C
{
    public abstract void [|m|]();
}

class D : C
{
    public override void m() { }
}",
@"
abstract class C
{
    public abstract void M();
}

class D : C
{
    public override void M() { }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_NotInAbstractMethodImplementation()
        {
            await TestMissingInRegularAndScriptAsync(
@"
abstract class C
{
    public abstract void m();
}

class D : C
{
    public override void [|m|]() { }
}", new TestParameters(options: MethodNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseProperty_InInterface()
        {
            await TestInRegularAndScriptAsync(
@"
interface I
{
    int [|p|] { get; set; }
}

class C : I
{
    public int p { get { return 1; } set { } }
}",
@"
interface I
{
    int P { get; set; }
}

class C : I
{
    public int P { get { return 1; } set { } }
}",
                options: PropertyNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseProperty_NotInImplicitInterfaceImplementation()
        {
            await TestMissingInRegularAndScriptAsync(
@"
interface I
{
    int p { get; set; }
}

class C : I
{
    public int [|p|] { get { return 1; } set { } }
}", new TestParameters(options: PropertyNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_OverrideInternalMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"
abstract class C
{
    internal abstract void m();
}

class D : C
{
    internal override void [|m|]() { }
}", new TestParameters(options: MethodNamesArePascalCase));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        [WorkItem(19106, "https://github.com/dotnet/roslyn/issues/19106")]
        public async Task TestMissingOnSymbolsWithNoName()
        {
            await TestMissingInRegularAndScriptAsync(
@"
namespace Microsoft.CodeAnalysis.Host
{
    internal interface 
[|}|]
", new TestParameters(options: InterfaceNamesStartWithI));
        }
        
        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        [WorkItem(16562, "https://github.com/dotnet/roslyn/issues/16562")]
        public async Task TestRefactorNotify()
        {
            var markup = @"public class [|c|] { }";
            var testParameters = new TestParameters(options: ClassNamesArePascalCase);

            using (var workspace = CreateWorkspaceFromOptions(markup, testParameters))
            {
                var actions = await GetCodeActionsAsync(workspace, testParameters);

                var previewOperations = await actions[0].GetPreviewOperationsAsync(CancellationToken.None);
                Assert.Empty(previewOperations.OfType<TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation>());

                var commitOperations = await actions[0].GetOperationsAsync(CancellationToken.None);
                Assert.Equal(2, commitOperations.Length);

                var symbolRenamedOperation = (TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation)commitOperations[1];
                Assert.Equal("c", symbolRenamedOperation._symbol.Name);
                Assert.Equal("C", symbolRenamedOperation._newName);
            }
        }
    }
}
