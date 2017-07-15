// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20371"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void GenerateMethodInClosedFile()
        {
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Foo.vb", @"
Class Foo
End Class
");

            SetUpEditor(@"
Imports System;

Class Program
    Sub Main(args As String())
        Dim f as Foo = new Foo()
        f.Bar()$$
    End Sub
End Class
");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Generate method 'Foo.Bar'", applyFix: true);
            VisualStudio.SolutionExplorer.Verify.FileContents(project, "Foo.vb", @"
Class Foo
    Friend Sub Bar()
        Throw New NotImplementedException()
    End Sub
End Class
");
        }
    }
}
