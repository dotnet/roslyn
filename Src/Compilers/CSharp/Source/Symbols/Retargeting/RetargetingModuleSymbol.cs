// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a primary module of a <see cref="RetargetingAssemblySymbol"/>. Essentially this is a wrapper around 
    /// another <see cref="SourceModuleSymbol"/> that is responsible for retargeting symbols from one assembly to another. 
    /// It can retarget symbols for multiple assemblies at the same time.
    /// 
    /// Here is how retargeting is implemented in general:
    /// - Symbols from underlying module are substituted with retargeting symbols.
    /// - Symbols from referenced assemblies that can be reused as is (i.e. don't have to be retargeted) are
    ///   used as is.
    /// - Symbols from referenced assemblies that must be retargeted are substituted with result of retargeting.
    /// </summary>
    internal sealed partial class RetargetingModuleSymbol : NonMissingModuleSymbol
    {
        /// <summary>
        /// Owning <see cref="RetargetingAssemblySymbol"/>.
        /// </summary>
        private readonly RetargetingAssemblySymbol retargetingAssembly;

        /// <summary>
        /// The underlying <see cref="ModuleSymbol"/>, cannot be another <see cref="RetargetingModuleSymbol"/>.
        /// </summary>
        private readonly SourceModuleSymbol underlyingModule;

        /// <summary>
        /// The map that captures information about what assembly should be retargeted 
        /// to what assembly. Key is the <see cref="AssemblySymbol"/> referenced by the underlying module,
        /// value is the corresponding <see cref="AssemblySymbol"/> referenced by this module, and corresponding
        /// retargeting map for symbols.
        /// </summary>
        private readonly Dictionary<AssemblySymbol, DestinationData> retargetingAssemblyMap =
            new Dictionary<AssemblySymbol, DestinationData>();

        private struct DestinationData
        {
            public AssemblySymbol To;
            public ConcurrentDictionary<NamedTypeSymbol, NamedTypeSymbol> SymbolMap;
        }

        internal readonly RetargetingSymbolTranslator RetargetingTranslator;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="retargetingAssembly">
        /// Owning assembly.
        /// </param>
        /// <param name="underlyingModule">
        /// The underlying ModuleSymbol, cannot be another RetargetingModuleSymbol.
        /// </param>
        public RetargetingModuleSymbol(RetargetingAssemblySymbol retargetingAssembly, SourceModuleSymbol underlyingModule)
        {
            Debug.Assert((object)retargetingAssembly != null);
            Debug.Assert((object)underlyingModule != null);

            this.retargetingAssembly = retargetingAssembly;
            this.underlyingModule = underlyingModule;
            this.RetargetingTranslator = new RetargetingSymbolTranslator(this);

            this.createRetargetingMethod = CreateRetargetingMethod;
            this.createRetargetingNamespace = CreateRetargetingNamespace;
            this.createRetargetingNamedType = CreateRetargetingNamedType;
            this.createRetargetingField = CreateRetargetingField;
            this.createRetargetingProperty = CreateRetargetingProperty;
            this.createRetargetingEvent = CreateRetargetingEvent;
            this.createRetargetingTypeParameter = CreateRetargetingTypeParameter;
        }

        internal override int Ordinal
        {
            get
            {
                Debug.Assert(underlyingModule.Ordinal == 0); // Always a source module
                return 0;
            }
        }

        internal override Machine Machine
        {
            get
            {
                return underlyingModule.Machine;
            }
        }

        internal override bool Bit32Required
        {
            get
            {
                return underlyingModule.Bit32Required;
            }
        }

        /// <summary>
        /// The underlying ModuleSymbol, cannot be another RetargetingModuleSymbol.
        /// </summary>
        public SourceModuleSymbol UnderlyingModule
        {
            get
            {
                return underlyingModule;
            }
        }

        public override NamespaceSymbol GlobalNamespace
        {
            get
            {
                return RetargetingTranslator.Retarget(underlyingModule.GlobalNamespace);
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return underlyingModule.IsImplicitlyDeclared; }
        }

        public override string Name
        {
            get
            {
                return underlyingModule.Name;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return underlyingModule.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return retargetingAssembly;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return retargetingAssembly;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return underlyingModule.Locations;
            }
        }

        /// <summary>
        /// A helper method for ReferenceManager to set AssemblySymbols for assemblies 
        /// referenced by this module.
        /// </summary>
        internal override void SetReferences(ModuleReferences<AssemblySymbol> moduleReferences, SourceAssemblySymbol originatingSourceAssemblyDebugOnly)
        {
            base.SetReferences(moduleReferences, originatingSourceAssemblyDebugOnly);

            // Build the retargeting map
            retargetingAssemblyMap.Clear();

            ImmutableArray<AssemblySymbol> underlyingBoundReferences = underlyingModule.GetReferencedAssemblySymbols();
            ImmutableArray<AssemblySymbol> referencedAssemblySymbols = moduleReferences.Symbols;

            Debug.Assert(referencedAssemblySymbols.Length == moduleReferences.Names.Length);
            Debug.Assert(referencedAssemblySymbols.Length <= underlyingBoundReferences.Length); // Linked references are filtered out.

            int i, j;
            for (i = 0, j = 0; i < referencedAssemblySymbols.Length; i++, j++)
            {
                // Skip linked assemblies for source module
                while (underlyingBoundReferences[j].IsLinked)
                {
                    j++;
                }

#if DEBUG
                var identityComparer = underlyingModule.DeclaringCompilation.Options.AssemblyIdentityComparer;
                var definitionIdentity = ReferenceEquals(referencedAssemblySymbols[i], originatingSourceAssemblyDebugOnly) ?
                        new AssemblyIdentity(name: originatingSourceAssemblyDebugOnly.Name) :
                        referencedAssemblySymbols[i].Identity;

                Debug.Assert(identityComparer.Compare(moduleReferences.Names[i], definitionIdentity) != AssemblyIdentityComparer.ComparisonResult.NotEquivalent);
                Debug.Assert(identityComparer.Compare(moduleReferences.Names[i], underlyingBoundReferences[j].Identity) != AssemblyIdentityComparer.ComparisonResult.NotEquivalent);
#endif

                if (!ReferenceEquals(referencedAssemblySymbols[i], underlyingBoundReferences[j]))
                {
                    DestinationData destinationData;

                    if (!retargetingAssemblyMap.TryGetValue(underlyingBoundReferences[j], out destinationData))
                    {
                        var symbolMap = new ConcurrentDictionary<NamedTypeSymbol, NamedTypeSymbol>();

                        retargetingAssemblyMap.Add(underlyingBoundReferences[j],
                            new DestinationData { To = referencedAssemblySymbols[i], SymbolMap = symbolMap });
                    }
                    else
                    {
                        Debug.Assert(ReferenceEquals(destinationData.To, referencedAssemblySymbols[i]));
                    }
                }
            }

#if DEBUG
            while (j < underlyingBoundReferences.Length && underlyingBoundReferences[j].IsLinked)
            {
                j++;
            }

            Debug.Assert(j == underlyingBoundReferences.Length);
#endif
        }

        internal override ICollection<string> TypeNames
        {
            get
            {
                return underlyingModule.TypeNames;
            }
        }

        internal override ICollection<string> NamespaceNames
        {
            get
            {
                return underlyingModule.NamespaceNames;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return RetargetingTranslator.GetRetargetedAttributes(this.underlyingModule.GetAttributes(), ref this.lazyCustomAttributes);
        }

        internal override bool HasAssemblyCompilationRelaxationsAttribute
        {
            get
            {
                return underlyingModule.HasAssemblyCompilationRelaxationsAttribute;
            }
        }

        internal override bool HasAssemblyRuntimeCompatibilityAttribute
        {
            get
            {
                return underlyingModule.HasAssemblyRuntimeCompatibilityAttribute;
            }
        }

        internal override CharSet? DefaultMarshallingCharSet
        {
            get
            {
                return underlyingModule.DefaultMarshallingCharSet;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
