// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;

namespace Microsoft.CodeAnalysis.Editor.StackTraceExplorer
{
    internal class StackFramePropertyMethodResolver : AbstractStackTraceSymbolResolver
    {
        public override Task<IMethodSymbol?> TryGetBestMatchAsync(Project project, INamedTypeSymbol type, StackFrameSimpleNameNode methodNode, StackFrameParameterList methodArguments, StackFrameTypeArgumentList? methodTypeArguments, CancellationToken cancellationToken)
        {
            var methodName = methodNode.ToString();

            var parts = methodName.Split('_');
            if (parts.Length != 2)
            {
                return Task.FromResult<IMethodSymbol?>(null);
            }

            var getSetName = parts[0];
            var propertyName = parts[1];

            if (getSetName != "get" && getSetName != "set")
            {
                return Task.FromResult<IMethodSymbol?>(null);
            }

            var candidateProperties = type
                .GetMembers(propertyName)
                .OfType<IPropertySymbol>();

            var bestMatch = candidateProperties
                .Select(c => getSetName == "get" ? c.GetMethod : c.SetMethod)
                .FirstOrDefault();

            return Task.FromResult(bestMatch);
        }
    }
}
