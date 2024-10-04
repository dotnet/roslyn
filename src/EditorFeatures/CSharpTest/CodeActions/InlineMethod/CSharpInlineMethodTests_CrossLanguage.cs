// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineMethod;

[Trait(Traits.Feature, Traits.Features.CodeActionsInlineMethod)]
public class CSharpInlineMethodTests_CrossLanguage : AbstractCSharpCodeActionTest
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
        => new CSharpInlineMethodRefactoringProvider();

    private async Task TestNoActionIsProvided(string initialMarkup)
    {
        var workspace = CreateWorkspaceFromOptions(initialMarkup);
        var (actions, _) = await GetCodeActionsAsync(workspace).ConfigureAwait(false);
        Assert.True(actions.IsEmpty);
    }

    // Because this issue: https://github.com/dotnet/roslyn-sdk/issues/464
    // it is hard to test cross language scenario.
    // After it is resolved then this test should be merged to the other test class
    [Fact]
    public async Task TestCrossLanguageInline()
    {
        var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
    <ProjectReference>VBAssembly</ProjectReference>
    <Document>
        using VBAssembly;
        public class TestClass
        {
            public void Caller()
            {
                var x = new VBClass();
                x.C[||]allee();
            }
        }
    </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""VBAssembly"" CommonReferences=""true"">
    <Document>
        Public Class VBClass
            Private Sub Callee()
            End Sub
        End Class
    </Document>
    </Project>
</Workspace>";
        await TestNoActionIsProvided(input);
    }
}

