// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGenerateFromUsage : AbstractIdeEditorTest
    {
        public BasicGenerateFromUsage()
            : base(nameof(BasicGenerateFromUsage))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateLocal)]
        public async Task GenerateLocalAsync()
        {
            await SetUpEditorAsync(
@"Module Program
    Sub Main(args As String())
        Dim x As String = $$xyz
    End Sub
End Module");
            await VisualStudio.Editor.Verify.CodeActionAsync("Generate local 'xyz'", applyFix: true);
            await VisualStudio.Editor.Verify.TextContainsAsync(
@"Module Program
    Sub Main(args As String())
        Dim xyz As String = Nothing
        Dim x As String = xyz
    End Sub
End Module");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInNewFileAsync()
        {
            await SetUpEditorAsync(
@"Module Program
    Sub Main(args As String())
        Dim x As New $$ClassInNewFile()
    End Sub
End Module");
            await VisualStudio.Editor.Verify.CodeActionAsync("Generate class 'ClassInNewFile' in new file", applyFix: true);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "ClassInNewFile.vb");
            await VisualStudio.Editor.Verify.TextContainsAsync(
@"Friend Class ClassInNewFile
    Public Sub New()
    End Sub
End Class");
        }
    }
}
