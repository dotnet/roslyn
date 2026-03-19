// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class DynamicSiteContainer : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol _topLevelMethod;

        internal DynamicSiteContainer(string name, MethodSymbol topLevelMethod, MethodSymbol containingMethod)
            : base(name,
                  (topLevelMethod.ContainingSymbol is NamedTypeSymbol { IsExtension: true } extensionType ? extensionType.TypeParameters : []).Concat(
                   TypeMap.ConcatMethodTypeParameters(containingMethod, stopAt: null)))
        {
            Debug.Assert(topLevelMethod != null);
            _topLevelMethod = topLevelMethod;
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                var containingSymbol = _topLevelMethod.ContainingSymbol;
                if (containingSymbol is NamedTypeSymbol { IsExtension: true })
                {
                    return containingSymbol.ContainingSymbol;
                }

                return containingSymbol;
            }
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Class; }
        }

        public sealed override bool AreLocalsZeroed
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool IsRecord => false;
        internal override bool IsRecordStruct => false;
        internal override bool HasPossibleWellKnownCloneMethod() => false;

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get { return true; }
        }

        IMethodSymbolInternal ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return _topLevelMethod; }
        }
    }
}
