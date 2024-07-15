// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageService;

/// <summary>
/// Helper service for telling you what type can be inferred to be viable in a particular
/// location in code.  This is useful for features that are starting from code that doesn't bind,
/// but would like to know type that code should be in the location that it can be found in.  For
/// example:
/// 
///   int i = Here(); 
/// 
/// If 'Here()' doesn't bind, then this class can be used to say that it is currently in a
/// location whose type has been inferred to be 'int' from the surrounding context.  Note: this
/// is simply a best effort guess.  'byte/short/etc.' as well as any user convertible types to
/// int would also be valid here, however 'int' seems the most reasonable when considering user
/// intuition.
/// </summary>
internal interface ITypeInferenceService : ILanguageService
{
    ImmutableArray<ITypeSymbol> InferTypes(SemanticModel semanticModel, SyntaxNode expression, string nameOpt, CancellationToken cancellationToken);
    ImmutableArray<ITypeSymbol> InferTypes(SemanticModel semanticModel, int position, string nameOpt, CancellationToken cancellationToken);

    ImmutableArray<TypeInferenceInfo> GetTypeInferenceInfo(SemanticModel semanticModel, int position, string nameOpt, CancellationToken cancellationToken);
    ImmutableArray<TypeInferenceInfo> GetTypeInferenceInfo(SemanticModel semanticModel, SyntaxNode expression, string nameOpt, CancellationToken cancellationToken);
}

internal readonly record struct TypeInferenceInfo(ITypeSymbol InferredType, bool IsParams)
{
    public TypeInferenceInfo(ITypeSymbol type) : this(type, IsParams: false)
    {
    }
}
