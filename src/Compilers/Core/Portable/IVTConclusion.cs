// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The result of <see cref="ISymbolExtensions.PerformIVTCheck(AssemblyIdentity, ImmutableArray{byte}, ImmutableArray{byte})"/>
    /// </summary>
    internal enum IVTConclusion
    {
        /// <summary>
        /// This indicates that friend access should be granted.
        /// </summary>
        Match,

        /// <summary>
        /// This indicates that friend access should be granted for the purposes of error recovery,
        /// but the program is wrong.
        ///
        /// That's because this indicates that a strong-named assembly has referred to a weak-named assembly 
        /// which has extended friend access to the strong-named assembly. This will ultimately 
        /// result in an error because strong-named assemblies may not refer to weak-named assemblies. 
        /// In Roslyn we give a new error, CS7029, before emit time. In the dev10 compiler we error at 
        /// emit time.
        /// </summary>
        OneSignedOneNot,

        /// <summary>
        /// This indicates that friend access should not be granted because the other assembly grants
        /// friend access to a strong-named assembly, and either this assembly is weak-named, or
        /// it is strong-named and the names don't match.
        /// </summary>
        PublicKeyDoesntMatch,

        /// <summary>
        /// This indicates that friend access should not be granted because the other assembly 
        /// does not name this assembly as a friend in any way whatsoever.
        /// </summary>
        NoRelationshipClaimed
    }
}
