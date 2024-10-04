// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

public class BasicOrganizing : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicOrganizing()
        : base(nameof(BasicOrganizing))
    {
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.Organizing)]
    public async Task RemoveAndSort()
    {
        await SetUpEditorAsync(@"Imports System.Linq$$
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices
Class Test
    Sub Method(<CallerMemberName> Optional str As String = Nothing)
        Dim data As COMException
    End Sub
End Class", HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.RemoveAndSort, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(
            FeatureAttribute.OrganizeDocument,
            HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Class Test
    Sub Method(<CallerMemberName> Optional str As String = Nothing)
        Dim data As COMException
    End Sub
End Class", cancellationToken: HangMitigatingCancellationToken);

    }
}
