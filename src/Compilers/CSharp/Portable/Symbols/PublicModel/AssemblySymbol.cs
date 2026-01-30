// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal abstract class AssemblySymbol : Symbol, IAssemblySymbol
    {
        private IEnumerable<IModuleSymbol> _lazyModules;

        internal abstract Symbols.AssemblySymbol UnderlyingAssemblySymbol { get; }

        INamespaceSymbol IAssemblySymbol.GlobalNamespace
        {
            get
            {
                return UnderlyingAssemblySymbol.GlobalNamespace.GetPublicSymbol();
            }
        }

        IEnumerable<IModuleSymbol> IAssemblySymbol.Modules
        {
            get
            {
                return InterlockedOperations.Initialize(
                    ref _lazyModules,
                    static self => self.UnderlyingAssemblySymbol.Modules.SelectAsArray(static module => module.GetPublicSymbol()),
                    this);
            }
        }

        bool IAssemblySymbol.IsInteractive => UnderlyingAssemblySymbol.IsInteractive;

        AssemblyIdentity IAssemblySymbol.Identity => UnderlyingAssemblySymbol.Identity;

        ICollection<string> IAssemblySymbol.TypeNames => UnderlyingAssemblySymbol.TypeNames;

        ICollection<string> IAssemblySymbol.NamespaceNames => UnderlyingAssemblySymbol.NamespaceNames;

        bool IAssemblySymbol.MightContainExtensionMethods => UnderlyingAssemblySymbol.MightContainExtensions;

        AssemblyMetadata IAssemblySymbol.GetMetadata() => UnderlyingAssemblySymbol.GetMetadata();

        INamedTypeSymbol IAssemblySymbol.ResolveForwardedType(string fullyQualifiedMetadataName)
        {
            return UnderlyingAssemblySymbol.ResolveForwardedType(fullyQualifiedMetadataName).GetPublicSymbol();
        }

        ImmutableArray<INamedTypeSymbol> IAssemblySymbol.GetForwardedTypes()
        {
            return UnderlyingAssemblySymbol.GetAllTopLevelForwardedTypes().Select(t => t.GetPublicSymbol()).
                   OrderBy(t => t.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat)).AsImmutable();
        }

        bool IAssemblySymbol.GivesAccessTo(IAssemblySymbol assemblyWantingAccess)
        {
            if (Equals(this, assemblyWantingAccess))
            {
                return true;
            }

            var myKeys = UnderlyingAssemblySymbol.GetInternalsVisibleToPublicKeys(assemblyWantingAccess.Name);

            if (myKeys.Any())
            {
                // We have an easy out here. Suppose the assembly wanting access is 
                // being compiled as a module. You can only strong-name an assembly. So we are going to optimistically 
                // assume that it is going to be compiled into an assembly with a matching strong name, if necessary.
                if (assemblyWantingAccess.IsNetModule())
                {
                    return true;
                }

                AssemblyIdentity identity = UnderlyingAssemblySymbol.Identity;

                // Avoid using the identity to obtain the public key if possible to avoid the allocations associated
                // with identity creation
                ImmutableArray<byte> publicKey = (assemblyWantingAccess is AssemblySymbol assemblyWantingAccessAssemblySymbol)
                    ? assemblyWantingAccessAssemblySymbol.UnderlyingAssemblySymbol.PublicKey.NullToEmpty()
                    : assemblyWantingAccess.Identity.PublicKey;

                foreach (var key in myKeys)
                {
                    IVTConclusion conclusion = identity.PerformIVTCheck(publicKey, key);
                    Debug.Assert(conclusion != IVTConclusion.NoRelationshipClaimed);
                    if (conclusion == IVTConclusion.Match || conclusion == IVTConclusion.OneSignedOneNot)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

#nullable enable
        INamedTypeSymbol? IAssemblySymbol.GetTypeByMetadataName(string metadataName)
        {
            return UnderlyingAssemblySymbol.GetTypeByMetadataName(metadataName).GetPublicSymbol();
        }
#nullable disable

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitAssembly(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitAssembly(this);
        }

        protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAssembly(this, argument);
        }

        #endregion
    }
}
