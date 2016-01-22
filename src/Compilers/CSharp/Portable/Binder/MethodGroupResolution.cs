// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Packages up the various parts returned when resolving a method group. 
    /// </summary>
    internal struct MethodGroupResolution
    {
        public readonly MethodGroup MethodGroup;
        public readonly Symbol OtherSymbol;
        public readonly OverloadResolutionResult<MethodSymbol> OverloadResolutionResult;
        public readonly AnalyzedArguments AnalyzedArguments;
        public readonly ImmutableArray<Diagnostic> Diagnostics;
        public readonly LookupResultKind ResultKind;
        public readonly bool ExtensionMethodsOfSameViabilityAreAvailable;

        public MethodGroupResolution(MethodGroup methodGroup, ImmutableArray<Diagnostic> diagnostics)
            : this(methodGroup, null, null, null, methodGroup.ResultKind, diagnostics, false)
        {
        }

        public MethodGroupResolution(
            MethodGroup methodGroup,
            OverloadResolutionResult<MethodSymbol> overloadResolutionResult,
            AnalyzedArguments analyzedArguments,
            ImmutableArray<Diagnostic> diagnostics)
            : this(methodGroup, null, overloadResolutionResult, analyzedArguments, methodGroup.ResultKind, diagnostics, false)
        {
        }

        public MethodGroupResolution(Symbol otherSymbol, LookupResultKind resultKind, ImmutableArray<Diagnostic> diagnostics)
            : this(null, otherSymbol, null, null, resultKind, diagnostics, false)
        {
        }

        public MethodGroupResolution(
            MethodGroup methodGroup,
            Symbol otherSymbol,
            OverloadResolutionResult<MethodSymbol> overloadResolutionResult,
            AnalyzedArguments analyzedArguments,
            LookupResultKind resultKind,
            ImmutableArray<Diagnostic> diagnostics,
            bool extensionMethodsOfSameViabilityAreAvailable)
        {
            Debug.Assert((methodGroup == null) || (methodGroup.Methods.Count > 0));
            Debug.Assert((methodGroup == null) || ((object)otherSymbol == null));
            // Methods should be represented in the method group.
            Debug.Assert(((object)otherSymbol == null) || (otherSymbol.Kind != SymbolKind.Method));
            Debug.Assert(resultKind != LookupResultKind.Ambiguous); // HasAnyApplicableMethod is expecting Viable methods.
            Debug.Assert(!diagnostics.IsDefault);
            Debug.Assert(!extensionMethodsOfSameViabilityAreAvailable || methodGroup == null || !methodGroup.IsExtensionMethodGroup);

            this.MethodGroup = methodGroup;
            this.OtherSymbol = otherSymbol;
            this.OverloadResolutionResult = overloadResolutionResult;
            this.AnalyzedArguments = analyzedArguments;
            this.ResultKind = resultKind;
            this.Diagnostics = diagnostics;
            this.ExtensionMethodsOfSameViabilityAreAvailable = extensionMethodsOfSameViabilityAreAvailable;
        }

        public bool IsEmpty
        {
            get { return (this.MethodGroup == null) && ((object)this.OtherSymbol == null); }
        }

        public bool HasAnyErrors
        {
            get { return this.Diagnostics.HasAnyErrors(); }
        }

        public bool HasAnyApplicableMethod
        {
            get
            {
                return (this.MethodGroup != null) &&
                    (this.ResultKind == LookupResultKind.Viable) &&
                    ((this.OverloadResolutionResult == null) || this.OverloadResolutionResult.HasAnyApplicableMember);
            }
        }

        public bool IsExtensionMethodGroup
        {
            get { return (this.MethodGroup != null) && this.MethodGroup.IsExtensionMethodGroup; }
        }

        public void Free()
        {
            if (this.MethodGroup != null)
            {
                if (this.MethodGroup.IsExtensionMethodGroup)
                {
                    // Arguments are only owned by this instance if the arguments are for
                    // extension methods. Otherwise, the caller supplied the arguments.
                    if (this.AnalyzedArguments != null)
                    {
                        this.AnalyzedArguments.Free();
                    }
                }
                this.MethodGroup.Free();
            }
            if (this.OverloadResolutionResult != null)
            {
                this.OverloadResolutionResult.Free();
            }
        }
    }
}
