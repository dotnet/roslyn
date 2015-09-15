// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Suggested action for fix multiple occurrences code fix.
    /// </summary>
    internal class FixMultipleSuggestedAction : FixAllSuggestedAction
    {
        private readonly Document _triggerDocumentOpt;
        private readonly string _hashOfIds;

        internal FixMultipleSuggestedAction(
            Workspace workspace,
            ICodeActionEditHandlerService editHandler,
            FixMultipleCodeAction codeAction,
            FixAllProvider provider,
            ITextBuffer subjectBufferOpt = null)
            : base(workspace, subjectBufferOpt, editHandler, codeAction, provider, originalFixedDiagnostic: codeAction.GetTriggerDiagnostic())
        {
            _triggerDocumentOpt = codeAction.FixAllContext.Document;

            // hash all the diagnostic IDs
            _hashOfIds = GetHash(codeAction.FixAllContext.DiagnosticIds);
        }

        private static string GetHash(IEnumerable<string> diagnosticIds)
        {
            var hash = 0;
            foreach (var diagnosticId in diagnosticIds)
            {
                hash = Hash.Combine(diagnosticId.GetHashCode(), hash);
            }

            return hash.ToString(CultureInfo.InvariantCulture);
        }

        public new string GetDiagnosticID()
        {
            return _hashOfIds;
        }

        public override bool HasPreview
        {
            get
            {
                return false;
            }
        }

        public override Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return SpecializedTasks.Default<object>();
        }

        public override void Invoke(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesSession, cancellationToken))
            {
                // We might not have an origin subject buffer, for example if we are fixing selected diagnostics in the error list.
                if (this.SubjectBuffer != null)
                {
                    base.Invoke(cancellationToken);
                }
                else
                {
                    Func<Document> getDocument = () => _triggerDocumentOpt;
                    base.InvokeCore(getDocument, cancellationToken);
                }
            }
        }
    }
}
