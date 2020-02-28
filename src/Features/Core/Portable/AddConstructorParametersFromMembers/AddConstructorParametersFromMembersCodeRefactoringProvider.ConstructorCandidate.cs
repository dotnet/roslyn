// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider
    {
        private readonly struct ConstructorCandidate
        {
            public readonly IMethodSymbol Constructor;
            public readonly ImmutableArray<ISymbol> MissingMembers;
            public readonly ImmutableArray<IParameterSymbol> MissingParameters;

            public ConstructorCandidate(IMethodSymbol constructor, ImmutableArray<ISymbol> missingMembers, ImmutableArray<IParameterSymbol> missingParameters)
            {
                Constructor = constructor;
                MissingMembers = missingMembers;
                MissingParameters = missingParameters;
            }
        }
    }
}
