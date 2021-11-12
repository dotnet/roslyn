// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    public class BasicNavigationBar : AbstractEditorTest
    {
        private const string TestSource = @"
Class C
    Public WithEvents Domain As AppDomain
    Public Sub $$Goo()
    End Sub
End Class

Structure S
    Public Property A As Integer
    Public Property B As Integer
End Structure";

        public BasicNavigationBar()
            : base(nameof(BasicNavigationBar))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact]
        [Trait(Traits.Feature, Traits.Features.NavigationBar)]
        public async Task CodeSpit()
        {
            await SetUpEditorAsync(TestSource, HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("C", charsOffset: 1, HangMitigatingCancellationToken);
            await VerifyLeftSelectedAsync("C", HangMitigatingCancellationToken);
            await TestServices.Editor.ExpandMemberNavigationBarAsync(HangMitigatingCancellationToken);
            Assert.Equal(new[] { "New", "Finalize", "Goo" }, await TestServices.Editor.GetMemberNavigationBarItemsAsync(HangMitigatingCancellationToken));
            await TestServices.Editor.SelectMemberNavigationBarItemAsync("New", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"
    Public Sub New()

    End Sub", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CaretPositionAsync(78, HangMitigatingCancellationToken); // Caret is between New() and End Sub() in virtual whitespace
            await TestServices.EditorVerifier.CurrentLineTextAsync("$$", assertCaretPosition: true, cancellationToken: HangMitigatingCancellationToken);
        }

        private async Task VerifyLeftSelectedAsync(string expected, CancellationToken cancellationToken)
        {
            Assert.Equal(expected, await TestServices.Editor.GetTypeNavigationBarSelectionAsync(cancellationToken));
        }

        private async Task VerifyRightSelectedAsync(string expected, CancellationToken cancellationToken)
        {
            Assert.Equal(expected, await TestServices.Editor.GetMemberNavigationBarSelectionAsync(cancellationToken));
        }
    }
}
