// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    interface IPrecedenceService : ILanguageService
    {
        /// <summary>
        /// Returns the precedence of the given expression, mapped down to one of the 
        /// <see cref="PrecedenceKind"/> values.  The mapping is language specific.
        /// </summary>
        PrecedenceKind GetPrecedenceKind(int operatorPrecedence);

        /// <summary>
        /// Returns the precedence of this expression in a scale specific to a particular
        /// language.  These values cannot be compared across languages, but relates the 
        /// precedence of expressions in the same language.  A smaller value means lower
        /// precedence.
        /// </summary>
        int GetOperatorPrecedence(SyntaxNode expression);
    }

    internal abstract class AbstractPrecedenceService<
        TExpressionSyntax,
        TOperatorPrecedence> : IPrecedenceService
        where TExpressionSyntax : SyntaxNode
        where TOperatorPrecedence : struct
    {
        int IPrecedenceService.GetOperatorPrecedence(SyntaxNode expression)
            => (int)(object)this.GetOperatorPrecedence((TExpressionSyntax)expression);

        PrecedenceKind IPrecedenceService.GetPrecedenceKind(int operatorPrecedence)
             => this.GetPrecedenceKind((TOperatorPrecedence)(object)operatorPrecedence);

        public abstract TOperatorPrecedence GetOperatorPrecedence(TExpressionSyntax expression);
        public abstract PrecedenceKind GetPrecedenceKind(TOperatorPrecedence operatorPrecedence);
    }

    internal static class PrecedenceServiceExtensions
    {
        public static PrecedenceKind GetPrecedenceKind(this IPrecedenceService service, SyntaxNode expression)
            => service.GetPrecedenceKind(service.GetOperatorPrecedence(expression));
    }
}
