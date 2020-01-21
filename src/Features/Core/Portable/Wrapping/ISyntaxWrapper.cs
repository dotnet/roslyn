// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Wrapping
{
    /// <summary>
    /// Interface for types that can wrap some sort of language construct.
    /// </summary>
    /// <remarks>
    /// The main refactoring
    /// keeps walking up nodes until it finds the first IWrapper that can handle that node.  That
    /// way the user is not inundated with lots of wrapping options for all the nodes their cursor
    /// is contained within.
    /// </remarks>
    /// <seealso cref="AbstractWrappingCodeRefactoringProvider"/>
    internal interface ISyntaxWrapper
    {
        /// <summary>
        /// Returns the <see cref="ICodeActionComputer"/> that produces wrapping code actions for the  
        /// node passed in.  Returns <see langword="null"/> if this Wrapper cannot wrap this node.
        /// </summary>
        Task<ICodeActionComputer> TryCreateComputerAsync(
            Document document, int position, SyntaxNode node, CancellationToken cancellationToken);
    }
}
