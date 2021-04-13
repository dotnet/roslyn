// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedSubmissionConstructor : SynthesizedInstanceConstructor
    {
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal SynthesizedSubmissionConstructor(NamedTypeSymbol containingType, BindingDiagnosticBag diagnostics)
            : base(containingType)
        {
            Debug.Assert(containingType.TypeKind == TypeKind.Submission);
            Debug.Assert(diagnostics != null);

            var compilation = containingType.DeclaringCompilation;

            var submissionArrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Object));
            var useSiteInfo = submissionArrayType.GetUseSiteInfo();
            diagnostics.Add(useSiteInfo, NoLocation.Singleton);

            _parameters = ImmutableArray.Create<ParameterSymbol>(
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(submissionArrayType), 0, RefKind.None, "submissionArray"));
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }
    }
}
