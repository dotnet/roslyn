// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal interface IMembersPullerService : ILanguageService
    {
        /// <summary>
        /// Return the CodeAction to pull <paramref name="selectedMember"/> up to destinationType. If the pulling will cause error, it will return null.
        /// </summary>
        CodeAction? TryComputeCodeAction(
            Document document,
            ISymbol selectedMember,
            INamedTypeSymbol destination);

        /// <summary>
        /// Return the changed solution if all changes in pullMembersUpOptions are applied.
        /// </summary>
        /// <param name="pullMembersUpOptions">Contains the members to pull up and all the fix operations</param>
        Task<Solution> PullMembersUpAsync(
            Document document,
            PullMembersUpOptions pullMembersUpOptions,
            CancellationToken cancellationToken);
    }
}
