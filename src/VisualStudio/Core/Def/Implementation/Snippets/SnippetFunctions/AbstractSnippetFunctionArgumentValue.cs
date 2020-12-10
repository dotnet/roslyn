// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract class AbstractSnippetFunctionArgumentValue : AbstractSnippetFunction
    {
        private readonly string _parameterName;
        private readonly SymbolKey _parameter;

        protected AbstractSnippetFunctionArgumentValue(AbstractSnippetExpansionClient snippetExpansionClient, ITextBuffer subjectBuffer, string fieldName, string parameter)
            : base(snippetExpansionClient, subjectBuffer)
        {
            _parameterName = fieldName;
            _parameter = new SymbolKey(parameter);
        }

        protected abstract string? FallbackDefaultLiteral { get; }

        protected override int GetDefaultValue(CancellationToken cancellationToken, out string? value, out int hasCurrentValue)
        {
            if (!TryGetDocument(out var document))
            {
                value = null;
                hasCurrentValue = 0;
                return VSConstants.E_FAIL;
            }

            var textSpan = new VsTextSpan[1];
            if (snippetExpansionClient.ExpansionSession.GetFieldSpan(_parameterName, textSpan) != VSConstants.S_OK)
            {
                value = null;
                hasCurrentValue = 0;
                return VSConstants.E_FAIL;
            }

            var text = document.GetTextSynchronously(cancellationToken);
            var position = text.Lines.GetPosition(new LinePosition(textSpan[0].iStartLine, textSpan[0].iStartIndex));
            var compilation = document.Project.GetRequiredCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            if (!snippetExpansionClient.Arguments.TryGetValue(_parameterName, out var previousValue))
                previousValue = null;

            if (_parameter.Resolve(compilation, cancellationToken: cancellationToken).GetAnySymbol() is IParameterSymbol parameter)
            {
                foreach (var provider in snippetExpansionClient.GetArgumentProviders(document.Project.Solution.Workspace))
                {
                    var context = new ArgumentContext(provider, document, position, parameter, previousValue, cancellationToken);
                    provider.ProvideArgumentAsync(context).Wait(cancellationToken);
                    if (context.DefaultValue is not null)
                    {
                        value = context.DefaultValue;
                        hasCurrentValue = 1;
                        return VSConstants.S_OK;
                    }
                }
            }

            value = FallbackDefaultLiteral;
            hasCurrentValue = value is not null ? 1 : 0;
            return value is not null ? VSConstants.S_OK : VSConstants.E_FAIL;
        }
    }
}
