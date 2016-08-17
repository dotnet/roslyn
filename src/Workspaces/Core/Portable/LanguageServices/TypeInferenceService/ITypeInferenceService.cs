// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
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
        IEnumerable<ITypeSymbol> InferTypes(SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken);
        IEnumerable<ITypeSymbol> InferTypes(SemanticModel semanticModel, int position, CancellationToken cancellationToken);

        IEnumerable<TypeInferenceInfo> GetTypeInferenceInfo(SemanticModel semanticModel, int position, CancellationToken cancellationToken);

        IEnumerable<TypeInferenceInfo> GetTypeInferenceInfo(SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken);
    }

    internal struct TypeInferenceInfo
    {
        public TypeInferenceInfo(ITypeSymbol type)
        {
            InferredType = type;
            IsParams = false;
        }

        public TypeInferenceInfo(ITypeSymbol type, bool isParams)
        {
            InferredType = type;
            IsParams = isParams;
        }

        public ITypeSymbol InferredType { get; }
        public bool IsParams { get; }
    }
}
