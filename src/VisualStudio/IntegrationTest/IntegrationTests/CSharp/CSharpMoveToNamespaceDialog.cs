// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public CSharpMoveToNamespaceDialog(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpMoveToNamespaceDialog))
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
        public void VerifyCancellationWithChange()
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
            MoveToNamespaceDialog.SetNamespace("B");
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToNamespace)]
        public void VerifyOkWithChange()
        {
            SetUpEditor(
@"namespace A
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
            MoveToNamespaceDialog.SetNamespace("B");
            MoveToNamespaceDialog.ClickOK();
            MoveToNamespaceDialog.VerifyClosed();

            VisualStudio.Editor.Verify.TextContains(
@"namespace B
{
    class C
    {
    }
}
");
        }
    }
}
