// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
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
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                await SetUpEditorAsync(TestSource);
                var encapsulateField = VisualStudio.EncapsulateField;
                var dialog = VisualStudio.PreviewChangesDialog;
                await encapsulateField.InvokeAsync();
                await dialog.VerifyOpenAsync(encapsulateField.DialogName, cancellationTokenSource.Token);
                await dialog.ClickCancelAsync(encapsulateField.DialogName);
                await dialog.VerifyClosedAsync(encapsulateField.DialogName, cancellationTokenSource.Token);
                await encapsulateField.InvokeAsync();
                await dialog.VerifyOpenAsync(encapsulateField.DialogName, cancellationTokenSource.Token);
                await dialog.ClickApplyAndWaitForFeatureAsync(encapsulateField.DialogName, FeatureAttribute.EncapsulateField);
                await VisualStudio.Editor.Verify.TextContainsAsync("public static int? Param { get => param; set => param = value; }");
            }
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateThroughLightbulbIncludingReferencesAsync()
        {
            await SetUpEditorAsync(TestSource);
            await VisualStudio.Editor.InvokeCodeActionListAsync();
            await VisualStudio.Editor.Verify.CodeActionAsync("Encapsulate field: 'param' (and use property)", applyFix: true, willBlockUntilComplete: true);
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
            await VisualStudio.Editor.InvokeCodeActionListAsync();
            await VisualStudio.Editor.Verify.CodeActionAsync("Encapsulate field: 'param' (but still use field)", applyFix: true, willBlockUntilComplete: true);
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
