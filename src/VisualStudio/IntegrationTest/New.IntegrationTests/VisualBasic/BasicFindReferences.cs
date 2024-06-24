// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Shell.TableControl;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.FindReferences)]
public class BasicFindReferences : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicFindReferences()
        : base(nameof(BasicFindReferences))
    {
    }

    [IdeFact]
    public async Task FindReferencesToLocals()
    {
        await SetUpEditorAsync(@"
Class Program
  Sub Main()
      Dim local = 1
      Console.WriteLine(loca$$l)
  End Sub
End Class
", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);

        var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

        Assert.Collection(
            results,
            new Action<ITableEntryHandle2>[]
            {
                reference =>
                {
                    Assert.Equal(expected: "Dim local = 1", actual: reference.GetText());
                    Assert.Equal(expected: 3, actual: reference.GetLine());
                    Assert.Equal(expected: 10, actual: reference.GetColumn());
                },
                reference =>
                {
                    Assert.Equal(expected: "Console.WriteLine(local)", actual: reference.GetText());
                    Assert.Equal(expected: 4, actual: reference.GetLine());
                    Assert.Equal(expected: 24, actual: reference.GetColumn());
                }
            });
    }

    [IdeFact]
    public async Task FindReferencesToSharedField()
    {
        await SetUpEditorAsync(@"
Class Program
    Public Shared Alpha As Int32
End Class$$
", HangMitigatingCancellationToken);
        var project = ProjectName;
        await TestServices.SolutionExplorer.AddFileAsync(project, "File2.vb", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "File2.vb", HangMitigatingCancellationToken);

        await SetUpEditorAsync(@"
Class SomeOtherClass
    Sub M()
        Console.WriteLine(Program.$$Alpha)
    End Sub
End Class
", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);

        var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

        Assert.Collection(
            results,
            new Action<ITableEntryHandle2>[]
            {
                reference =>
                {
                    Assert.Equal(expected: "Public Shared Alpha As Int32", actual: reference.GetText());
                    Assert.Equal(expected: 2, actual: reference.GetLine());
                    Assert.Equal(expected: 18, actual: reference.GetColumn());
                },
                reference =>
                {
                    Assert.Equal(expected: "Console.WriteLine(Program.Alpha)", actual: reference.GetText());
                    Assert.Equal(expected: 3, actual: reference.GetLine());
                    Assert.Equal(expected: 34, actual: reference.GetColumn());
                }
            });

        await TestServices.FindReferencesWindow.NavigateToAsync(results[0], isPreview: false, shouldActivate: true, HangMitigatingCancellationToken);

        // Assert we are in the right file now
        Assert.Equal($"Class1.vb", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        Assert.Equal("Alpha As Int32", await TestServices.Editor.GetLineTextAfterCaretAsync(HangMitigatingCancellationToken));
    }
}
