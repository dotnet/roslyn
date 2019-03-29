// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpMoveToNamespaceDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private MoveToNamespaceDialog_OutOfProc MoveToNamespaceDialog => VisualStudio.MoveToNamespaceDialog;

        public CSharpMoveToNamespaceDialog(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(CSharpMoveToNamespaceDialog))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToNamespace)]
        public void VerifyCancellation()
        {
            SetUpEditor(
@"
namespace A
{
    class C$$
    {
    }
}
");
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Move to namespace...",
                applyFix: true,
                blockUntilComplete: false);

            MoveToNamespaceDialog.VerifyOpen();
            MoveToNamespaceDialog.ClickCancel();
            MoveToNamespaceDialog.VerifyClosed();

            VisualStudio.Editor.Verify.TextContains(
@"
namespace A
{
    class C
    {
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToNamespace)]
        public void VerifyOkNoChange()
        {
            SetUpEditor(
@"
namespace A
{
    class C$$
    {
    }
}
");
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Move to namespace...",
                applyFix: true,
                blockUntilComplete: false);

            MoveToNamespaceDialog.VerifyOpen();
            MoveToNamespaceDialog.ClickOK();
            MoveToNamespaceDialog.VerifyClosed();

            VisualStudio.Editor.Verify.TextContains(
@"
namespace A
{
    class C
    {
    }
}
");
        }
}
