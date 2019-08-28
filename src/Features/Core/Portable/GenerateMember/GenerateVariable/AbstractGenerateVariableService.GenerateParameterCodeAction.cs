// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeActions;

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

            protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return AddParameterService.Instance.AddParameterAsync(
                    _document,
                    _state.ContainingMethod,
                    _state.LocalType,
                    RefKind.None,
                    _state.IdentifierToken.ValueText,
                    newParameterIndex: null,
                    _includeOverridesAndImplementations,
                    cancellationToken);
            }
        }
    }
}
