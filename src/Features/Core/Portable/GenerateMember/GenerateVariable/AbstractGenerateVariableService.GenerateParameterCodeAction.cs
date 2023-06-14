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
        private class GenerateParameterCodeAction(Document document, State state, bool includeOverridesAndImplementations, int parameterIndex) : CodeAction
        {
            public override string Title
            {
                get
                {
                    var text = includeOverridesAndImplementations
                        ? FeaturesResources.Generate_parameter_0_and_overrides_implementations
                        : FeaturesResources.Generate_parameter_0;

                    return string.Format(
                        text,
                        state.IdentifierToken.ValueText);
                }
            }

            protected override Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return AddParameterService.AddParameterAsync(
                    document,
                    state.ContainingMethod,
                    state.LocalType,
                    RefKind.None,
                    state.IdentifierToken.ValueText,
                    parameterIndex,
                    includeOverridesAndImplementations,
                    cancellationToken).AsNullable();
            }
        }
    }
}
