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

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.EncapsulateField)]
public class CSharpEncapsulateField : AbstractEditorTest
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
        await TestServices.EditorVerifier.TextContainsAsync("public static int? Param { get => param; set => param = value; }");
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
            await TestServices.EditorVerifier.CodeActionAsync("Encapsulate field: 'param' (and use property)", applyFix: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
        }

        await TestServices.EditorVerifier.TextContainsAsync(@"
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
}", cancellationToken: HangMitigatingCancellationToken);
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
            await TestServices.EditorVerifier.CodeActionAsync("Encapsulate field: 'param' (but still use field)", applyFix: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
        }

        await TestServices.EditorVerifier.TextContainsAsync(@"
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
}", cancellationToken: HangMitigatingCancellationToken);
    }
}
