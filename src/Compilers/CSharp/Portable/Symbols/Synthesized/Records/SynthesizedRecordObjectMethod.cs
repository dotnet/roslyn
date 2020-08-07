// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Common base for ordinary methods overriding methods from object synthesized by compiler for records.
    /// </summary>
    internal abstract class SynthesizedRecordObjectMethod : SynthesizedRecordOrdinaryMethod
    {
        protected SynthesizedRecordObjectMethod(SourceMemberContainerTypeSymbol containingType, string name, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, name, hasBody: true, memberOffset, diagnostics)
        {
        }

        protected sealed override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, DiagnosticBag diagnostics)
        {
            const DeclarationModifiers result = DeclarationModifiers.Public | DeclarationModifiers.Override;
            Debug.Assert((result & ~allowedModifiers) == 0);
            return result;
        }

        protected sealed override void MethodChecks(DiagnosticBag diagnostics)
        {
            base.MethodChecks(diagnostics);
            VerifyOverridesMethodFromObject(this, ReturnType.SpecialType, diagnostics);
        }

        /// <summary>
        /// Returns true if reported an error
        /// </summary>
        internal static bool VerifyOverridesMethodFromObject(MethodSymbol overriding, SpecialType returnSpecialType, DiagnosticBag diagnostics)
        {
            bool reportAnError = false;

            if (!overriding.IsOverride)
            {
                reportAnError = true;
            }
            else
            {
                var overridden = overriding.OverriddenMethod?.OriginalDefinition;

                if (overridden is object && !(overridden.ContainingType is SourceMemberContainerTypeSymbol { IsRecord: true } && overridden.ContainingModule == overriding.ContainingModule))
                {
                    MethodSymbol leastOverridden = overriding.GetLeastOverriddenMethod(accessingTypeOpt: null);

                    reportAnError = leastOverridden.ReturnType.Equals(overriding.ReturnType, TypeCompareKind.AllIgnoreOptions) &&
                                    (leastOverridden.ContainingType.SpecialType != SpecialType.System_Object || returnSpecialType != leastOverridden.ReturnType.SpecialType);
                }
            }

            if (reportAnError)
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideMethodFromObject, overriding.Locations[0], overriding);
            }

            return reportAnError;
        }
    }
}
