// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [ImportingConstructor]
        public CSharpTypeInferenceService()
        {
        }

        protected override AbstractTypeInferrer CreateTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return new TypeInferrer(semanticModel, cancellationToken);
        }
    }
}
