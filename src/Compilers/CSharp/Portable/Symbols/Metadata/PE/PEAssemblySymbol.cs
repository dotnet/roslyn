// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// Represents an assembly imported from a PE.
    /// </summary>
    internal sealed class PEAssemblySymbol : MetadataOrSourceAssemblySymbol
    {
        /// <summary>
        /// An Assembly object providing metadata for the assembly.
        /// </summary>
        private readonly PEAssembly _assembly;

        /// <summary>
        /// A DocumentationProvider that provides XML documentation comments for this assembly.
        /// </summary>
        private readonly DocumentationProvider _documentationProvider;

        /// <summary>
        /// The list of contained PEModuleSymbol objects.
        /// The list doesn't use type ReadOnlyCollection(Of PEModuleSymbol) so that we
        /// can return it from Modules property as is.
        /// </summary>
        private readonly ImmutableArray<ModuleSymbol> _modules;

        /// <summary>
        /// An array of assemblies involved in canonical type resolution of
        /// NoPia local types defined within this assembly. In other words, all 
        /// references used by a compilation referencing this assembly.
        /// The array and its content is provided by ReferenceManager and must not be modified.
        /// </summary>
        private ImmutableArray<AssemblySymbol> _noPiaResolutionAssemblies;

        /// <summary>
        /// An array of assemblies referenced by this assembly, which are linked (/l-ed) by 
        /// each compilation that is using this AssemblySymbol as a reference. 
        /// If this AssemblySymbol is linked too, it will be in this array too.
        /// The array and its content is provided by ReferenceManager and must not be modified.
        /// </summary>
        private ImmutableArray<AssemblySymbol> _linkedReferencedAssemblies;

        /// <summary>
        /// Assembly is /l-ed by compilation that is using it as a reference.
        /// </summary>
        private readonly bool _isLinked;

        /// <summary>
        /// Assembly's custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        internal PEAssemblySymbol(PEAssembly assembly, DocumentationProvider documentationProvider, bool isLinked, MetadataImportOptions importOptions)
        {
            Debug.Assert(assembly != null);
            Debug.Assert(documentationProvider != null);
            _assembly = assembly;
            _documentationProvider = documentationProvider;

            var modules = new ModuleSymbol[assembly.Modules.Length];

            for (int i = 0; i < assembly.Modules.Length; i++)
            {
                modules[i] = new PEModuleSymbol(this, assembly.Modules[i], importOptions, i);
            }

            _modules = modules.AsImmutableOrNull();
            _isLinked = isLinked;
        }

        internal PEAssembly Assembly
        {
            get
            {
                return _assembly;
            }
        }

        public override AssemblyIdentity Identity
        {
            get
            {
                return _assembly.Identity;
            }
        }

        public override ImmutableArray<ModuleSymbol> Modules
        {
            get
            {
                return _modules;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.PrimaryModule.MetadataLocation.Cast<MetadataLocation, Location>();
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                if (this.MightContainExtensionMethods)
                {
                    this.PrimaryModule.LoadCustomAttributesFilterExtensions(_assembly.Handle,
                        ref _lazyCustomAttributes);
                }
                else
                {
                    this.PrimaryModule.LoadCustomAttributes(_assembly.Handle,
                        ref _lazyCustomAttributes);
                }
            }
            return _lazyCustomAttributes;
        }

        /// <summary>
        /// Look up the assembly to which the given metadata type is forwarded.
        /// </summary>
        /// <param name="emittedName"></param>
        /// <returns>
        /// The assembly to which the given type is forwarded or null, if there isn't one.
        /// </returns>
        /// <remarks>
        /// The returned assembly may also forward the type.
        /// </remarks>
        internal AssemblySymbol LookupAssemblyForForwardedMetadataType(ref MetadataTypeName emittedName)
        {
            // Look in the type forwarders of the primary module of this assembly, clr does not honor type forwarder
            // in non-primary modules.

            // Examine the type forwarders, but only from the primary module.
            return this.PrimaryModule.GetAssemblyForForwardedType(ref emittedName);
        }

        internal override NamedTypeSymbol TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies)
        {
            // Check if it is a forwarded type.
            var forwardedToAssembly = LookupAssemblyForForwardedMetadataType(ref emittedName);
            if ((object)forwardedToAssembly != null)
            {
                // Don't bother to check the forwarded-to assembly if we've already seen it.
                if (visitedAssemblies != null && visitedAssemblies.Contains(forwardedToAssembly))
                {
                    return CreateCycleInTypeForwarderErrorTypeSymbol(ref emittedName);
                }
                else
                {
                    visitedAssemblies = new ConsList<AssemblySymbol>(this, visitedAssemblies ?? ConsList<AssemblySymbol>.Empty);
                    return forwardedToAssembly.LookupTopLevelMetadataTypeWithCycleDetection(ref emittedName, visitedAssemblies, digThroughForwardedTypes: true);
                }
            }

            return null;
        }

        internal override ImmutableArray<AssemblySymbol> GetNoPiaResolutionAssemblies()
        {
            return _noPiaResolutionAssemblies;
        }

        internal override void SetNoPiaResolutionAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            _noPiaResolutionAssemblies = assemblies;
        }

        internal override void SetLinkedReferencedAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            _linkedReferencedAssemblies = assemblies;
        }

        internal override ImmutableArray<AssemblySymbol> GetLinkedReferencedAssemblies()
        {
            return _linkedReferencedAssemblies;
        }

        internal override ImmutableArray<byte> PublicKey
        {
            get
            {
                return Identity.PublicKey;
            }
        }

        internal override bool GetGuidString(out string guidString)
        {
            return Assembly.Modules[0].HasGuidAttribute(Assembly.Handle, out guidString);
        }

        internal override bool AreInternalsVisibleToThisAssembly(AssemblySymbol potentialGiverOfAccess)
        {
            IVTConclusion conclusion = MakeFinalIVTDetermination(potentialGiverOfAccess);
            return conclusion == IVTConclusion.Match || conclusion == IVTConclusion.OneSignedOneNot;
        }

        internal override IEnumerable<ImmutableArray<byte>> GetInternalsVisibleToPublicKeys(string simpleName)
        {
            return Assembly.GetInternalsVisibleToPublicKeys(simpleName);
        }

        internal DocumentationProvider DocumentationProvider
        {
            get
            {
                return _documentationProvider;
            }
        }

        internal override bool IsLinked
        {
            get
            {
                return _isLinked;
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                // While the specification for ExtensionAttribute requires that the containing assembly
                // have the attribute if any type in the assembly has the attribute, some compilers do
                // not properly follow that spec. Therefore we pessimistically assume every assembly
                // may contain extension methods.
                return true;
            }
        }

        internal PEModuleSymbol PrimaryModule
        {
            get
            {
                return (PEModuleSymbol)_modules[0];
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        public override AssemblyMetadata GetMetadata() => _assembly.GetMetadataCopy();
    }
}
