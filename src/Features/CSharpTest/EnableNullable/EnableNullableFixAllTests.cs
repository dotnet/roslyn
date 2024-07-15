// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.EnableNullable
{
    public class EnableNullableFixAllTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
            => new EnableNullableCodeRefactoringProvider();

        [Fact]
        public async Task EnableNullable_FixAllInSolution()
        {
            await TestInRegularAndScriptAsync(@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
{|FixAllInSolution:|}#nullable enable

class Example
{
  string? value;
}
        </Document>
        <Document>
class Example2
{
  string value;
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Example3
{
#nullable enable
  string? value;
#nullable restore
}
        </Document>
        <Document>
#nullable disable

class Example4
{
  string value;
}
        </Document>
    </Project>
</Workspace>
", @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>

class Example
{
  string? value;
}
        </Document>
        <Document>
#nullable disable

class Example2
{
  string value;
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
#nullable disable

class Example3
{
#nullable restore
  string? value;
#nullable disable
}
        </Document>
        <Document>
#nullable disable

class Example4
{
  string value;
}
        </Document>
    </Project>
</Workspace>
");
        }

        [Theory]
        [InlineData("FixAllInDocument")]
        [InlineData("FixAllInProject")]
        [InlineData("FixAllInContainingMember")]
        [InlineData("FixAllInContainingType")]
        public async Task EnableNullable_UnsupportedFixAllScopes(string fixAllScope)
        {
            await TestMissingInRegularAndScriptAsync($@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
{{|{fixAllScope}:|}}#nullable enable

class Example
{{
  string? value;
}}
        </Document>
        <Document>
class Example2
{{
  string value;
}}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Example3
{{
#nullable enable
  string? value;
#nullable restore
}}
        </Document>
        <Document>
#nullable disable

class Example4
{{
  string value;
}}
        </Document>
    </Project>
</Workspace>
");
        }
    }
}
