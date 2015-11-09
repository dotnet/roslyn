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
using Microsoft.CodeAnalysis.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Suggested action for fix multiple occurrences code fix.
    /// </summary>
    internal class FixMultipleSuggestedAction : FixAllSuggestedAction
    {
        private readonly Document _triggerDocumentOpt;
        private readonly string _telemetryId;

        internal FixMultipleSuggestedAction(
            Workspace workspace,
            ICodeActionEditHandlerService editHandler,
            FixMultipleCodeAction codeAction,
            FixAllProvider provider,
            ITextBuffer subjectBufferOpt = null)
            : base(workspace, subjectBufferOpt, editHandler, codeAction, provider, originalFixedDiagnostic: codeAction.GetTriggerDiagnostic())
        {
            _triggerDocumentOpt = codeAction.FixAllContext.Document;

            _telemetryId = GetTelemetryId(codeAction.FixAllContext.DiagnosticIds);
        }

        private static string GetTelemetryId(IEnumerable<string> diagnosticIds)
        {
            // hash all the diagnostic IDs
            var hash = 0;
            foreach (var diagnosticId in diagnosticIds.Order())
            {
                hash = Hash.Combine(diagnosticId.GetHashCode(), hash);
            }

            return hash.ToString(CultureInfo.InvariantCulture);
        }

        public override string GetDiagnosticID()
        {
            return _telemetryId;
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

        public Solution GetChangedSolution(CancellationToken cancellationToken)
        {
            Solution newSolution = null;
            var extensionManager = this.Workspace.Services.GetService<IExtensionManager>();
            extensionManager.PerformAction(Provider, () =>
            {
                // We don't need to post process changes here as the inner code action created for Fix multiple code fix already executes.
                newSolution = CodeAction.GetChangedSolutionInternalAsync(postProcessChanges: false, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            });

            return newSolution;
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
                    InvokeCore(getDocument, cancellationToken);
                }
            }
        }
    }
}
