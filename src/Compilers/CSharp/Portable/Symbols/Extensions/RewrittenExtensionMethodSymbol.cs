// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class RewrittenExtensionMethodSymbol : RewrittenMethodSymbol
    {
        protected RewrittenExtensionMethodSymbol(MethodSymbol originalMethod) : base(originalMethod, TypeMap.Empty)
        {
            Debug.Assert(originalMethod.ContainingType.GetExtendedTypeNoUseSiteDiagnostics(null) is not null);
        }

        public sealed override bool IsExtensionMethod => false;
        public sealed override bool IsVirtual => false;

        public sealed override bool IsOverride => false;
        public sealed override bool IsAbstract => false;
        public sealed override bool IsSealed => false;

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;
        internal sealed override bool IsMetadataFinal => false;
        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal sealed override bool IsAccessCheckedOnOverride => false;

        // PROTOTYPE(roles): Do we want to support extern/external instance methods
        public sealed override bool IsExtern => false;
        public sealed override DllImportData? GetDllImportData() => null;
        internal sealed override bool IsExternal => false;

        // PROTOTYPE(roles): How doc comments are supposed to work? GetDocumentationCommentXml

        // PROTOTYPE(roles): Might need to adjust if we will support 'readonly' methods
        internal sealed override bool IsDeclaredReadOnly => false;

        // PROTOTYPE(roles): Are we going to support UnscopedRefAttribute? It should be moved to the 'this' parameter and back then. 
        //internal sealed override bool HasUnscopedRefAttribute => false;

        public sealed override Symbol ContainingSymbol => _originalMethod.ContainingSymbol;

        internal sealed override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            _originalMethod.AddSynthesizedAttributes(moduleBuilder, ref attributes);
        }
    }
}
