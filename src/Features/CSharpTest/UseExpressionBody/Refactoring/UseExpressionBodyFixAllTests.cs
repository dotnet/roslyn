// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
    public class UseExpressionBodyFixAllTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
            => new UseExpressionBodyCodeRefactoringProvider();

        private OptionsCollection UseBlockBody
            => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

        private OptionsCollection UseBlockBodyForMethodsAndAccessorsAndProperties
            => new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            };

        [Fact]
        public async Task FixAllInDocument()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M1()
    {
        {|FixAllInDocument:|}Bar();
    }

    void M2()
    {
        Bar();
    }
}",
@"class C
{
    void M1() => Bar();

    void M2() => Bar();
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact]
        public async Task FixAllInProject()
        {
            await TestInRegularAndScript1Async(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class C
{
    void M1()
    {
        {|FixAllInProject:|}Bar();
    }

    void M2()
    {
        Bar();
    }
}
        </Document>
        <Document>
class C2
{
    void M3()
    {
        Bar();
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class C3
{
    void M4()
    {
        Bar();
    }
}
        </Document>
    </Project>
</Workspace>",
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class C
{
    void M1() => Bar();

    void M2() => Bar();
}
        </Document>
        <Document>
class C2
{
    void M3() => Bar();
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class C3
{
    void M4()
    {
        Bar();
    }
}
        </Document>
    </Project>
</Workspace>", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact]
        public async Task FixAllInSolution()
        {
            await TestInRegularAndScript1Async(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class C
{
    void M1()
    {
        {|FixAllInSolution:|}Bar();
    }

    void M2()
    {
        Bar();
    }
}
        </Document>
        <Document>
class C2
{
    void M3()
    {
        Bar();
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class C3
{
    void M4()
    {
        Bar();
    }
}
        </Document>
    </Project>
</Workspace>",
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class C
{
    void M1() => Bar();

    void M2() => Bar();
}
        </Document>
        <Document>
class C2
{
    void M3() => Bar();
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class C3
{
    void M4() => Bar();
}
        </Document>
    </Project>
</Workspace>", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact]
        public async Task FixAllInContainingMember()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M1()
    {
        {|FixAllInContainingMember:|}Bar();
    }

    void M2()
    {
        Bar();
    }
}

class C2
{
    void M3()
    {
        Bar();
    }
}",
@"class C
{
    void M1() => Bar();

    void M2()
    {
        Bar();
    }
}

class C2
{
    void M3()
    {
        Bar();
    }
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Fact]
        public async Task FixAllInContainingType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M1()
    {
        {|FixAllInContainingType:|}Bar();
    }

    void M2()
    {
        Bar();
    }
}

class C2
{
    void M3()
    {
        Bar();
    }
}",
@"class C
{
    void M1() => Bar();

    void M2() => Bar();
}

class C2
{
    void M3()
    {
        Bar();
    }
}", parameters: new TestParameters(options: UseBlockBody));
        }

        [Theory]
        [CombinatorialData]
        public async Task FixAllDoesNotFixDifferentSymbolKinds(bool forMethods)
        {
            var fixAllAnnotationForMethods = forMethods ? "{|FixAllInDocument:|}" : string.Empty;
            var fixAllAnnotationForProperties = forMethods ? string.Empty : "{|FixAllInDocument:|}";

            var source = @$"class C
{{
    void M1()
    {{
        {fixAllAnnotationForMethods}Bar();
    }}

    void M2()
    {{
        Bar();
    }}

    int P1
    {{
        get
        {{
            {fixAllAnnotationForProperties}return 0;
        }}
    }}

    int P2
    {{
        get
        {{
            return 0;
        }}
    }}
}}";
            var fixedCodeForMethods = @"class C
{
    void M1() => Bar();

    void M2() => Bar();

    int P1
    {
        get
        {
            return 0;
        }
    }

    int P2
    {
        get
        {
            return 0;
        }
    }
}";
            var fixedCodeForProperties = @"class C
{
    void M1()
    {
        Bar();
    }

    void M2()
    {
        Bar();
    }

    int P1 => 0;

    int P2 => 0;
}";
            var fixedCode = forMethods ? fixedCodeForMethods : fixedCodeForProperties;

            await TestInRegularAndScript1Async(source, fixedCode,
                parameters: new TestParameters(options: UseBlockBodyForMethodsAndAccessorsAndProperties));
        }
    }
}
