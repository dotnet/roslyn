// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal static class SyntaxAnnotationExtensions
    {
        public static TSymbol AddAnnotationToSymbol<TSymbol>(
            this SyntaxAnnotation annotation,
            TSymbol symbol)
            where TSymbol : ISymbol
        {
            Contract.ThrowIfFalse(symbol is CodeGenerationSymbol);
            var codeGenSymbol = (CodeGenerationSymbol)(object)symbol;
            return (TSymbol)(object)codeGenSymbol.WithAdditionalAnnotations(annotation);
        }

        internal static SyntaxAnnotation[] CombineAnnotations(
            SyntaxAnnotation[] originalAnnotations,
            SyntaxAnnotation[] newAnnotations)
        {
            if (!originalAnnotations.IsNullOrEmpty())
            {
                // Make a new array (that includes the new annotations) and copy the original
                // annotations into it.
                var finalAnnotations = newAnnotations;
                Array.Resize(ref finalAnnotations, originalAnnotations.Length + newAnnotations.Length);
                Array.Copy(originalAnnotations, 0, finalAnnotations, newAnnotations.Length, originalAnnotations.Length);

                return finalAnnotations;
            }

            return newAnnotations;
        }
    }
}
