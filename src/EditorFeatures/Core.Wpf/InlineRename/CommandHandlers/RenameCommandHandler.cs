// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [Name(PredefinedCommandHandlerNames.Rename)]
    // Line commit and rename are both executed on Save. Ensure any rename session is committed
    // before line commit runs to ensure changes from both are correctly applied.
    [Order(Before = PredefinedCommandHandlerNames.Commit)]
    // Commit rename before invoking command-based refactorings
    [Order(Before = PredefinedCommandHandlerNames.ChangeSignature)]
    [Order(Before = PredefinedCommandHandlerNames.ExtractInterface)]
    [Order(Before = PredefinedCommandHandlerNames.EncapsulateField)]
    internal partial class RenameCommandHandler : AbstractRenameCommandHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RenameCommandHandler(IThreadingContext threadingContext, InlineRenameService renameService)
            : base(threadingContext, renameService)
        {
        }

        protected override bool DashboardShouldReceiveKeyboardNavigation(ITextView textView)
            => GetDashboard(textView) is { } dashboard && dashboard.ShouldReceiveKeyboardNavigation;

        protected override void TextViewFocus(ITextView textView)
        {
            (textView as IWpfTextView)?.VisualElement.Focus();
        }

        protected override void DashboardFocus(ITextView textView)
        {
            if (GetDashboard(textView) is { } dashboard)
            {
                dashboard.Focus();
            }
        }

        protected override void DashboardFocusNextElement(ITextView textView)
        {
            if (GetDashboard(textView) is { } dashboard)
            {
                dashboard.FocusNextElement();
            }
        }

        protected override void DashboardFocusPreviousElement(ITextView textView)
        {
            if (GetDashboard(textView) is { } dashboard)
            {
                dashboard.FocusNextElement();
            }
        }

        private static Dashboard? GetDashboard(ITextView textView)
        {
            // If our adornment layer somehow didn't get composed, GetAdornmentLayer will throw.
            // Don't crash if that happens.
            try
            {
                var adornment = ((IWpfTextView)textView).GetAdornmentLayer("RoslynRenameDashboard");
                return adornment.Elements.Any()
                    ? adornment.Elements[0].Adornment as Dashboard
                    : null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }
    }
}
