// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Common base for ordinary methods overriding methods from object synthesized by compiler for records.
    /// </summary>
    internal abstract class SynthesizedRecordObjectMethod : SynthesizedRecordOrdinaryMethod
    {
        protected SynthesizedRecordObjectMethod(SourceMemberContainerTypeSymbol containingType, string name, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, name, memberOffset, diagnostics)
        {
        }

        protected sealed override void MethodChecks(DiagnosticBag diagnostics)
        {
            base.MethodChecks(diagnostics);

            var overridden = OverriddenMethod?.OriginalDefinition;

            if (overridden is null || (overridden is SynthesizedRecordObjEquals && overridden.DeclaringCompilation == DeclaringCompilation))
            {
                return;
            }

            MethodSymbol leastOverridden = GetLeastOverriddenMethod(accessingTypeOpt: null);

            if (leastOverridden is object &&
                leastOverridden.ReturnType.SpecialType == ReturnType.SpecialType &&
                leastOverridden.ContainingType.SpecialType != SpecialType.System_Object)
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideMethodFromObject, Locations[0], this);
            }
        }
    }
}
