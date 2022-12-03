// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertNamespace;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertNamespace
{
    public class ConvertNamespaceRefactoringFixAllTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new ConvertNamespaceCodeRefactoringProvider();

        private OptionsCollection PreferBlockScopedNamespace
            => this.Option(CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Warning);

        private OptionsCollection PreferFileScopedNamespace
            => this.Option(CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped, NotificationOption2.Warning);

        [Fact]
        public async Task TestConvertToFileScope_FixAllInProject()
        {
            await TestInRegularAndScript1Async(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace {|FixAllInProject:|}N1
{
}
        </Document>
        <Document>
namespace N2
{
    class C { }
}
        </Document>
        <Document>
namespace N3.N4
{
    class C2 { }
}
        </Document>
        <Document>
namespace N5;
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace N6
{
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace N1;
        </Document>
        <Document>
namespace N2;

class C { }
    </Document>
        <Document>
namespace N3.N4;

class C2 { }
    </Document>
        <Document>
namespace N5;
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace N6
{
}
        </Document>
    </Project>
</Workspace>
", new TestParameters(options: PreferBlockScopedNamespace));
        }

        [Fact]
        public async Task TestConvertToFileScope_FixAllInSolution()
        {
            await TestInRegularAndScript1Async(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace {|FixAllInSolution:|}N1
{
}
        </Document>
        <Document>
namespace N2
{
    class C { }
}
        </Document>
        <Document>
namespace N3.N4
{
    class C2 { }
}
        </Document>
        <Document>
namespace N5;
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace N6
{
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace N1;
        </Document>
        <Document>
namespace N2;

class C { }
    </Document>
        <Document>
namespace N3.N4;

class C2 { }
    </Document>
        <Document>
namespace N5;
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace N6;
        </Document>
    </Project>
</Workspace>
", new TestParameters(options: PreferBlockScopedNamespace));
        }

        [Theory]
        [InlineData("FixAllInDocument")]
        [InlineData("FixAllInContainingType")]
        [InlineData("FixAllInContainingMember")]
        public async Task TestConvertToFileScope_UnsupportedFixAllScopes(string fixAllScope)
        {
            await TestMissingInRegularAndScriptAsync($@"
namespace {{|{fixAllScope}:|}}N1
{{
}}", new TestParameters(options: PreferBlockScopedNamespace));
        }

        [Fact]
        public async Task TestConvertToBlockScope_FixAllInProject()
        {
            await TestInRegularAndScript1Async(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace {|FixAllInProject:|}N1;
        </Document>
        <Document>
namespace N2;

class C { }
        </Document>
        <Document>
namespace N3.N4;

class C2 { }
        </Document>
        <Document>
namespace N5
{
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace N6;
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace N1
{
}</Document>
        <Document>
namespace N2
{
    class C { }
}</Document>
        <Document>
namespace N3.N4
{
    class C2 { }
}</Document>
        <Document>
namespace N5
{
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace N6;
        </Document>
    </Project>
</Workspace>
", new TestParameters(options: PreferFileScopedNamespace));
        }

        [Fact]
        public async Task TestConvertToBlockScope_FixAllInSolution()
        {
            await TestInRegularAndScript1Async(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace {|FixAllInSolution:|}N1;
        </Document>
        <Document>
namespace N2;

class C { }
        </Document>
        <Document>
namespace N3.N4;

class C2 { }
        </Document>
        <Document>
namespace N5
{
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace N6;
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace N1
{
}</Document>
        <Document>
namespace N2
{
    class C { }
}</Document>
        <Document>
namespace N3.N4
{
    class C2 { }
}</Document>
        <Document>
namespace N5
{
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace N6
{
}</Document>
    </Project>
</Workspace>
", new TestParameters(options: PreferFileScopedNamespace));
        }

        [Theory]
        [InlineData("FixAllInDocument")]
        [InlineData("FixAllInContainingType")]
        [InlineData("FixAllInContainingMember")]
        public async Task TestConvertToBlockScope_UnsupportedFixAllScopes(string fixAllScope)
        {
            await TestMissingInRegularAndScriptAsync($@"
namespace {{|{fixAllScope}:|}}N1;
", new TestParameters(options: PreferFileScopedNamespace));
        }
    }
}
