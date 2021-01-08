// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.LanguageServerProtocol
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class LspGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public LspGoToDefinition(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(LspGoToDefinition))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition), Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public void GoToDefinitionWithMultipleLSP()
        {
            SetUpEditor(
@"partial class /*Marker*/ $$PartialClass { }

partial class PartialClass { int i = 0; }");

            VisualStudio.Editor.GoToDefinition("Class1.cs");

            const string programReferencesCaption = "'PartialClass' references";
            VisualStudio.Editor.WaitForActiveWindow(programReferencesCaption);
            var results = VisualStudio.FindReferencesWindow.GetContents(programReferencesCaption);

            var activeWindowCaption = VisualStudio.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: programReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "partial class /*Marker*/ PartialClass { }", actual: reference.Code);
                        Assert.Equal(expected: 0, actual: reference.Line);
                        Assert.Equal(expected: 25, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "partial class PartialClass { int i = 0; }", actual: reference.Code);
                        Assert.Equal(expected: 2, actual: reference.Line);
                        Assert.Equal(expected: 14, actual: reference.Column);
                    }
                });
        }
    }
}
