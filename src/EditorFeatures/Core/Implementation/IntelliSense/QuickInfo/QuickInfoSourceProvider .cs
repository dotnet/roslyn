// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    [Export]    
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("RoslynQuickInfoProvider")]
    internal partial class QuickInfoSourceProvider :
        ForegroundThreadAffinitizedObject,
        IAsyncQuickInfoSourceProvider
    {
        private bool TryGetController(ITextView textView, ITextBuffer subjectBuffer, out Controller controller)
        {
            // check whether this feature is on.
            if (!subjectBuffer.GetFeatureOnOffOption(InternalFeatureOnOffOptions.QuickInfo))
            {
                controller = null;
                return false;
            }

            controller = Controller.GetInstance(textView, subjectBuffer);
            return true;
        }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new QuickInfoSource(this, textBuffer);
        }

        internal bool TryHandleEscapeKey(EscapeKeyCommandArgs commandArgs)
        {
            if (!TryGetController(commandArgs.TextView, commandArgs.SubjectBuffer, out var controller))
            {
                return false;
            }

            return controller.TryHandleEscapeKey();
        }
    }
}
