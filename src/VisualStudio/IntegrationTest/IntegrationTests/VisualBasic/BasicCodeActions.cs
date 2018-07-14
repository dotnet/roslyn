// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicCodeActions : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicCodeActions()
            : base(nameof(BasicCodeActions))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task GenerateMethodInClosedFileAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "Goo.vb", @"
Class Goo
End Class
");

            await SetUpEditorAsync(@"
Imports System;

Class Program
    Sub Main(args As String())
        Dim f as Goo = new Goo()
        f.Bar()$$
    End Sub
End Class
");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync("Generate method 'Goo.Bar'", applyFix: true, cancellationToken: HangMitigatingCancellationToken);
            VisualStudio.SolutionExplorer.Verify.FileContents(ProjectName, "Goo.vb", @"
Class Goo
    Friend Sub Bar()
        Throw New NotImplementedException()
    End Sub
End Class
");
        }
    }
}
