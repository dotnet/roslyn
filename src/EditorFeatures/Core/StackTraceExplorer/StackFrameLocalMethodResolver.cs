// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.StackTraceExplorer
{
    internal class StackFrameLocalMethodResolver : AbstractStackTraceSymbolResolver
    {
        public override async Task<IMethodSymbol?> TryGetBestMatchAsync(Project project, INamedTypeSymbol type, StackFrameSimpleNameNode methodNode, StackFrameParameterList methodArguments, StackFrameTypeArgumentList? methodTypeArguments, CancellationToken cancellationToken)
        {
            if (methodNode is not StackFrameLocalMethodNameNode localMethodNameNode)
            {
                return null;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
            {
                return null;
            }

            var syntaxFacts = project.GetRequiredLanguageService<ISyntaxFactsService>();
            var localFunctions = type.GetLocalFunctionSymbols(syntaxFacts, compilation, cancellationToken);
            var containingMethodName = localMethodNameNode.EncapsulatingMethod.Identifier.ToString();

            var candidateFunctions = localFunctions
                .Where(m => m.ContainingSymbol.Name == containingMethodName)
                .ToImmutableArray();

            return TryGetBestMatch(candidateFunctions, methodTypeArguments, methodArguments);
        }
    }
}
