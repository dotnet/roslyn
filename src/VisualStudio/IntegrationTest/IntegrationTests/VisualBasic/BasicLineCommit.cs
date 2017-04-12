﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using ProjName = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicLineCommit : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicLineCommit(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicLineCommit))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        void CaseCorrection()
        {
            VisualStudio.Editor.SetText(@"Module Foo
    Sub M()
Dim x = Sub()
    End Sub
End Module");

            VisualStudio.Editor.PlaceCaret("Sub()", charsOffset: 1);
            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.Verify.CaretPosition(48);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        void UndoWithEndConstruct()
        {
            VisualStudio.Editor.SetText(@"Module Module1
    Sub Main()
    End Sub
    REM
End Module");

            VisualStudio.Editor.PlaceCaret("    REM");
            VisualStudio.Editor.SendKeys("sub", VirtualKey.Escape, " foo()", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"Sub foo()

    End Sub");
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_Undo);
            VisualStudio.Editor.Verify.CaretPosition(54);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        void UndoWithoutEndConstruct()
        {
            VisualStudio.Editor.SetText(@"Module Module1

    ''' <summary></summary>
    Sub Main()
    End Sub
End Module");

            VisualStudio.Editor.PlaceCaret("Module1");
            VisualStudio.Editor.SendKeys(VirtualKey.Down, VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"Module Module1


    ''' <summary></summary>
    Sub Main()
    End Sub
End Module");
            VisualStudio.Editor.Verify.CaretPosition(18);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_Undo);
            VisualStudio.Editor.Verify.CaretPosition(16);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        void CommitOnSave()
        {
            VisualStudio.Editor.SetText(@"Module Module1
    Sub Main()
    End Sub
End Module");

            VisualStudio.Editor.PlaceCaret("(", charsOffset: 1);
            VisualStudio.Editor.SendKeys("x   as   integer", VirtualKey.Tab);
            VisualStudio.ExecuteCommand("File.SaveSelectedItems");
            VisualStudio.Editor.Verify.TextContains(@"Sub Main(x As Integer)");
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_Undo);
            VisualStudio.Editor.Verify.TextContains(@"Sub Main(x   As   Integer)");
            VisualStudio.Editor.Verify.CaretPosition(45);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        void CommitOnFocusLost()
        {
            VisualStudio.Editor.SetText(@"Module M
    Sub M()
    End Sub
End Module");

            VisualStudio.Editor.PlaceCaret("End Sub", charsOffset: -1);
            VisualStudio.Editor.SendKeys(" ");
            VisualStudio.SolutionExplorer.AddFile(new ProjName(ProjectName), "TestZ.vb", open: true); // Cause focus lost
            VisualStudio.SolutionExplorer.OpenFile(new ProjName(ProjectName), "TestZ.vb"); // Work around https://github.com/dotnet/roslyn/issues/18488
            VisualStudio.Editor.SendKeys("                  ");
            VisualStudio.SolutionExplorer.CloseFile(new ProjName(ProjectName), "TestZ.vb", saveFile: false);
            VisualStudio.Editor.Verify.TextContains(@"
    Sub M()
    End Sub
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineCommit)]
        void CommitOnFocusLostDoesNotFormatWithPrettyListingOff()
        {
            try
            {
                VisualStudio.Workspace.SetPerLanguageOption("PrettyListing", "FeatureOnOffOptions", LanguageNames.VisualBasic, false);
                VisualStudio.Editor.SetText(@"Module M
    Sub M()
    End Sub
End Module");

                VisualStudio.Editor.PlaceCaret("End Sub", charsOffset: -1);
                VisualStudio.Editor.SendKeys(" ");
                VisualStudio.SolutionExplorer.AddFile(new ProjName(ProjectName), "TestZ.vb", open: true); // Cause focus lost
                VisualStudio.SolutionExplorer.OpenFile(new ProjName(ProjectName), "TestZ.vb"); // Work around https://github.com/dotnet/roslyn/issues/18488
                VisualStudio.Editor.SendKeys("                  ");
                VisualStudio.SolutionExplorer.CloseFile(new ProjName(ProjectName), "TestZ.vb", saveFile: false);
                VisualStudio.Editor.Verify.TextContains(@"
    Sub M()
     End Sub
");
            }
            finally
            {
                VisualStudio.Workspace.SetPerLanguageOption("PrettyListing", "FeatureOnOffOptions", LanguageNames.VisualBasic, true);
            }
        }
    }
}