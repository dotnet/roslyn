// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

internal sealed partial class CSharpCopilotCodeFixProvider
{
    /// <summary>
    /// Code action that triggers the dismissal of the Copilot suggestion.
    /// Reports telemetry when the suggestion is dismissed and will be extended to support
    /// dismissal of the diagnostic and removal of the suggestion from being shown again.
    /// </summary>
    private sealed class CopilotDismissChangesCodeAction(SyntaxNode originalMethodNode, Diagnostic diagnostic) : CodeAction
    {
        public override string Title => FeaturesResources.Dismiss;

        protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            => null!;

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            => [new TriggerDismissalCodeActionOperation(originalMethodNode, diagnostic)];

        private sealed class TriggerDismissalCodeActionOperation(SyntaxNode originalMethodNode, Diagnostic diagnostic) : CodeActionOperation
        {
            public override void Apply(Workspace workspace, CancellationToken cancellationToken)
            {
                // TODO: do not show this suggestion again after dismissal

                if (workspace.Services.GetService<IWorkspaceTelemetryService>()?.IsUserMicrosoftInternal is true)
                {
                    Logger.Log(FunctionId.Copilot_Suggestion_Dismissed, KeyValueLogMessage.Create(static (m, args) =>
                    {
                        var (diagnostic, originalMethodNode) = args;
                        if (diagnostic.Properties.TryGetValue(FixPropertyName, out var fix))
                            m["FixedMethod"] = fix;

                        if (diagnostic.Properties.TryGetValue(PromptTitlePropertyName, out var promptTitle))
                            m["PromptTitle"] = promptTitle;

                        m["DiagnosticId"] = diagnostic.Id;
                        m["Message"] = diagnostic.GetMessage();
                        m["OriginalMethod"] = originalMethodNode.ToFullString();
                    },
                    (diagnostic, originalMethodNode),
                    LogLevel.Information));
                }
                else
                {
                    Logger.Log(FunctionId.Copilot_Suggestion_Dismissed);
                }
            }
        }
    }
}
