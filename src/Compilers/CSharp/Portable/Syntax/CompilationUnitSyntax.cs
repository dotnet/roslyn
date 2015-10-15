// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class CompilationUnitSyntax : CSharpSyntaxNode
    {
        /// <summary>
        /// Returns #r directives specified in the compilation.
        /// </summary>
        public IList<ReferenceDirectiveTriviaSyntax> GetReferenceDirectives()
        {
            return GetReferenceDirectives(null);
        }

        internal IList<ReferenceDirectiveTriviaSyntax> GetReferenceDirectives(Func<ReferenceDirectiveTriviaSyntax, bool> filter)
        {
            // #r directives are always on the first token of the compilation unit.
            var firstToken = (SyntaxNodeOrToken)this.GetFirstToken(includeZeroWidth: true);
            return firstToken.GetDirectives<ReferenceDirectiveTriviaSyntax>(filter);
        }

        /// <summary>
        /// Returns #load directives specified in the compilation.
        /// </summary>
        public IList<LoadDirectiveTriviaSyntax> GetLoadDirectives()
        {
            // #load directives are always on the first token of the compilation unit.
            var firstToken = (SyntaxNodeOrToken)this.GetFirstToken(includeZeroWidth: true);
            return firstToken.GetDirectives<LoadDirectiveTriviaSyntax>(filter: null);
        }

        internal Syntax.InternalSyntax.DirectiveStack GetConditionalDirectivesStack()
        {
            IEnumerable<DirectiveTriviaSyntax> directives = this.GetDirectives(filter: IsActiveConditionalDirective);
            var directiveStack = Syntax.InternalSyntax.DirectiveStack.Empty;
            foreach (DirectiveTriviaSyntax directive in directives)
            {
                var internalDirective = (Syntax.InternalSyntax.DirectiveTriviaSyntax)directive.Green;
                directiveStack = internalDirective.ApplyDirectives(directiveStack);
            }
            return directiveStack;
        }

        private static bool IsActiveConditionalDirective(DirectiveTriviaSyntax directive)
        {
            switch (directive.Kind())
            {
                case SyntaxKind.DefineDirectiveTrivia:
                    return ((DefineDirectiveTriviaSyntax)directive).IsActive;
                case SyntaxKind.UndefDirectiveTrivia:
                    return ((UndefDirectiveTriviaSyntax)directive).IsActive;
                default:
                    return false;
            }
        }
    }
}
