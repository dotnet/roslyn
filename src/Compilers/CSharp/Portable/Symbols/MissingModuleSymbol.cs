// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A <see cref="MissingModuleSymbol"/> is a special kind of <see cref="ModuleSymbol"/> that represents
    /// a module that couldn't be found.
    /// </summary>
    internal class MissingModuleSymbol : ModuleSymbol
    {
        protected readonly AssemblySymbol assembly;
        protected readonly int ordinal;
        protected readonly MissingNamespaceSymbol globalNamespace;

        public MissingModuleSymbol(AssemblySymbol assembly, int ordinal)
        {
            Debug.Assert((object)assembly != null);
            Debug.Assert(ordinal >= -1);

            this.assembly = assembly;
            this.ordinal = ordinal;
            globalNamespace = new MissingNamespaceSymbol(this);
        }

        internal override int Ordinal
        {
            get
            {
                return ordinal;
            }
        }

        internal override Machine Machine
        {
            get
            {
                return Machine.I386;
            }
        }

        internal override bool Bit32Required
        {
            get
            {
                return false;
            }
        }

        internal sealed override bool IsMissing
        {
            get
            {
                return true;
            }
        }

        public override string Name
        {
            get
            {
                // Once we switch to a non-hardcoded name, GetHashCode/Equals should be adjusted.
                return "<Missing Module>";
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return assembly;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return assembly;
            }
        }

        public override NamespaceSymbol GlobalNamespace
        {
            get
            {
                return globalNamespace;
            }
        }

        public override int GetHashCode()
        {
            return assembly.GetHashCode();
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            MissingModuleSymbol other = obj as MissingModuleSymbol;

            return (object)other != null && assembly.Equals(other.assembly, compareKind);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        internal override ICollection<string> NamespaceNames
        {
            get
            {
                return SpecializedCollections.EmptyCollection<string>();
            }
        }

        internal override ICollection<string> TypeNames
        {
            get
            {
                return SpecializedCollections.EmptyCollection<string>();
            }
        }

        internal override NamedTypeSymbol LookupTopLevelMetadataType(ref MetadataTypeName emittedName)
        {
            return new MissingMetadataTypeSymbol.TopLevel(this, ref emittedName);
        }

        internal override ImmutableArray<AssemblyIdentity> GetReferencedAssemblies()
        {
            return ImmutableArray<AssemblyIdentity>.Empty;
        }

        internal override ImmutableArray<AssemblySymbol> GetReferencedAssemblySymbols()
        {
            return ImmutableArray<AssemblySymbol>.Empty;
        }

        internal override void SetReferences(ModuleReferences<AssemblySymbol> moduleReferences, SourceAssemblySymbol originatingSourceAssemblyDebugOnly)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override bool HasUnifiedReferences
        {
            get { return false; }
        }

        internal override bool GetUnificationUseSiteDiagnostic(ref DiagnosticInfo result, TypeSymbol dependentType)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override bool HasAssemblyCompilationRelaxationsAttribute
        {
            get { return false; }
        }

        internal override bool HasAssemblyRuntimeCompatibilityAttribute
        {
            get { return false; }
        }

        internal override CharSet? DefaultMarshallingCharSet
        {
            get { return null; }
        }

        public override ModuleMetadata GetMetadata() => null;
    }

    internal sealed class MissingModuleSymbolWithName : MissingModuleSymbol
    {
        private readonly string _name;

        public MissingModuleSymbolWithName(AssemblySymbol assembly, string name)
            : base(assembly, ordinal: -1)
        {
            Debug.Assert(name != null);

            _name = name;
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(assembly.GetHashCode(), StringComparer.OrdinalIgnoreCase.GetHashCode(_name));
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            MissingModuleSymbolWithName other = obj as MissingModuleSymbolWithName;

            return (object)other != null && assembly.Equals(other.assembly, compareKind) && string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
