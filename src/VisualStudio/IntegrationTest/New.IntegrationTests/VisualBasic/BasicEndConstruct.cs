// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.EndConstructGeneration)]
public class BasicEndConstruct : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicEndConstruct()
        : base(nameof(BasicEndConstruct))
    {
    }

    [IdeFact]
    public async Task EndConstruct()
    {
        await SetUpEditorAsync(@"
Class Program
    Sub Main()
        If True Then $$
    End Sub
End Class", HangMitigatingCancellationToken);
        // Send a space to convert virtual whitespace into real whitespace
        await TestServices.Input.SendAsync([VirtualKeyCode.RETURN, " "], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"
Class Program
    Sub Main()
        If True Then
             $$
        End If
    End Sub
End Class", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task IntelliSenseCompletedWhile()
    {
        await SetUpEditorAsync(@"
Class Program
    Sub Main()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);
        // Send a space to convert virtual whitespace into real whitespace
        await TestServices.Input.SendAsync(["While True", VirtualKeyCode.RETURN, " "], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"
Class Program
    Sub Main()
        While True
             $$
        End While
    End Sub
End Class", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task InterfaceToClassFixup()
    {
        await SetUpEditorAsync(@"
Interface$$ C
End Interface", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync((VirtualKeyCode.BACK, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(["Class", VirtualKeyCode.TAB], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"
Class C
End Class", cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task CaseInsensitiveSubToFunction()
    {
        await SetUpEditorAsync(@"
Class C
    Public Sub$$ Goo()
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync((VirtualKeyCode.BACK, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(["fu", VirtualKeyCode.TAB], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"
Class C
    Public Function Goo()
    End Function
End Class", cancellationToken: HangMitigatingCancellationToken);
    }
}
