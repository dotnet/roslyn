// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors;

internal abstract partial class AbstractGenerateDefaultConstructorsService<TService>
{
    private sealed class GenerateDefaultConstructorCodeAction(
        Document document,
        State state,
        IMethodSymbol constructor,
        CodeAndImportGenerationOptionsProvider fallbackOptions) : AbstractCodeAction(document, state, new[] { constructor }, GetDisplayText(state, constructor), fallbackOptions)
    {
        private static string GetDisplayText(State state, IMethodSymbol constructor)
        {
            var parameters = constructor.Parameters.Select(p => p.Name);
            var parameterString = string.Join(", ", parameters);

            Contract.ThrowIfNull(state.ClassType);
            return string.Format(FeaturesResources.Generate_constructor_0_1,
                state.ClassType.Name, parameterString);
        }
    }
}
