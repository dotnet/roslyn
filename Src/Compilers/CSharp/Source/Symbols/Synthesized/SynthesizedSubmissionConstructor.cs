// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedSubmissionConstructor : SynthesizedInstanceConstructor
    {
        private readonly ImmutableArray<ParameterSymbol> parameters;

        internal SynthesizedSubmissionConstructor(NamedTypeSymbol containingType, DiagnosticBag diagnostics)
            : base(containingType)
        {
            Debug.Assert(containingType.TypeKind == TypeKind.Submission);
            Debug.Assert(diagnostics != null);

            var compilation = containingType.DeclaringCompilation;

            var interactiveSessionType = compilation.GetWellKnownType(WellKnownType.Microsoft_CSharp_RuntimeHelpers_Session);
            var useSiteError = interactiveSessionType.GetUseSiteDiagnostic();
            if (useSiteError != null)
            {
                diagnostics.Add(useSiteError, NoLocation.Singleton);
            }

            // resolve return type:
            TypeSymbol returnType = compilation.GetTypeByReflectionType(compilation.SubmissionReturnType, diagnostics);

            this.parameters = ImmutableArray.Create<ParameterSymbol>(
                new SynthesizedParameterSymbol(this, interactiveSessionType, 0, RefKind.None, "session"),
                new SynthesizedParameterSymbol(this, returnType, 1, RefKind.Ref, "submissionResult")
            );
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return parameters; }
        }
    }
}
