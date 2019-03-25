// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedSubmissionConstructor : SynthesizedInstanceConstructor
    {
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal SynthesizedSubmissionConstructor(NamedTypeSymbol containingType, DiagnosticBag diagnostics)
            : base(containingType)
        {
            Debug.Assert(containingType.TypeKind == TypeKind.Submission);
            Debug.Assert(diagnostics != null);

            var compilation = containingType.DeclaringCompilation;

            var submissionArrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Object));
            var useSiteError = submissionArrayType.GetUseSiteDiagnostic();
            if (useSiteError != null)
            {
                diagnostics.Add(useSiteError, NoLocation.Singleton);
            }

            _parameters = ImmutableArray.Create<ParameterSymbol>(
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(submissionArrayType), 0, RefKind.None, "submissionArray"));
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }
    }
}
