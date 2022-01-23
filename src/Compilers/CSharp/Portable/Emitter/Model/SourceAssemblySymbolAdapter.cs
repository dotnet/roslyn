// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceAssemblySymbol
    {
        internal IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder, bool emittingRefAssembly, bool emittingAssemblyAttributesInNetModule)
        {
            CheckDefinitionInvariant();

            ImmutableArray<CSharpAttributeData> userDefined = this.GetAttributes();
            ArrayBuilder<SynthesizedAttributeData> synthesized = null;
            this.AddSynthesizedAttributes(moduleBuilder, ref synthesized);

            if (emittingRefAssembly && !HasReferenceAssemblyAttribute)
            {
                var referenceAssemblyAttribute = this.DeclaringCompilation
                    .TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_ReferenceAssemblyAttribute__ctor, isOptionalUse: true);
                Symbol.AddSynthesizedAttribute(ref synthesized, referenceAssemblyAttribute);
            }

            // Note that callers of this method (CCI and ReflectionEmitter) have to enumerate
            // all items of the returned iterator, otherwise the synthesized ArrayBuilder may leak.
            return GetCustomAttributesToEmit(userDefined, synthesized, isReturnType: false, emittingAssemblyAttributesInNetModule: emittingAssemblyAttributesInNetModule);
        }
    }
}
