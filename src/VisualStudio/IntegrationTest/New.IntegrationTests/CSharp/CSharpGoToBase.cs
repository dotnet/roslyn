// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.GoToBase)]
    public class CSharpGoToBase : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToBase()
            : base(nameof(CSharpGoToBase))
        {
        }

        [IdeFact]
        public async Task GoToBaseFromMetadataAsSource()
        {
            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "C.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "C.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"using System;

class C
{
    public override string ToString()
    {
        return ""C"";
    }
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("ToString", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToBaseAsync(HangMitigatingCancellationToken);
            Assert.Equal("Object [decompiled] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            await TestServices.EditorVerifier.TextContainsAsync(@"public virtual string ToString$$()", assertCaretPosition: true);
        }
    }
}
