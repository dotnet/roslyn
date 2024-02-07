// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.EncapsulateField)]
    public class BasicEncapsulateField : AbstractEditorTest
    {
        public BasicEncapsulateField()
            : base(nameof(BasicEncapsulateField))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        private const string TestSource = @"
Module Module1
        Public $$name As Integer? = 0
    Sub Main()
        name = 90
    End Sub
End Module";

        [IdeFact]
        public async Task EncapsulateThroughCommand()
        {
            await SetUpEditorAsync(TestSource, HangMitigatingCancellationToken);

            var encapsulateField = TestServices.EncapsulateField;
            var dialog = TestServices.PreviewChangesDialog;
            await encapsulateField.InvokeAsync(HangMitigatingCancellationToken);
            await dialog.VerifyOpenAsync(encapsulateField.DialogName, HangMitigatingCancellationToken);
            await dialog.ClickCancelAsync(encapsulateField.DialogName, HangMitigatingCancellationToken);
            await dialog.VerifyClosedAsync(encapsulateField.DialogName, HangMitigatingCancellationToken);
            await encapsulateField.InvokeAsync(HangMitigatingCancellationToken);
            await dialog.VerifyOpenAsync(encapsulateField.DialogName, HangMitigatingCancellationToken);
            await dialog.ClickApplyAndWaitForFeatureAsync(encapsulateField.DialogName, FeatureAttribute.EncapsulateField, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"    Private _name As Integer? = 0

    Public Property Name As Integer?
        Get
            Return _name
        End Get
        Set(value As Integer?)
            _name = value
        End Set
    End Property", cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task EncapsulateThroughLightbulbIncludingReferences()
        {
            await SetUpEditorAsync(TestSource, HangMitigatingCancellationToken);

            // Suspend file change notification during code action application, since spurious file change notifications
            // can cause silent failure to apply the code action if they occur within this block.
            await using (var fileChangeRestorer = await TestServices.Shell.PauseFileChangesAsync(HangMitigatingCancellationToken))
            {
                await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
                await TestServices.EditorVerifier.CodeActionAsync("Encapsulate field: 'name' (and use property)", applyFix: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
            }

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Private _name As Integer? = 0

    Public Property Name As Integer?
        Get
            Return _name
        End Get
        Set(value As Integer?)
            _name = value
        End Set
    End Property

    Sub Main()
        Name = 90
    End Sub
End Module", cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task EncapsulateThroughLightbulbDefinitionsOnly()
        {
            await SetUpEditorAsync(TestSource, HangMitigatingCancellationToken);

            // Suspend file change notification during code action application, since spurious file change notifications
            // can cause silent failure to apply the code action if they occur within this block.
            await using (var fileChangeRestorer = await TestServices.Shell.PauseFileChangesAsync(HangMitigatingCancellationToken))
            {
                await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
                await TestServices.EditorVerifier.CodeActionAsync("Encapsulate field: 'name' (but still use field)", applyFix: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
            }

            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Module1
    Private _name As Integer? = 0

    Public Property Name As Integer?
        Get
            Return _name
        End Get
        Set(value As Integer?)
            _name = value
        End Set
    End Property

    Sub Main()
        name = 90
    End Sub
End Module", cancellationToken: HangMitigatingCancellationToken);
        }
    }
}
