// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class PEExtensionInstanceEventSymbol : RewrittenExtensionEventSymbol
    {
        private readonly PEExtensionInstanceMethodSymbol? _addMethod;
        private readonly PEExtensionInstanceMethodSymbol? _removeMethod;

        public PEExtensionInstanceEventSymbol(PEEventSymbol metadataEvent, PEExtensionInstanceMethodSymbol? addMethod, PEExtensionInstanceMethodSymbol? removeMethod) : base(metadataEvent)
        {
            Debug.Assert(metadataEvent.IsStatic);
            _addMethod = addMethod;
            _removeMethod = removeMethod;
        }

        public override bool IsStatic => false;
        public override bool RequiresInstanceReceiver => true;

        public override MethodSymbol? AddMethod => _addMethod;

        public override MethodSymbol? RemoveMethod => _removeMethod;
    }
}
