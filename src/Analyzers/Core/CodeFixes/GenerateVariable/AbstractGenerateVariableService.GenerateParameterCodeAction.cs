// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateVariable;

internal partial class AbstractGenerateVariableService<TService, TSimpleNameSyntax, TExpressionSyntax>
{
    private sealed class GenerateParameterCodeAction(Document document, State state, bool includeOverridesAndImplementations, int parameterIndex) : CodeAction
    {
        private readonly Document _document = document;
        private readonly State _state = state;
        private readonly bool _includeOverridesAndImplementations = includeOverridesAndImplementations;
        private readonly int _parameterIndex = parameterIndex;

        public override string Title
        {
            get
            {
                var text = _includeOverridesAndImplementations
                    ? CodeFixesResources.Generate_parameter_0_and_overrides_implementations
                    : CodeFixesResources.Generate_parameter_0;

                return string.Format(
                    text,
                    _state.IdentifierToken.ValueText);
            }
        }

        protected override Task<Solution?> GetChangedSolutionAsync(
            IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            return AddParameterService.AddParameterAsync(
                _document,
                _state.ContainingMethod,
                _state.LocalType,
                RefKind.None,
                _state.IdentifierToken.ValueText,
                _parameterIndex,
                _includeOverridesAndImplementations,
                cancellationToken).AsNullable();
        }
    }
}
