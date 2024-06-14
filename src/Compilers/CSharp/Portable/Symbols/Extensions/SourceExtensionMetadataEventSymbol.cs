// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceExtensionMetadataEventSymbol : RewrittenExtensionEventSymbol
    {
        public SourceExtensionMetadataEventSymbol(EventSymbol sourceEvent) : base(sourceEvent)
        {
            Debug.Assert(!sourceEvent.IsStatic);
            Debug.Assert(!sourceEvent.MustCallMethodsDirectly);
            Debug.Assert(sourceEvent.ContainingSymbol is SourceExtensionTypeSymbol);
        }

        public override bool IsStatic => true;
        public override bool RequiresInstanceReceiver => false;

        public override MethodSymbol? AddMethod
        {
            get
            {
                if (UnderlyingEvent.AddMethod is { } accessor)
                {
                    return (MethodSymbol?)ContainingType.TryGetCorrespondingStaticMetadataExtensionMember(accessor);
                }

                return null;
            }
        }

        public override MethodSymbol? RemoveMethod
        {
            get
            {
                if (UnderlyingEvent.RemoveMethod is { } accessor)
                {
                    return (MethodSymbol?)ContainingType.TryGetCorrespondingStaticMetadataExtensionMember(accessor);
                }

                return null;
            }
        }
    }
}
