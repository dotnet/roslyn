// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicLineCommit : AbstractIdeEditorTest
    {
        public BasicLineCommit()
            : base(nameof(BasicLineCommit))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        public async Task CaseCorrectionAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"Module Goo
    Sub M()
Dim x = Sub()
    End Sub
End Module");

            await VisualStudio.Editor.PlaceCaretAsync("Sub()", charsOffset: 1);
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Enter);
            await VisualStudio.Editor.Verify.CaretPositionAsync(48);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        public async Task UndoWithEndConstructAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"Module Module1
    Sub Main()
    End Sub
    REM
End Module");

            await VisualStudio.Editor.PlaceCaretAsync("    REM");
            await VisualStudio.Editor.SendKeysAsync("sub", VirtualKey.Escape, " goo()", VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"Sub goo()

    End Sub");
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_Undo);
            await VisualStudio.Editor.Verify.CaretPositionAsync(54);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        public async Task UndoWithoutEndConstructAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"Module Module1

    ''' <summary></summary>
    Sub Main()
    End Sub
End Module");

            await VisualStudio.Editor.PlaceCaretAsync("Module1");
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Down, VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"Module Module1


    ''' <summary></summary>
    Sub Main()
    End Sub
End Module");
            await VisualStudio.Editor.Verify.CaretPositionAsync(18);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_Undo);
            await VisualStudio.Editor.Verify.CaretPositionAsync(16);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        public async Task CommitOnSaveAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"Module Module1
    Sub Main()
    End Sub
End Module
");

            await VisualStudio.Editor.PlaceCaretAsync("(", charsOffset: 1);
            await VisualStudio.Editor.SendKeysAsync("x   as   integer", VirtualKey.Tab);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.File_SaveSelectedItems);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"Sub Main(x As Integer)");
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_Undo);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"Sub Main(x   As   Integer)");
            await VisualStudio.Editor.Verify.CaretPositionAsync(45);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        public async Task CommitOnFocusLostAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"Module M
    Sub M()
    End Sub
End Module");

            await VisualStudio.Editor.PlaceCaretAsync("End Sub", charsOffset: -1);
            await VisualStudio.Editor.SendKeysAsync(" ");
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "TestZ.vb", open: true); // Cause focus lost
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "TestZ.vb"); // Work around https://github.com/dotnet/roslyn/issues/18488
            await VisualStudio.Editor.SendKeysAsync("                  ");
            await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "TestZ.vb", saveFile: false);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
    Sub M()
    End Sub
");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        public async Task CommitOnFocusLostDoesNotFormatWithPrettyListingOffAsync()
        {
            try
            {
                await VisualStudio.Workspace.SetPerLanguageOptionAsync(FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic, false);
                await VisualStudio.Editor.SetTextAsync(@"Module M
    Sub M()
    End Sub
End Module");

                await VisualStudio.Editor.PlaceCaretAsync("End Sub", charsOffset: -1);
                await VisualStudio.Editor.SendKeysAsync(" ");
                await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "TestZ.vb", open: true); // Cause focus lost
                await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "TestZ.vb"); // Work around https://github.com/dotnet/roslyn/issues/18488
                await VisualStudio.Editor.SendKeysAsync("                  ");
                await VisualStudio.SolutionExplorer.CloseFileAsync(ProjectName, "TestZ.vb", saveFile: false);
                await VisualStudio.Editor.Verify.TextContainsAsync(@"
    Sub M()
     End Sub
");
            }
            finally
            {
                await VisualStudio.Workspace.SetPerLanguageOptionAsync(FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic, true);
            }
        }
    }
}
