// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
