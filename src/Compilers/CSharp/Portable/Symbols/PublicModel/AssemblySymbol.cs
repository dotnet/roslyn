// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal abstract class AssemblySymbol : Symbol, IAssemblySymbol
    {
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
                foreach (var module in UnderlyingAssemblySymbol.Modules)
                {
                    yield return module.GetPublicSymbol();
                }
            }
        }

        bool IAssemblySymbol.IsInteractive => UnderlyingAssemblySymbol.IsInteractive;

        AssemblyIdentity IAssemblySymbol.Identity => UnderlyingAssemblySymbol.Identity;

        ICollection<string> IAssemblySymbol.TypeNames => UnderlyingAssemblySymbol.TypeNames;

        ICollection<string> IAssemblySymbol.NamespaceNames => UnderlyingAssemblySymbol.NamespaceNames;

        bool IAssemblySymbol.MightContainExtensionMethods => UnderlyingAssemblySymbol.MightContainExtensionMethods;

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

                foreach (var key in myKeys)
                {
                    IVTConclusion conclusion = identity.PerformIVTCheck(assemblyWantingAccess.Identity.PublicKey, key);
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
