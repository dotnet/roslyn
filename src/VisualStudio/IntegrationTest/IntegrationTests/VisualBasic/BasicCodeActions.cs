// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicCodeActions : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicCodeActions(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicCodeActions))
        {
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/20371"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void GenerateMethodInClosedFile()
        {
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Goo.vb", @"
Class Goo
End Class
");

            SetUpEditor(@"
Imports System;

Class Program
    Sub Main(args As String())
        Dim f as Goo = new Goo()
        f.Bar()$$
    End Sub
End Class
");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Generate method 'Goo.Bar'", applyFix: true);
            VisualStudio.SolutionExplorer.Verify.FileContents(project, "Goo.vb", @"
Class Goo
    Friend Sub Bar()
        Throw New NotImplementedException()
    End Sub
End Class
");
        }
    }
}
