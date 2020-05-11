// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private ObjectCreationExpressionSyntax? TryGenerateObjectCreation(IObjectCreationOperation? operation, SyntaxType syntaxType)
        {
            if (operation == null)
                return null;

            if (syntaxType == SyntaxType.Statement)
                throw new ArgumentException($"{nameof(IObjectCreationOperation)} cannot be converted to a {nameof(StatementSyntax)}");

            var type = operation.Constructor?.ContainingType ?? operation.Type;
            if (type == null)
                throw new ArgumentException($"{nameof(IObjectCreationOperation)} must have a {nameof(IObjectCreationOperation.Constructor)} or {nameof(IObjectCreationOperation.Type)}");

            var argumentList = GenerateArgumentList(operation.Arguments);
            var initializer = GenerateInitilizer(operation.Initializer);

            return ObjectCreationExpression(
                type.GenerateTypeSyntax(),
                argumentList,
                initializer);
        }

        private InitializerExpressionSyntax? GenerateInitilizer(IObjectOrCollectionInitializerOperation initializer)
        {
            if (initializer == null)
                return null;

            throw new NotImplementedException();
        }
    }
}
