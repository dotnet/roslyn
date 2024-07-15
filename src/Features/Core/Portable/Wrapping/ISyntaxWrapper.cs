// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Indentation;

namespace Microsoft.CodeAnalysis.Wrapping;

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
        Document document, int position, SyntaxNode node, SyntaxWrappingOptions options, bool containsSyntaxError, CancellationToken cancellationToken);
}
