// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Suggested action for fix all occurrences code fix.
    /// </summary>
    internal class FixAllSuggestedAction : SuggestedAction, ITelemetryDiagnosticID<string>
    {
        private readonly Diagnostic _fixedDiagnostic;

        internal FixAllSuggestedAction(
            Workspace workspace,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            IWaitIndicator waitIndicator,
            FixAllCodeAction codeAction,
            FixAllProvider provider,
            Diagnostic originalFixedDiagnostic)
            : base(workspace, subjectBuffer, editHandler, waitIndicator, codeAction, provider)
        {
            _fixedDiagnostic = originalFixedDiagnostic;
        }

        public virtual string GetDiagnosticID()
        {
            // we log diagnostic id as it is if it is from us
            if (_fixedDiagnostic.Descriptor.CustomTags.Any(t => t == WellKnownDiagnosticTags.Telemetry))
            {
                return _fixedDiagnostic.Id;
            }

            // if it is from third party, we use hashcode
            return _fixedDiagnostic.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }

        public override bool HasPreview
        {
            get
            {
                // Since FixAllSuggestedAction will always be presented as a
                // 'flavored' action, it will never have a preview.
                return false;
            }
        }

        public override Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            // Since FixAllSuggestedAction will always be presented as a
            // 'flavored' action, code in the VS editor / lightbulb layer should
            // never call GetPreview() on it. We override and return null here
            // regardless so that nothing blows up if this ends up getting called.
            return SpecializedTasks.Default<object>();
        }

        public override void Invoke(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesSession, cancellationToken))
            {
                base.Invoke(cancellationToken);
            }
        }
    }
}
