// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ITypeInferenceService), LanguageNames.CSharp), Shared]
    internal partial class CSharpTypeInferenceService : ITypeInferenceService
    {
        public IEnumerable<ITypeSymbol> InferTypes(SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
        {
            return new TypeInferrer(semanticModel, cancellationToken).InferTypes(expression as ExpressionSyntax);
        }

        public IEnumerable<ITypeSymbol> InferTypes(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return new TypeInferrer(semanticModel, cancellationToken).InferTypes(position);
        }
    }
}