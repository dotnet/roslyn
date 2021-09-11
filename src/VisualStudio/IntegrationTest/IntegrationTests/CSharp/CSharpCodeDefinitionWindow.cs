// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpCodeDefinitionWindow : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpCodeDefinitionWindow(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpCodeDefinitionWindow))
        {
        }

        [WpfFact]
        public void CodeDefinitionWindowOpensMetadataAsSource()
        {
            VisualStudio.CodeDefinitionWindow.Show();

            // Opening the code definition window sets focus to the code definition window, but we want to go back to editing
            // our regular file.
            VisualStudio.Editor.Activate();

            SetUpEditor(@"
public class Test
{
    $$int field;
}
");

            Assert.Contains("public struct Int32", VisualStudio.CodeDefinitionWindow.GetCurrentLineText());
        }
    }
}
