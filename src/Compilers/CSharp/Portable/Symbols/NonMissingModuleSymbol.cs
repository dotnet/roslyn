// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A <see cref="NonMissingModuleSymbol"/> is a special kind of <see cref="ModuleSymbol"/> that represents
    /// a module that is not missing, i.e. the "real" thing.
    /// </summary>
    internal abstract class NonMissingModuleSymbol : ModuleSymbol
    {
        /// <summary>
        /// An array of <see cref="AssemblySymbol"/> objects corresponding to assemblies directly referenced by this module.
        /// </summary>
        /// <remarks>
        /// The contents are provided by ReferenceManager and may not be modified.
        /// </remarks>
        private ModuleReferences<AssemblySymbol> _moduleReferences;

        /// <summary>
        /// Does this symbol represent a missing module.
        /// </summary>
        internal sealed override bool IsMissing
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns an array of assembly identities for assemblies referenced by this module.
        /// Items at the same position from GetReferencedAssemblies and from GetReferencedAssemblySymbols 
        /// should correspond to each other.
        /// </summary>
        internal sealed override ImmutableArray<AssemblyIdentity> GetReferencedAssemblies()
        {
            AssertReferencesInitialized();
            return _moduleReferences.Identities;
        }

        /// <summary>
        /// Returns an array of AssemblySymbol objects corresponding to assemblies referenced 
        /// by this module. Items at the same position from GetReferencedAssemblies and 
        /// from GetReferencedAssemblySymbols should correspond to each other. If reference is 
        /// not resolved by compiler, GetReferencedAssemblySymbols returns MissingAssemblySymbol in the
        /// corresponding item.
        /// </summary>
        internal sealed override ImmutableArray<AssemblySymbol> GetReferencedAssemblySymbols()
        {
            AssertReferencesInitialized();
            return _moduleReferences.Symbols;
        }

        internal ImmutableArray<UnifiedAssembly<AssemblySymbol>> GetUnifiedAssemblies()
        {
            AssertReferencesInitialized();
            return _moduleReferences.UnifiedAssemblies;
        }

        internal override bool HasUnifiedReferences
        {
            get { return GetUnifiedAssemblies().Length > 0; }
        }

        internal override bool GetUnificationUseSiteDiagnostic(ref DiagnosticInfo result, TypeSymbol dependentType)
        {
            AssertReferencesInitialized();

            var ownerModule = this;
            var ownerAssembly = ownerModule.ContainingAssembly;
            var dependentAssembly = dependentType.ContainingAssembly;
            if (ownerAssembly == dependentAssembly)
            {
                return false;
            }

            // TODO (tomat): we should report an error/warning for all unified references, not just the first one.

            foreach (var unifiedAssembly in GetUnifiedAssemblies())
            {
                if (!ReferenceEquals(unifiedAssembly.TargetAssembly, dependentAssembly))
                {
                    continue;
                }

                var referenceId = unifiedAssembly.OriginalReference;
                var definitionId = dependentAssembly.Identity;
                var involvedAssemblies = ImmutableArray.Create<Symbol>(ownerAssembly, dependentAssembly);

                DiagnosticInfo info;
                if (definitionId.Version > referenceId.Version)
                {
                    // unified with a definition whose version is higher than the reference                    
                    ErrorCode warning = (definitionId is
                    {
                        Version: { Major: referenceId.Version.Major, Minor: referenceId.Version.Minor }
                    }
) ?
                                        ErrorCode.WRN_UnifyReferenceBldRev : ErrorCode.WRN_UnifyReferenceMajMin;

                    // warning: Assuming assembly reference '{0}' used by '{1}' matches identity '{2}' of '{3}', you may need to supply runtime policy.
                    info = new CSDiagnosticInfo(
                        warning,
                        new object[]
                        {
                            referenceId.GetDisplayName(),
                            ownerAssembly.Name, // TODO (tomat): should rather be MetadataReference.Display for the corresponding reference
                            definitionId.GetDisplayName(),
                            dependentAssembly.Name
                        },
                        involvedAssemblies,
                        ImmutableArray<Location>.Empty);
                }
                else
                {
                    // unified with a definition whose version is lower than the reference

                    // error: Assembly '{0}' with identity '{1}' uses '{2}' which has a higher version than referenced assembly '{3}' with identity '{4}'
                    info = new CSDiagnosticInfo(
                        ErrorCode.ERR_AssemblyMatchBadVersion,
                        new object[]
                        {
                            ownerAssembly.Name, // TODO (tomat): should rather be MetadataReference.Display for the corresponding reference
                            ownerAssembly.Identity.GetDisplayName(),
                            referenceId.GetDisplayName(),
                            dependentAssembly.Name, // TODO (tomat): should rather be MetadataReference.Display for the corresponding reference
                            definitionId.GetDisplayName()
                        },
                        involvedAssemblies,
                        ImmutableArray<Location>.Empty);
                }

                if (MergeUseSiteDiagnostics(ref result, info))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A helper method for ReferenceManager to set assembly identities for assemblies 
        /// referenced by this module and corresponding AssemblySymbols.
        /// </summary>
        internal override void SetReferences(ModuleReferences<AssemblySymbol> moduleReferences, SourceAssemblySymbol originatingSourceAssemblyDebugOnly = null)
        {
            Debug.Assert(moduleReferences != null);

            AssertReferencesUninitialized();

            _moduleReferences = moduleReferences;
        }

        [Conditional("DEBUG")]
        internal void AssertReferencesUninitialized()
        {
            Debug.Assert(_moduleReferences == null);
        }

        [Conditional("DEBUG")]
        internal void AssertReferencesInitialized()
        {
            Debug.Assert(_moduleReferences != null);
        }

        /// <summary>
        /// Lookup a top level type referenced from metadata, names should be
        /// compared case-sensitively.
        /// </summary>
        /// <param name="emittedName">
        /// Full type name, possibly with generic name mangling.
        /// </param>
        /// <returns>
        /// Symbol for the type, or MissingMetadataSymbol if the type isn't found.
        /// </returns>
        /// <remarks></remarks>
        internal sealed override NamedTypeSymbol LookupTopLevelMetadataType(ref MetadataTypeName emittedName)
        {
            NamedTypeSymbol result;
            NamespaceSymbol scope = this.GlobalNamespace.LookupNestedNamespace(emittedName.NamespaceSegments);

            if ((object)scope == null)
            {
                // We failed to locate the namespace
                result = new MissingMetadataTypeSymbol.TopLevel(this, ref emittedName);
            }
            else
            {
                result = scope.LookupMetadataType(ref emittedName);
            }

            Debug.Assert((object)result != null);
            return result;
        }
    }
}
