// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.LineCommit)]
    public class BasicLineCommit : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicLineCommit()
            : base(nameof(BasicLineCommit))
        {
        }

        [IdeFact]
        public async Task CaseCorrection()
        {
            await TestServices.Editor.SetTextAsync(@"Module Goo
    Sub M()
Dim x = Sub()
    End Sub
End Module", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("Sub()", charsOffset: 1, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync(VirtualKey.Enter);
            Assert.Equal(48, await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task UndoWithEndConstruct()
        {
            await TestServices.Editor.SetTextAsync(@"Module Module1
    Sub Main()
    End Sub
    REM
End Module", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("    REM", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("sub", VirtualKey.Escape, " goo()", VirtualKey.Enter);
            AssertEx.EqualOrDiff(@"Module Module1
    Sub Main()
    End Sub
    Sub goo()

    End Sub
End Module", await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.Undo, HangMitigatingCancellationToken);
            Assert.Equal(54, await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task UndoWithoutEndConstruct()
        {
            await TestServices.Editor.SetTextAsync(@"Module Module1

    ''' <summary></summary>
    Sub Main()
    End Sub
End Module", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("Module1", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync(VirtualKey.Down, VirtualKey.Enter);
            AssertEx.EqualOrDiff(@"Module Module1


    ''' <summary></summary>
    Sub Main()
    End Sub
End Module", await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
            Assert.Equal(18, await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken));
            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.Undo, HangMitigatingCancellationToken);
            Assert.Equal(16, await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task CommitOnSave()
        {
            await TestServices.Editor.SetTextAsync(@"Module Module1
    Sub Main()
    End Sub
End Module
", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("(", charsOffset: 1, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("x   As   integer", VirtualKey.Tab);

            Assert.False(await TestServices.Editor.IsSavedAsync(HangMitigatingCancellationToken));
            await TestServices.Input.SendAsync(new KeyPress(VirtualKey.S, ShiftState.Ctrl));
            Assert.True(await TestServices.Editor.IsSavedAsync(HangMitigatingCancellationToken));
            AssertEx.EqualOrDiff(@"Module Module1
    Sub Main(x As Integer)
    End Sub
End Module
", await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.Undo, HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(@"Module Module1
    Sub Main(x   As   Integer)
    End Sub
End Module
", await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
            Assert.Equal(45, await TestServices.Editor.GetCaretPositionAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task CommitOnFocusLost()
        {
            await TestServices.Editor.SetTextAsync(@"Module M
    Sub M()
    End Sub
End Module", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("End Sub", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync(" ");
            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "TestZ.vb", open: true, cancellationToken: HangMitigatingCancellationToken); // Cause focus lost
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "TestZ.vb", HangMitigatingCancellationToken); // Work around https://github.com/dotnet/roslyn/issues/18488
            await TestServices.Input.SendAsync("                  ");
            await TestServices.SolutionExplorer.CloseCodeFileAsync(ProjectName, "TestZ.vb", saveFile: false, cancellationToken: HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(@"Module M
    Sub M()
    End Sub
End Module", await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task CommitOnFocusLostDoesNotFormatWithPrettyListingOff()
        {
            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
            globalOptions.SetGlobalOption(new OptionKey(FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic), false);

            await TestServices.Editor.SetTextAsync(@"Module M
    Sub M()
    End Sub
End Module", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("End Sub", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync(" ");
            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "TestZ.vb", open: true, cancellationToken: HangMitigatingCancellationToken); // Cause focus lost
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "TestZ.vb", HangMitigatingCancellationToken); // Work around https://github.com/dotnet/roslyn/issues/18488
            await TestServices.Input.SendAsync("                  ");
            await TestServices.SolutionExplorer.CloseCodeFileAsync(ProjectName, "TestZ.vb", saveFile: false, HangMitigatingCancellationToken);
            AssertEx.EqualOrDiff(@"Module M
    Sub M()
     End Sub
End Module", await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        }
    }
}
