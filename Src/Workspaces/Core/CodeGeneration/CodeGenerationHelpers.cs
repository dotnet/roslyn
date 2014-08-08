// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal static class CodeGenerationHelpers
    {
        public static SyntaxNode GenerateThrowStatement(
            SyntaxGenerator factory,
            SemanticDocument document,
            string exceptionMetadataName,
            CancellationToken cancellationToken)
        {
            var compilation = document.SemanticModel.Compilation;
            var exceptionType = compilation.GetTypeByMetadataName(exceptionMetadataName);

            // If we can't find the Exception, we obviously can't generate anything.
            if (exceptionType == null)
            {
                return null;
            }

            var exceptionCreationExpression = factory.ObjectCreationExpression(
                exceptionType,
                SpecializedCollections.EmptyList<SyntaxNode>());

            return factory.ThrowStatement(exceptionCreationExpression);
        }
    }
}
