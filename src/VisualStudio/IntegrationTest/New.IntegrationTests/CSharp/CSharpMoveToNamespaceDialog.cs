// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.CodeActionsMoveToNamespace)]
public class CSharpMoveToNamespaceDialog : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpMoveToNamespaceDialog()
        : base(nameof(CSharpMoveToNamespaceDialog))
    {
    }

    [IdeFact]
    public async Task VerifyCancellation()
    {
        await SetUpEditorAsync(
            """

            namespace A
            {
                class C$$
                {
                }
            }

            """, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Move to namespace...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.MoveToNamespaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.MoveToNamespaceDialog.ClickCancelAsync(HangMitigatingCancellationToken);
        await TestServices.MoveToNamespaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync(
            """

            namespace A
            {
                class C
                {
                }
            }

            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyCancellationWithChange()
    {
        await SetUpEditorAsync(
            """

            namespace A
            {
                class C$$
                {
                }
            }

            """, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Move to namespace...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.MoveToNamespaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.MoveToNamespaceDialog.SetNamespaceAsync("B", HangMitigatingCancellationToken);
        await TestServices.MoveToNamespaceDialog.ClickCancelAsync(HangMitigatingCancellationToken);
        await TestServices.MoveToNamespaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync(
            """

            namespace A
            {
                class C
                {
                }
            }

            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyOkNoChange()
    {
        await SetUpEditorAsync(
            """

            namespace A
            {
                class C$$
                {
                }
            }

            """, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Move to namespace...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.MoveToNamespaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.MoveToNamespaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
        await TestServices.MoveToNamespaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync(
            """

            namespace A
            {
                class C
                {
                }
            }

            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyOkWithChange()
    {
        await SetUpEditorAsync(
            """
            namespace A
            {
                class C$$
                {
                }
            }

            """, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Move to namespace...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.MoveToNamespaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.MoveToNamespaceDialog.SetNamespaceAsync("B", HangMitigatingCancellationToken);
        await TestServices.MoveToNamespaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
        await TestServices.MoveToNamespaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync(
            """
            namespace B
            {
                class C
                {
                }
            }

            """, cancellationToken: HangMitigatingCancellationToken);
    }
}
