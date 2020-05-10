// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private IArgumentOperation? WrapWithArgument(IOperation? operation)
            => operation == null ? null :
               operation is IArgumentOperation argument ? argument :
                    CodeGenerator.Argument(ArgumentKind.Explicit, null, operation);

        private ArgumentSyntax? TryGenerateArgument(IArgumentOperation? operation)
        {
            if (operation == null)
                return null;

            // todo: handle reordered parameters
            var expr = TryGenerateExpression(operation.Value);
            if (expr == null)
                return null;

            var parameterRefKind = operation.Parameter?.RefKind ?? default;
            var refKindKeyword = parameterRefKind switch
            {
                RefKind.Ref => SyntaxKind.RefKeyword,
                RefKind.Out => SyntaxKind.OutKeyword,
                RefKind.In => SyntaxKind.InKeyword,
                _ => default,
            };

            return Argument(
                nameColon: null,
                refKindKeyword == default ? default : Token(refKindKeyword),
                expr);
        }
    }
}
