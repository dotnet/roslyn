// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Packages up the various parts returned when resolving a method group.
    ///
    /// A resolution may find a non-method group result (an extension member).
    /// That is represented as a viable ResultKind with an OtherSymbol, but no MethodGroup.
    /// </summary>
    internal readonly struct MethodGroupResolution
    {
        public readonly MethodGroup MethodGroup;
        public readonly Symbol OtherSymbol;
        public readonly OverloadResolutionResult<MethodSymbol> OverloadResolutionResult;
        public readonly AnalyzedArguments AnalyzedArguments;
        public readonly ReadOnlyBindingDiagnostic<AssemblySymbol> Diagnostics;
        public readonly LookupResultKind ResultKind;

        public MethodGroupResolution(MethodGroup methodGroup, ReadOnlyBindingDiagnostic<AssemblySymbol> diagnostics)
            : this(methodGroup, otherSymbol: null, overloadResolutionResult: null, analyzedArguments: null, methodGroup.ResultKind, diagnostics)
        {
        }

        public MethodGroupResolution(Symbol otherSymbol, LookupResultKind resultKind, ReadOnlyBindingDiagnostic<AssemblySymbol> diagnostics)
            : this(methodGroup: null, otherSymbol, overloadResolutionResult: null, analyzedArguments: null, resultKind, diagnostics)
        {
            Debug.Assert(resultKind != LookupResultKind.Viable || otherSymbol.ContainingType.IsExtension);
        }

        public MethodGroupResolution(
            MethodGroup methodGroup,
            Symbol otherSymbol,
            OverloadResolutionResult<MethodSymbol> overloadResolutionResult,
            AnalyzedArguments analyzedArguments,
            LookupResultKind resultKind,
            ReadOnlyBindingDiagnostic<AssemblySymbol> diagnostics)
        {
            Debug.Assert((methodGroup == null) || (methodGroup.Methods.Count > 0));
            Debug.Assert((methodGroup == null) || ((object)otherSymbol == null));
            // Methods should be represented in the method group.
            Debug.Assert(((object)otherSymbol == null) || (otherSymbol.Kind != SymbolKind.Method));
            Debug.Assert(resultKind != LookupResultKind.Ambiguous); // HasAnyApplicableMethod is expecting Viable methods.
            Debug.Assert(!diagnostics.Diagnostics.IsDefault);
            Debug.Assert(!diagnostics.Dependencies.IsDefault);
            Debug.Assert(overloadResolutionResult == null || methodGroup != null);

            // If we have an extension member (viable result but which isn't a method group), it is stored in other symbol.
            Debug.Assert((otherSymbol is not null) || !(ResultKind == LookupResultKind.Viable && MethodGroup is null));

            this.MethodGroup = methodGroup;
            this.OtherSymbol = otherSymbol;
            this.OverloadResolutionResult = overloadResolutionResult;
            this.AnalyzedArguments = analyzedArguments;
            this.ResultKind = resultKind;
            this.Diagnostics = diagnostics;
        }

        public bool IsEmpty
        {
            get { return (this.MethodGroup == null) && ((object)this.OtherSymbol == null); }
        }

        public bool HasAnyErrors
        {
            get { return this.Diagnostics.Diagnostics.HasAnyErrors(); }
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

#nullable enable
        public bool IsExtensionMember([NotNullWhen(true)] out Symbol? extensionMember)
        {
            bool isExtensionMember = ResultKind == LookupResultKind.Viable && MethodGroup is null;
            extensionMember = isExtensionMember ? OtherSymbol : null;
            Debug.Assert((extensionMember is not null) || !isExtensionMember);

            return isExtensionMember;
        }
#nullable disable

        public bool IsLocalFunctionInvocation =>
            MethodGroup?.Methods.Count == 1 && // Local functions cannot be overloaded
            MethodGroup.Methods[0].MethodKind == MethodKind.LocalFunction;

        public void Free()
        {
            this.AnalyzedArguments?.Free();
            this.MethodGroup?.Free();
            this.OverloadResolutionResult?.Free();
        }
    }
}
