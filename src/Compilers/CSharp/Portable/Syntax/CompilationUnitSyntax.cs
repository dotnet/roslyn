// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class CompilationUnitSyntax : CSharpSyntaxNode, ICompilationUnitSyntax
    {
        /// <summary>
        /// Returns #r directives specified in the compilation.
        /// </summary>
        public IList<ReferenceDirectiveTriviaSyntax> GetReferenceDirectives()
        {
            return GetReferenceDirectives(null);
        }

        internal IList<ReferenceDirectiveTriviaSyntax> GetReferenceDirectives(Func<ReferenceDirectiveTriviaSyntax, bool>? filter)
        {
            if (!this.ContainsDirectives)
                return SpecializedCollections.EmptyList<ReferenceDirectiveTriviaSyntax>();

            // #r directives are always on the first token of the compilation unit.
            var firstToken = (SyntaxNodeOrToken)this.GetFirstToken(includeZeroWidth: true);
            return firstToken.GetDirectives(filter);
        }

        /// <summary>
        /// Returns #load directives specified in the compilation.
        /// </summary>
        public IList<LoadDirectiveTriviaSyntax> GetLoadDirectives()
        {
            if (!this.ContainsDirectives)
                return SpecializedCollections.EmptyList<LoadDirectiveTriviaSyntax>();

            // #load directives are always on the first token of the compilation unit.
            var firstToken = (SyntaxNodeOrToken)this.GetFirstToken(includeZeroWidth: true);
            return firstToken.GetDirectives<LoadDirectiveTriviaSyntax>(filter: null);
        }

        internal bool HasReferenceDirectives
            // #r and #load directives are always on the first token of the compilation unit.
            => HasFirstTokenDirective(static n => n is ReferenceDirectiveTriviaSyntax);

        internal bool HasLoadDirectives
            // #r and #load directives are always on the first token of the compilation unit.
            => HasFirstTokenDirective(static n => n is LoadDirectiveTriviaSyntax);

        private bool HasFirstTokenDirective(Func<SyntaxNode, bool> predicate)
        {
            if (this.ContainsDirectives)
            {
                var firstToken = this.GetFirstToken(includeZeroWidth: true);
                if (firstToken.ContainsDirectives)
                {
                    foreach (var trivia in firstToken.LeadingTrivia)
                    {
                        if (trivia.GetStructure() is { } structure &&
                            predicate(structure))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
