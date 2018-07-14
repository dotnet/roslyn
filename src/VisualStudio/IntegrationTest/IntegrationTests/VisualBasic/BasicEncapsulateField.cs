// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicEncapsulateField : AbstractIdeEditorTest
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
        [Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateThroughCommandAsync()
        {
            await SetUpEditorAsync(TestSource);

            var encapsulateField = VisualStudio.EncapsulateField;
            var dialog = VisualStudio.PreviewChangesDialog;
            await encapsulateField.InvokeAsync(cancellationToken: HangMitigatingCancellationToken);
            await dialog.VerifyOpenAsync(encapsulateField.DialogName, HangMitigatingCancellationToken);
            await dialog.ClickCancelAsync(encapsulateField.DialogName);
            await dialog.VerifyClosedAsync(encapsulateField.DialogName, HangMitigatingCancellationToken);
            await encapsulateField.InvokeAsync(cancellationToken: HangMitigatingCancellationToken);
            await dialog.VerifyOpenAsync(encapsulateField.DialogName, HangMitigatingCancellationToken);
            await dialog.ClickApplyAndWaitForFeatureAsync(encapsulateField.DialogName, FeatureAttribute.EncapsulateField);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"    Private _name As Integer? = 0

    Public Property Name As Integer?
        Get
            Return _name
        End Get
        Set(value As Integer?)
            _name = value
        End Set
    End Property");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateThroughLightbulbIncludingReferencesAsync()
        {
            await SetUpEditorAsync(TestSource);
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync("Encapsulate field: 'name' (and use property)", applyFix: true, willBlockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
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
End Module");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateThroughLightbulbDefinitionsOnlyAsync()
        {
            await SetUpEditorAsync(TestSource);
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync("Encapsulate field: 'name' (but still use field)", applyFix: true, willBlockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
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
End Module");
        }
    }
}
