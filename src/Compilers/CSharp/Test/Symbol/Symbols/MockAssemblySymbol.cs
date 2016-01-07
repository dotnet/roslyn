// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        internal override ImmutableArray<byte> PublicKey
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

        public override MetadataId MetadataId => null;
    }
}
