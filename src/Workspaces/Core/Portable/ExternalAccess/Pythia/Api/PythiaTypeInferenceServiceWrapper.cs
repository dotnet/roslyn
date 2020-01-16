// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal readonly struct PythiaTypeInferenceServiceWrapper
    {
        internal readonly ITypeInferenceService UnderlyingObject;

        internal PythiaTypeInferenceServiceWrapper(ITypeInferenceService underlyingObject)
        {
            UnderlyingObject = underlyingObject;
        }

        public static PythiaTypeInferenceServiceWrapper Create(Document document)
            => new PythiaTypeInferenceServiceWrapper(document.GetRequiredLanguageService<ITypeInferenceService>());

        public ImmutableArray<ITypeSymbol> InferTypes(SemanticModel semanticModel, int position, string? name, CancellationToken cancellationToken)
            => UnderlyingObject.InferTypes(semanticModel, position, name, cancellationToken);
    }
}
