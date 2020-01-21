// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A <see cref="MissingAssemblySymbol"/> is a special kind of <see cref="AssemblySymbol"/> that represents
    /// an assembly that couldn't be found.
    /// </summary>
    internal class MissingAssemblySymbol : AssemblySymbol
    {
        protected readonly AssemblyIdentity identity;
        protected readonly MissingModuleSymbol moduleSymbol;

        private ImmutableArray<ModuleSymbol> _lazyModules;

        public MissingAssemblySymbol(AssemblyIdentity identity)
        {
            Debug.Assert(identity != null);
            this.identity = identity;
            moduleSymbol = new MissingModuleSymbol(this, 0);
        }

        internal sealed override bool IsMissing
        {
            get
            {
                return true;
            }
        }

        internal override bool IsLinked
        {
            get
            {
                return false;
            }
        }

        internal override Symbol GetDeclaredSpecialTypeMember(SpecialMember member)
        {
            return null;
        }

        public override AssemblyIdentity Identity
        {
            get
            {
                return identity;
            }
        }

        public override Version AssemblyVersionPattern => null;

        internal override ImmutableArray<byte> PublicKey
        {
            get { return Identity.PublicKey; }
        }

        public override ImmutableArray<ModuleSymbol> Modules
        {
            get
            {
                if (_lazyModules.IsDefault)
                {
                    _lazyModules = ImmutableArray.Create<ModuleSymbol>(moduleSymbol);
                }

                return _lazyModules;
            }
        }

        public override int GetHashCode()
        {
            return identity.GetHashCode();
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            return Equals(obj as MissingAssemblySymbol);
        }

        public bool Equals(MissingAssemblySymbol other)
        {
            if ((object)other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return identity.Equals(other.Identity);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        internal override void SetLinkedReferencedAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<AssemblySymbol> GetLinkedReferencedAssemblies()
        {
            return ImmutableArray<AssemblySymbol>.Empty;
        }

        internal override void SetNoPiaResolutionAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<AssemblySymbol> GetNoPiaResolutionAssemblies()
        {
            return ImmutableArray<AssemblySymbol>.Empty;
        }

        public sealed override NamespaceSymbol GlobalNamespace
        {
            get
            {
                return this.moduleSymbol.GlobalNamespace;
            }
        }

        public override ICollection<string> TypeNames
        {
            get
            {
                return SpecializedCollections.EmptyCollection<string>();
            }
        }

        public override ICollection<string> NamespaceNames
        {
            get
            {
                return SpecializedCollections.EmptyCollection<string>();
            }
        }

        internal override NamedTypeSymbol LookupTopLevelMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies, bool digThroughForwardedTypes)
        {
            var result = this.moduleSymbol.LookupTopLevelMetadataType(ref emittedName);
            Debug.Assert(result is MissingMetadataTypeSymbol);
            return result;
        }

        internal override NamedTypeSymbol GetDeclaredSpecialType(SpecialType type)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override bool AreInternalsVisibleToThisAssembly(AssemblySymbol other)
        {
            return false;
        }

        internal override IEnumerable<ImmutableArray<byte>> GetInternalsVisibleToPublicKeys(string simpleName)
        {
            return SpecializedCollections.EmptyEnumerable<ImmutableArray<byte>>();
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                return false;
            }
        }

        public override AssemblyMetadata GetMetadata() => null;
    }
}
