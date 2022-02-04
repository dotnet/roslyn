// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal class FunctionPointerCallingConventionClassifier : AbstractSyntaxClassifier
    {
        public override ImmutableArray<Type> SyntaxNodeTypes { get; } = ImmutableArray.Create(
            typeof(FunctionPointerUnmanagedCallingConventionSyntax));

        public override void AddClassifications(
            SyntaxNode syntax,
            SemanticModel semanticModel,
            ClassificationOptions options,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            result.Add(new ClassifiedSpan(callingConventionSyntax.Name.Span, ClassificationTypeNames.ClassName));
        }
    }
}
