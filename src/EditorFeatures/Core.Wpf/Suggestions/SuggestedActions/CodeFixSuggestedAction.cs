// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Represents light bulb menu item for code fixes.
    /// </summary>
    internal sealed class CodeFixSuggestedAction : SuggestedActionWithNestedFlavors, ITelemetryDiagnosticID<string>
    {
        private readonly CodeFix _fix;

        public CodeFixSuggestedAction(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            CodeFix fix,
            object provider,
            CodeAction action,
            SuggestedActionSet fixAllFlavors)
            : base(threadingContext, sourceProvider, workspace, subjectBuffer,
                   provider, action, fixAllFlavors)
        {
            _fix = fix;
        }

        public string GetDiagnosticID()
        {
            return _fix.PrimaryDiagnostic.GetTelemetryDiagnosticID();
        }

        protected override DiagnosticData GetDiagnostic()
        {
            return _fix.GetPrimaryDiagnosticData();
        }
    }
}
