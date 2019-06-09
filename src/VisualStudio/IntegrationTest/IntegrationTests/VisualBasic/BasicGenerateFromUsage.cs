// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGenerateFromUsage : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGenerateFromUsage(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(BasicGenerateFromUsage))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateLocal)]
        public void GenerateLocal()
        {
            SetUpEditor(
@"Module Program
    Sub Main(args As String())
        Dim x As String = $$xyz
    End Sub
End Module");
            VisualStudio.Editor.Verify.CodeAction("Generate local 'xyz'", applyFix: true);
            VisualStudio.Editor.Verify.TextContains(
@"Module Program
    Sub Main(args As String())
        Dim xyz As String = Nothing
        Dim x As String = xyz
    End Sub
End Module");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public void GenerateTypeInNewFile()
        {
            SetUpEditor(
@"Module Program
    Sub Main(args As String())
        Dim x As New $$ClassInNewFile()
    End Sub
End Module");
            VisualStudio.Editor.Verify.CodeAction("Generate class 'ClassInNewFile' in new file", applyFix: true);
            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "ClassInNewFile.vb");
            VisualStudio.Editor.Verify.TextContains(
@"Friend Class ClassInNewFile
    Public Sub New()
    End Sub
End Class");
        }
    }
}
