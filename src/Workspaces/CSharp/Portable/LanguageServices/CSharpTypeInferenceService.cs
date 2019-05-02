// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices.TypeInferenceService;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ITypeInferenceService), LanguageNames.CSharp), Shared]
    internal partial class CSharpTypeInferenceService : AbstractTypeInferenceService
    {
        public static readonly CSharpTypeInferenceService Instance = new CSharpTypeInferenceService();

#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public CSharpTypeInferenceService()
        {
        }

        protected override AbstractTypeInferrer CreateTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return new TypeInferrer(semanticModel, cancellationToken);
        }
    }
}
