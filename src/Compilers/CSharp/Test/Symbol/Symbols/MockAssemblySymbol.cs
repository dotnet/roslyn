// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal class MockAssemblySymbol : NonMissingAssemblySymbol
    {
        private readonly string _name;

        public MockAssemblySymbol(string name)
        {
            _name = name;
        }

        public override AssemblyIdentity Identity
        {
            get { return new AssemblyIdentity(_name); }
        }

        public override Version AssemblyVersionPattern => null;

        internal override ImmutableArray<byte> PublicKey
        {
            get { throw new NotImplementedException(); }
        }

        internal override TypeConversions TypeConversions
        {
            get { throw new NotImplementedException(); }
        }

        public override ImmutableArray<ModuleSymbol> Modules
        {
            get { return ImmutableArray.Create<ModuleSymbol>(); }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray.Create<Location>(); }
        }

        internal override NamedTypeSymbol GetDeclaredSpecialType(SpecialType type)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<AssemblySymbol> GetNoPiaResolutionAssemblies()
        {
            return default(ImmutableArray<AssemblySymbol>);
        }

        internal override ImmutableArray<AssemblySymbol> GetLinkedReferencedAssemblies()
        {
            return default(ImmutableArray<AssemblySymbol>);
        }

        internal override bool IsLinked
        {
            get { return false; }
        }

        internal override void SetLinkedReferencedAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            throw new NotImplementedException();
        }

        internal override void SetNoPiaResolutionAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            throw new NotImplementedException();
        }

        internal override bool AreInternalsVisibleToThisAssembly(AssemblySymbol other)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<ImmutableArray<byte>> GetInternalsVisibleToPublicKeys(string simpleName)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<string> GetInternalsVisibleToAssemblyNames()
        {
            throw new NotImplementedException();
        }

        public override ICollection<string> TypeNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ICollection<string> NamespaceNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool MightContainExtensionMethods
        {
            get { return true; }
        }

        internal override NamedTypeSymbol TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies)
        {
            return null;
        }

        public override AssemblyMetadata GetMetadata() => null;

        internal override IEnumerable<NamedTypeSymbol> GetAllTopLevelForwardedTypes()
        {
            throw new NotImplementedException();
        }

#nullable enable
        internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
            => null;
    }
}
