// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Simplification;

namespace Analyzer.Utilities
{
    internal static class SyntaxNodeExtensions
    {
        private static Optional<SyntaxAnnotation?> s_addImportsAnnotation;

        private static SyntaxAnnotation? AddImportsAnnotation
        {
            get
            {
                if (!s_addImportsAnnotation.HasValue)
                {
                    var property = typeof(Simplifier).GetTypeInfo().GetDeclaredProperty("AddImportsAnnotation");
                    s_addImportsAnnotation = property?.GetValue(null) as SyntaxAnnotation;
                }

                return s_addImportsAnnotation.Value;
            }
        }

        public static SyntaxNode WithAddImportsAnnotation(this SyntaxNode syntaxNode)
        {
            if (AddImportsAnnotation is null)
            {
                return syntaxNode;
            }

            return syntaxNode.WithAdditionalAnnotations(AddImportsAnnotation);
        }
    }
}
