// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;
using static Roslyn.Utilities.TaskExtensions;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract class AbstractSnippetFunctionArgumentValue : AbstractSnippetFunction
    {
        /// <summary>
        /// The name of the parameter for which an argument value is requested.
        /// </summary>
        private readonly string _parameterName;

        /// <summary>
        /// A <see cref="SymbolKey"/> allowing the <see cref="IParameterSymbol"/> to be resolved.
        /// </summary>
        private readonly SymbolKey _parameterKey;

        protected AbstractSnippetFunctionArgumentValue(AbstractSnippetExpansionClient snippetExpansionClient, ITextBuffer subjectBuffer, string parameterName, SymbolKey parameterKey)
            : base(snippetExpansionClient, subjectBuffer)
        {
            _parameterName = parameterName;
            _parameterKey = parameterKey;
        }

        protected abstract string? FallbackDefaultLiteral { get; }

        protected override int GetDefaultValue(CancellationToken cancellationToken, out string? value, out int hasCurrentValue)
        {
            value = snippetExpansionClient.ThreadingContext.JoinableTaskFactory.Run(() => TryGetDefaultValueAsync(cancellationToken));
            if (value is not null)
            {
                hasCurrentValue = 1;
                return VSConstants.S_OK;
            }
            else
            {
                hasCurrentValue = 0;
                return VSConstants.E_FAIL;
            }
        }

        [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Containing method uses JTF https://github.com/dotnet/roslyn-analyzers/issues/4283")]
        private async Task<string?> TryGetDefaultValueAsync(CancellationToken cancellationToken)
        {
            await snippetExpansionClient.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (!TryGetDocument(out var document))
            {
                // Unable to suggest arguments if a document is not available
                return null;
            }

            var textSpan = new VsTextSpan[1];
            if (snippetExpansionClient.ExpansionSession.GetFieldSpan(_parameterName, textSpan) != VSConstants.S_OK)
            {
                // Failed to obtain the snippet placeholder for the named parameter.
                // TODO: Telemetry https://github.com/dotnet/roslyn/issues/50033
                return null;
            }

            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken);
            if (_parameterKey.Resolve(compilation, cancellationToken: cancellationToken).GetAnySymbol() is not IParameterSymbol parameter)
            {
                // Failed to resolve the IParameterSymbol from the SymbolKey.
                // TODO: Telemetry https://github.com/dotnet/roslyn/issues/50033
                return null;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken);
            var text = await document.GetTextAsync(cancellationToken);
            var position = text.Lines.GetPosition(new LinePosition(textSpan[0].iStartLine, textSpan[0].iStartIndex));
            var previousValue = snippetExpansionClient.Arguments.GetValueOrDefault(_parameterName);
            foreach (var provider in snippetExpansionClient.GetArgumentProviders(document.Project.Solution.Workspace))
            {
                var context = new ArgumentContext(provider, semanticModel, position, parameter, previousValue, cancellationToken);
                await provider.ProvideArgumentAsync(context);
                if (context.DefaultValue is not null)
                {
                    return context.DefaultValue;
                }
            }

            // In this case, none of the argument providers offered a value. Use a language-specific default if
            // available; otherwise, we simply return null.
            return FallbackDefaultLiteral;
        }
    }
}
