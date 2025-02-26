// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        /// <summary>
        /// Annotates a syntax node representing a type so that any missing imports get automatically added. Does not work in any other kinds of nodes.
        /// </summary>
        /// <param name="syntaxNode">The type node to annotate.</param>
        /// <returns>The annotated type node.</returns>
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
