// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Basic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicCodeDefinitionWindow : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicCodeDefinitionWindow(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicCodeDefinitionWindow))
        {
        }

        [WpfTheory(Skip = "https://github.com/dotnet/roslyn/issues/60364")]
        [CombinatorialData]
        public void CodeDefinitionWindowOpensMetadataAsSource(bool enableDecompilation)
        {
            VisualStudio.Workspace.SetEnableDecompilationOption(enableDecompilation);
            VisualStudio.CodeDefinitionWindow.Show();

            // Opening the code definition window sets focus to the code definition window, but we want to go back to editing
            // our regular file.
            VisualStudio.Editor.Activate();

            SetUpEditor(@"
Public Class Test
    Dim field As $$Integer
End Class
");

            // If we are enabling decompilation, we'll get C# code since we don't support decompiling into VB
            if (enableDecompilation)
                Assert.Contains("public struct Int32", VisualStudio.CodeDefinitionWindow.GetCurrentLineText());
            else
                Assert.Contains("Public Structure Int32", VisualStudio.CodeDefinitionWindow.GetCurrentLineText());
        }
    }
}
