// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.CSharp.Copilot
{
    internal sealed partial class CSharpCopilotCodeFixProvider
    {
        private sealed class CopilotDismissChangesCodeAction : CodeAction
        {
            public override string Title => FeaturesResources.Dismiss;

            private readonly SyntaxNode _originalMethodNode;
            private readonly Diagnostic _diagnostic;

            public CopilotDismissChangesCodeAction(SyntaxNode originalMethodNode, Diagnostic diagnostic)
            {
                _originalMethodNode = originalMethodNode;
                _diagnostic = diagnostic;
            }

            protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
                => Task.FromResult<IEnumerable<CodeActionOperation>>(null!);

            protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
                => Task.FromResult<IEnumerable<CodeActionOperation>>(
                    [new TriggerDismissalCodeActionOperation(_originalMethodNode, _diagnostic)]);

            private sealed class TriggerDismissalCodeActionOperation(SyntaxNode originalMethodNode, Diagnostic diagnostic) : CodeActionOperation
            {
                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    Task.Run(Dismiss, cancellationToken);

                    void Dismiss()
                    {
                        // TODO: do not show this suggestion again after dismissal

                        if (workspace.Services.GetService<IWorkspaceTelemetryService>()?.IsUserMicrosoftInternal is true)
                        {
                            Logger.Log(FunctionId.Copilot_Suggestion_Dismissed, KeyValueLogMessage.Create(m =>
                            {
                                if (diagnostic.Properties.TryGetValue(FixPropertyName, out var fix))
                                    m["FixedMethod"] = fix;

                                if (diagnostic.Properties.TryGetValue(PromptTitlePropertyName, out var promptTitle))
                                    m["PromptTitle"] = promptTitle;

                                m["DiagnosticId"] = diagnostic.Id;
                                m["Message"] = diagnostic.GetMessage();
                                m["OriginalMethod"] = originalMethodNode.ToFullString();
                            },
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
    }
}
