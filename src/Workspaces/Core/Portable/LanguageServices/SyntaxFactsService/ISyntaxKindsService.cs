﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    /// <summary>
    /// Provides a uniform view of SyntaxKinds over C# and VB for constructs they have
    /// in common.
    /// </summary>
    internal interface ISyntaxKindsService : ILanguageService
    {
        int DotToken { get; }
        int QuestionToken { get; }

        int IfKeyword { get; }

        /// <summary>
        /// A short-circuiting logical 'and'. In C#, 'LogicalAndExpression'. In VB, 'AndAlsoExpression'.
        /// </summary>
        int LogicalAndExpression { get; }

        /// <summary>
        /// A short-circuiting logical 'or'. In C#, 'LogicalOrExpression'. In VB, 'OrElseExpression'.
        /// </summary>
        int LogicalOrExpression { get; }
    }

    internal abstract class AbstractSyntaxKindsService : ISyntaxKindsService
    {
        public abstract int DotToken { get; }
        public abstract int QuestionToken { get; }

        public abstract int IfKeyword { get; }

        public abstract int LogicalAndExpression { get; }
        public abstract int LogicalOrExpression { get; }
    }
}
