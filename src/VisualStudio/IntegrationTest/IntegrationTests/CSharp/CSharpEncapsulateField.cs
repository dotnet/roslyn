// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpEncapsulateField : AbstractIdeEditorTest
    {
        public CSharpEncapsulateField()
            : base(nameof(CSharpEncapsulateField))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        private const string TestSource = @"
namespace myNamespace
{
    class Program
    {
        private static int? $$param = 0;
        static void Main(string[] args)
        {
            param = 80;
        }
    }
}";

        [IdeFact]
        [Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateThroughCommandAsync()
        {
            await SetUpEditorAsync(TestSource);
            var encapsulateField = VisualStudio.EncapsulateField;
            var dialog = VisualStudio.PreviewChangesDialog;

            var asyncCommand = encapsulateField.InvokeAsync(cancellationToken: HangMitigatingCancellationToken);
            await dialog.VerifyOpenAsync(encapsulateField.DialogName, HangMitigatingCancellationToken);
            await dialog.ClickCancelAsync(encapsulateField.DialogName);
            await dialog.VerifyClosedAsync(encapsulateField.DialogName, HangMitigatingCancellationToken);
            await asyncCommand;

            asyncCommand = encapsulateField.InvokeAsync(cancellationToken: HangMitigatingCancellationToken);
            await dialog.VerifyOpenAsync(encapsulateField.DialogName, HangMitigatingCancellationToken);
            await dialog.ClickApplyAndWaitForFeatureAsync(encapsulateField.DialogName, FeatureAttribute.EncapsulateField);
            await asyncCommand;

            await VisualStudio.Editor.Verify.TextContainsAsync("public static int? Param { get => param; set => param = value; }");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateThroughLightbulbIncludingReferencesAsync()
        {
            await SetUpEditorAsync(TestSource);
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync("Encapsulate field: 'param' (and use property)", applyFix: true, willBlockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
namespace myNamespace
{
    class Program
    {
        private static int? param = 0;

        public static int? Param { get => param; set => param = value; }

        static void Main(string[] args)
        {
            Param = 80;
        }
    }
}");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateThroughLightbulbDefinitionsOnlyAsync()
        {
            await SetUpEditorAsync(TestSource);
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync("Encapsulate field: 'param' (but still use field)", applyFix: true, willBlockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
namespace myNamespace
{
    class Program
    {
        private static int? param = 0;

        public static int? Param { get => param; set => param = value; }

        static void Main(string[] args)
        {
            param = 80;
        }
    }
}");
        }
    }
}
