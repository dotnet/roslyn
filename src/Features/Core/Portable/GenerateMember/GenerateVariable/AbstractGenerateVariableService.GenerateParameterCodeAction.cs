// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateVariable
{
    internal partial class AbstractGenerateVariableService<TService, TSimpleNameSyntax, TExpressionSyntax>
    {
        private class GenerateParameterCodeAction : CodeAction
        {
            private readonly Document _document;
            private readonly State _state;
            private readonly bool _includeOverridesAndImplementations;

            public GenerateParameterCodeAction(Document document, State state, bool includeOverridesAndImplementations)
            {
                _document = document;
                _state = state;
                _includeOverridesAndImplementations = includeOverridesAndImplementations;
            }

            public override string Title
            {
                get
                {
                    var text = _includeOverridesAndImplementations
                        ? FeaturesResources.Generate_parameter_0_and_overrides_implementations
                        : FeaturesResources.Generate_parameter_0;

                    return string.Format(
                        text,
                        _state.IdentifierToken.ValueText);
                }
            }

            protected override Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return AddParameterService.AddParameterAsync(
                    _document,
                    _state.ContainingMethod,
                    _state.LocalType,
                    RefKind.None,
                    _state.IdentifierToken.ValueText,
                    newParameterIndex: null,
                    _includeOverridesAndImplementations,
                    cancellationToken).AsNullable();
            }
        }
    }
}
