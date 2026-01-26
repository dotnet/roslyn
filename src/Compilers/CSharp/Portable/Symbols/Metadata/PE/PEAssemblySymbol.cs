// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

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

#nullable enable
        private DiagnosticInfo? _lazyCachedCompilerFeatureRequiredDiagnosticInfo = CSDiagnosticInfo.EmptyErrorInfo;

        private ObsoleteAttributeData? _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;
#nullable disable

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

        // TODO: https://github.com/dotnet/roslyn/issues/9000
        public override Version AssemblyVersionPattern => null;

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

        public override int MetadataToken
        {
            get
            {
                return MetadataTokens.GetToken(_assembly.Handle);
            }
        }

        internal override bool HasImportedFromTypeLibAttribute
            => PrimaryModule.Module.HasImportedFromTypeLibAttribute(Assembly.Handle, out _);

        internal override bool HasPrimaryInteropAssemblyAttribute
            => PrimaryModule.Module.HasPrimaryInteropAssemblyAttribute(Assembly.Handle, out _, out _);

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyCustomAttributes, loadAndFilterAttributes());
            }

            return _lazyCustomAttributes;

            ImmutableArray<CSharpAttributeData> loadAndFilterAttributes()
            {
                var containingModule = this.PrimaryModule;
                if (!containingModule.TryGetNonEmptyCustomAttributes(_assembly.Handle, out var customAttributeHandles))
                {
                    return [];
                }

                var mightContainExtensionMethods = this.MightContainExtensionMethods;
                using var builder = TemporaryArray<CSharpAttributeData>.Empty;
                foreach (var handle in customAttributeHandles)
                {
                    if (mightContainExtensionMethods && containingModule.AttributeMatchesFilter(handle, AttributeDescription.CaseSensitiveExtensionAttribute))
                        continue;

                    builder.Add(new PEAttributeData(containingModule, handle));
                }

                return builder.ToImmutableAndClear();
            }
        }

        /// <summary>
        /// Look up the assemblies to which the given metadata type is forwarded.
        /// </summary>
        /// <param name="emittedName"></param>
        /// <returns>
        /// The assemblies to which the given type is forwarded.
        /// </returns>
        /// <remarks>
        /// The returned assemblies may also forward the type.
        /// </remarks>
        internal (AssemblySymbol FirstSymbol, AssemblySymbol SecondSymbol) LookupAssembliesForForwardedMetadataType(ref MetadataTypeName emittedName)
        {
            // Look in the type forwarders of the primary module of this assembly, clr does not honor type forwarder
            // in non-primary modules.

            // Examine the type forwarders, but only from the primary module.
            return this.PrimaryModule.GetAssembliesForForwardedType(ref emittedName);
        }

        internal override IEnumerable<NamedTypeSymbol> GetAllTopLevelForwardedTypes()
        {
            return this.PrimaryModule.GetForwardedTypes();
        }

#nullable enable

        internal override NamedTypeSymbol? TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol>? visitedAssemblies)
        {
            // Check if it is a forwarded type.
            (AssemblySymbol firstSymbol, AssemblySymbol secondSymbol) = LookupAssembliesForForwardedMetadataType(ref emittedName);

            if ((object)firstSymbol != null)
            {
                if ((object)secondSymbol != null)
                {
                    // Report the main module as that is the only one checked. clr does not honor type forwarders in non-primary modules.
                    return CreateMultipleForwardingErrorTypeSymbol(ref emittedName, this.PrimaryModule, firstSymbol, secondSymbol);
                }

                // Don't bother to check the forwarded-to assembly if we've already seen it.
                if (visitedAssemblies != null && visitedAssemblies.Contains(firstSymbol))
                {
                    return CreateCycleInTypeForwarderErrorTypeSymbol(ref emittedName);
                }
                else
                {
                    visitedAssemblies = new ConsList<AssemblySymbol>(this, visitedAssemblies ?? ConsList<AssemblySymbol>.Empty);
                    return firstSymbol.LookupDeclaredOrForwardedTopLevelMetadataType(ref emittedName, visitedAssemblies);
                }
            }

            return null;
        }

#nullable disable

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
            IVTConclusion conclusion = MakeFinalIVTDetermination(potentialGiverOfAccess, assertUnexpectedGiver: true);
            return conclusion == IVTConclusion.Match || conclusion == IVTConclusion.OneSignedOneNot;
        }

        internal override IEnumerable<ImmutableArray<byte>> GetInternalsVisibleToPublicKeys(string simpleName)
        {
            return Assembly.GetInternalsVisibleToPublicKeys(simpleName);
        }

        internal override IEnumerable<string> GetInternalsVisibleToAssemblyNames()
        {
            return Assembly.GetInternalsVisibleToAssemblyNames();
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

        public override AssemblyMetadata GetMetadata() => _assembly.GetNonDisposableMetadata();

#nullable enable
        internal DiagnosticInfo? GetCompilerFeatureRequiredDiagnostic()
        {
            if (_lazyCachedCompilerFeatureRequiredDiagnosticInfo == CSDiagnosticInfo.EmptyErrorInfo)
            {
                Interlocked.CompareExchange(
                    ref _lazyCachedCompilerFeatureRequiredDiagnosticInfo,
                    PEUtilities.DeriveCompilerFeatureRequiredAttributeDiagnostic(this, PrimaryModule, this.Assembly.Handle, CompilerFeatureRequiredFeatures.None, new MetadataDecoder(PrimaryModule)),
                    CSDiagnosticInfo.EmptyErrorInfo);
            }

            return _lazyCachedCompilerFeatureRequiredDiagnosticInfo;
        }

        public override bool HasUnsupportedMetadata
            => GetCompilerFeatureRequiredDiagnostic()?.Code == (int)ErrorCode.ERR_UnsupportedCompilerFeature || base.HasUnsupportedMetadata;

        internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
        {
            get
            {
                if (_lazyObsoleteAttributeData == ObsoleteAttributeData.Uninitialized)
                {
                    var experimentalData = PrimaryModule.Module.TryDecodeExperimentalAttributeData(Assembly.Handle, new MetadataDecoder(PrimaryModule));
                    Interlocked.CompareExchange(ref _lazyObsoleteAttributeData, experimentalData, ObsoleteAttributeData.Uninitialized);
                }

                return _lazyObsoleteAttributeData;
            }
        }
    }
}
