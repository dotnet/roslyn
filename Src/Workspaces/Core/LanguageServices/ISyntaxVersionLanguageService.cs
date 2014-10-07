// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    /// <summary>
    /// A service that computes the syntactic version of a syntax tree
    /// </summary>
    internal interface ISyntaxVersionLanguageService : ILanguageService
    {
        /// <summary>
        /// Computes a hash corresponding to the public declarations in the syntax tree.
        /// Interior regions like method bodies, or trivia are not included.
        /// </summary>
        int ComputePublicHash(SyntaxNode root, CancellationToken cancellationToken);
    }
}