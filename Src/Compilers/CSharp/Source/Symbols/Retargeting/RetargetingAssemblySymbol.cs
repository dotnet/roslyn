// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Essentially this is a wrapper around another AssemblySymbol that is responsible for retargeting
    /// symbols from one assembly to another. It can retarget symbols for multiple assemblies at the same time. 
    /// 
    /// For example, compilation C1 references v1 of Lib.dll and compilation C2 references C1 and v2 of Lib.dll. 
    /// In this case, in context of C2, all types from v1 of Lib.dll leaking through C1 (through method 
    /// signatures, etc.) must be retargeted to the types from v2 of Lib.dll. This is what 
    /// RetargetingAssemblySymbol is responsible for. In the example above, modules in C2 do not 
    /// reference C1.m_AssemblySymbol, but reference a special RetargetingAssemblySymbol created for 
    /// C1 by ReferenceManager.
    /// 
    /// Here is how retargeting is implemented in general:
    /// - Symbols from underlying assembly are substituted with retargeting symbols.
    /// - Symbols from referenced assemblies that can be reused as is (i.e. doesn't have to be retargeted) are
    ///   used as is.
    /// - Symbols from referenced assemblies that must be retargeted are substituted with result of retargeting.
    /// </summary>
    internal sealed class RetargetingAssemblySymbol : NonMissingAssemblySymbol
    {
        /// <summary>
        /// The underlying AssemblySymbol, it leaks symbols that should be retargeted.
        /// This cannot be an instance of RetargetingAssemblySymbol.
        /// </summary>
        private readonly SourceAssemblySymbol underlyingAssembly;

        /// <summary>
        /// The list of contained ModuleSymbol objects. First item in the list
        /// is RetargetingModuleSymbol that wraps corresponding SourceModuleSymbol 
        /// from underlyingAssembly.Modules list, the rest are PEModuleSymbols for 
        /// added modules.
        /// </summary>
        private readonly ImmutableArray<ModuleSymbol> modules;

        /// <summary>
        /// An array of assemblies involved in canonical type resolution of
        /// NoPia local types defined within this assembly. In other words, all 
        /// references used by a compilation referencing this assembly.
        /// The array and its content is provided by ReferenceManager and must not be modified.
        /// </summary>
        private ImmutableArray<AssemblySymbol> noPiaResolutionAssemblies;

        /// <summary>
        /// An array of assemblies referenced by this assembly, which are linked (/l-ed) by 
        /// each compilation that is using this AssemblySymbol as a reference. 
        /// If this AssemblySymbol is linked too, it will be in this array too.
        /// The array and its content is provided by ReferenceManager and must not be modified.
        /// </summary>
        private ImmutableArray<AssemblySymbol> linkedReferencedAssemblies;

        /// <summary>
        /// A map from a local NoPia type to corresponding canonical type.
        /// </summary>
        internal readonly ConcurrentDictionary<NamedTypeSymbol, NamedTypeSymbol> NoPiaUnificationMap =
            new ConcurrentDictionary<NamedTypeSymbol, NamedTypeSymbol>();

        /// <summary>
        /// Assembly is /l-ed by compilation that is using it as a reference.
        /// </summary>
        private readonly bool isLinked;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="underlyingAssembly">
        /// The underlying AssemblySymbol, cannot be an instance of RetargetingAssemblySymbol.
        /// </param>
        /// <param name="isLinked">
        /// Assembly is /l-ed by compilation that is using it as a reference.
        /// </param>
        public RetargetingAssemblySymbol(SourceAssemblySymbol underlyingAssembly, bool isLinked)
        {
            Debug.Assert((object)underlyingAssembly != null);

            this.underlyingAssembly = underlyingAssembly;

            ModuleSymbol[] modules = new ModuleSymbol[underlyingAssembly.Modules.Length];

            modules[0] = new RetargetingModuleSymbol(this, (SourceModuleSymbol)underlyingAssembly.Modules[0]);

            for (int i = 1; i < underlyingAssembly.Modules.Length; i++)
            {
                PEModuleSymbol under = (PEModuleSymbol)underlyingAssembly.Modules[i];
                modules[i] = new PEModuleSymbol(this, under.Module, under.ImportOptions, i);
            }

            this.modules = modules.AsImmutableOrNull();
            this.isLinked = isLinked;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return ((RetargetingModuleSymbol)this.modules[0]).RetargetingTranslator;
            }
        }

        /// <summary>
        /// The underlying AssemblySymbol.
        /// This cannot be an instance of RetargetingAssemblySymbol.
        /// </summary>
        public AssemblySymbol UnderlyingAssembly
        {
            get
            {
                return underlyingAssembly;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return underlyingAssembly.IsImplicitlyDeclared; }
        }

        public override AssemblyIdentity Identity
        {
            get
            {
                return underlyingAssembly.Identity;
            }
        }

        internal override ImmutableArray<byte> PublicKey
        {
            get { return underlyingAssembly.PublicKey; }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return underlyingAssembly.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override ImmutableArray<ModuleSymbol> Modules
        {
            get
            {
                return modules;
            }
        }

        internal override bool KeepLookingForDeclaredSpecialTypes
        {
            get
            {
                // RetargetingAssemblySymbol never represents Core library. 
                return false;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return underlyingAssembly.Locations;
            }
        }

        internal override IEnumerable<ImmutableArray<byte>> GetInternalsVisibleToPublicKeys(string simpleName)
        {
            return underlyingAssembly.GetInternalsVisibleToPublicKeys(simpleName);
        }

        internal override bool AreInternalsVisibleToThisAssembly(AssemblySymbol other)
        {
            return underlyingAssembly.AreInternalsVisibleToThisAssembly(other);
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return RetargetingTranslator.GetRetargetedAttributes(this.underlyingAssembly.GetAttributes(), ref this.lazyCustomAttributes);
        }

        /// <summary>
        /// Lookup declaration for FX type in this Assembly.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal override NamedTypeSymbol GetDeclaredSpecialType(SpecialType type)
        {
            // Cor library should not have any references and, therefore, should never be
            // wrapped by a RetargetingAssemblySymbol.
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<AssemblySymbol> GetNoPiaResolutionAssemblies()
        {
            return noPiaResolutionAssemblies;
        }

        internal override void SetNoPiaResolutionAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            this.noPiaResolutionAssemblies = assemblies;
        }

        internal override void SetLinkedReferencedAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            this.linkedReferencedAssemblies = assemblies;
        }

        internal override ImmutableArray<AssemblySymbol> GetLinkedReferencedAssemblies()
        {
            return this.linkedReferencedAssemblies;
        }

        internal override bool IsLinked
        {
            get
            {
                return this.isLinked;
            }
        }

        public override ICollection<string> TypeNames
        {
            get
            {
                return underlyingAssembly.TypeNames;
            }
        }

        public override ICollection<string> NamespaceNames
        {
            get
            {
                return underlyingAssembly.NamespaceNames;
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                return underlyingAssembly.MightContainExtensionMethods;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        internal override bool GetGuidString(out string guidString)
        {
            return underlyingAssembly.GetGuidString(out guidString);
        }

        internal override NamedTypeSymbol TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies)
        {
            NamedTypeSymbol underlying = underlyingAssembly.TryLookupForwardedMetadataType(ref emittedName);

            if ((object)underlying == null)
            {
                return null;
            }

            return this.RetargetingTranslator.Retarget(underlying, RetargetOptions.RetargetPrimitiveTypesByName);
        }
    }
}
