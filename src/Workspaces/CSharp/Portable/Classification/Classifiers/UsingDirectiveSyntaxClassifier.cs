// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class UsingDirectiveSyntaxClassifier : AbstractSyntaxClassifier
    {
        public override IEnumerable<ClassifiedSpan> ClassifyNode(
            SyntaxNode syntax,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (syntax is UsingDirectiveSyntax usingDirective)
            {
                return ClassifyUsingDirectiveSyntax(usingDirective, semanticModel, cancellationToken);
            }

            return null;
        }

        public override IEnumerable<Type> SyntaxNodeTypes
        {
            get
            {
                yield return typeof(UsingDirectiveSyntax);
            }
        }

        private IEnumerable<ClassifiedSpan> ClassifyUsingDirectiveSyntax(
            UsingDirectiveSyntax usingDirective,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // For using aliases, we bind the target on the right of the equals and use that
            // binding to classify the alias.
            if (usingDirective.Alias != null)
            {
                var info = semanticModel.GetTypeInfo(usingDirective.Name, cancellationToken);
                if (info.Type != null)
                {
                    var classification = GetClassificationForType(info.Type);
                    if (classification != null)
                    {
                        var token = usingDirective.Alias.Name;
                        return SpecializedCollections.SingletonEnumerable(new ClassifiedSpan(token.Span, classification));
                    }
                }
            }

            return null;
        }
    }
}
