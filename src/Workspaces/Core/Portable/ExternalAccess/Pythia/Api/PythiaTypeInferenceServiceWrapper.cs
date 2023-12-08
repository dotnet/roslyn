// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal readonly struct PythiaTypeInferenceServiceWrapper
    {
        internal readonly ITypeInferenceService UnderlyingObject;

        internal PythiaTypeInferenceServiceWrapper(ITypeInferenceService underlyingObject)
            => UnderlyingObject = underlyingObject;

        public static PythiaTypeInferenceServiceWrapper Create(Document document)
            => new(document.GetRequiredLanguageService<ITypeInferenceService>());

        public ImmutableArray<ITypeSymbol> InferTypes(SemanticModel semanticModel, int position, string? name, CancellationToken cancellationToken)
            => UnderlyingObject.InferTypes(semanticModel, position, name, cancellationToken);
    }
}
