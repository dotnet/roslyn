// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents source or metadata assembly.
    /// </summary>
    /// <remarks></remarks>
    internal abstract class MetadataOrSourceAssemblySymbol
        : NonMissingAssemblySymbol
    {
        /// <summary>
        /// An array of cached Cor types defined in this assembly.
        /// Lazily filled by GetSpecialType method.
        /// </summary>
        /// <remarks></remarks>
        private NamedTypeSymbol?[]? _lazySpecialTypes;

        /// <summary>
        /// How many Cor types have we cached so far.
        /// </summary>
        private int _cachedSpecialTypes;

        /// <summary>
        /// Lookup declaration for predefined CorLib type in this Assembly.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal override NamedTypeSymbol GetDeclaredSpecialType(SpecialType type)
        {
#if DEBUG
            foreach (var module in this.Modules)
            {
                Debug.Assert(module.GetReferencedAssemblies().Length == 0);
            }
#endif

            if (_lazySpecialTypes == null || (object?)_lazySpecialTypes[(int)type] == null)
            {
                MetadataTypeName emittedName = MetadataTypeName.FromFullName(type.GetMetadataName(), useCLSCompliantNameArityEncoding: true);
                ModuleSymbol module = this.Modules[0];
                NamedTypeSymbol result = module.LookupTopLevelMetadataType(ref emittedName);
                if (result.Kind != SymbolKind.ErrorType && result.DeclaredAccessibility != Accessibility.Public)
                {
                    result = new MissingMetadataTypeSymbol.TopLevel(module, ref emittedName, type);
                }
                RegisterDeclaredSpecialType(result);
            }

            return _lazySpecialTypes![(int)type]!;
        }

        /// <summary>
        /// Register declaration of predefined CorLib type in this Assembly.
        /// </summary>
        /// <param name="corType"></param>
        internal override sealed void RegisterDeclaredSpecialType(NamedTypeSymbol corType)
        {
            SpecialType typeId = corType.SpecialType;
            Debug.Assert(typeId != SpecialType.None);
            Debug.Assert(ReferenceEquals(corType.ContainingAssembly, this));
            Debug.Assert(corType.ContainingModule.Ordinal == 0);
            Debug.Assert(ReferenceEquals(this.CorLibrary, this));

            if (_lazySpecialTypes == null)
            {
                Interlocked.CompareExchange(ref _lazySpecialTypes,
                    new NamedTypeSymbol[(int)SpecialType.Count + 1], null);
            }

            if ((object?)Interlocked.CompareExchange(ref _lazySpecialTypes[(int)typeId], corType, null) != null)
            {
                Debug.Assert(ReferenceEquals(corType, _lazySpecialTypes[(int)typeId]) ||
                                        (corType.Kind == SymbolKind.ErrorType &&
#nullable disable // Should use ref local for '_lazySpecialTypes[(int)typeId]' so it works with nullable reference types https://github.com/dotnet/roslyn/issues/39166
                                        _lazySpecialTypes[(int)typeId].Kind == SymbolKind.ErrorType));
#nullable enable
            }
            else
            {
                Interlocked.Increment(ref _cachedSpecialTypes);
                Debug.Assert(_cachedSpecialTypes > 0 && _cachedSpecialTypes <= (int)SpecialType.Count);
            }
        }


        /// <summary>
        /// Continue looking for declaration of predefined CorLib type in this Assembly
        /// while symbols for new type declarations are constructed.
        /// </summary>
        internal override bool KeepLookingForDeclaredSpecialTypes
        {
            get
            {
                return ReferenceEquals(this.CorLibrary, this) && _cachedSpecialTypes < (int)SpecialType.Count;
            }
        }

        private ICollection<string>? _lazyTypeNames;
        private ICollection<string>? _lazyNamespaceNames;

        public override ICollection<string> TypeNames
        {
            get
            {
                if (_lazyTypeNames == null)
                {
                    Interlocked.CompareExchange(ref _lazyTypeNames, UnionCollection<string>.Create(this.Modules, m => m.TypeNames), null);
                }

                return _lazyTypeNames;
            }
        }

        public override ICollection<string> NamespaceNames
        {
            get
            {
                if (_lazyNamespaceNames == null)
                {
                    Interlocked.CompareExchange(ref _lazyNamespaceNames, UnionCollection<string>.Create(this.Modules, m => m.NamespaceNames), null);
                }

                return _lazyNamespaceNames;
            }
        }

        /// <summary>
        /// Not yet known value is represented by ErrorTypeSymbol.UnknownResultType
        /// </summary>
        private Symbol?[]? _lazySpecialTypeMembers;

        /// <summary>
        /// Lookup member declaration in predefined CorLib type in this Assembly. Only valid if this 
        /// assembly is the Cor Library
        /// </summary>
        internal override Symbol? GetDeclaredSpecialTypeMember(SpecialMember member)
        {
#if DEBUG
            foreach (var module in this.Modules)
            {
                Debug.Assert(module.GetReferencedAssemblies().Length == 0);
            }
#endif

            if (_lazySpecialTypeMembers == null || ReferenceEquals(_lazySpecialTypeMembers[(int)member], ErrorTypeSymbol.UnknownResultType))
            {
                if (_lazySpecialTypeMembers == null)
                {
                    var specialTypeMembers = new Symbol[(int)SpecialMember.Count];

                    for (int i = 0; i < specialTypeMembers.Length; i++)
                    {
                        specialTypeMembers[i] = ErrorTypeSymbol.UnknownResultType;
                    }

                    Interlocked.CompareExchange(ref _lazySpecialTypeMembers, specialTypeMembers, null);
                }

                var descriptor = SpecialMembers.GetDescriptor(member);
                NamedTypeSymbol type = GetDeclaredSpecialType((SpecialType)descriptor.DeclaringTypeId);
                Symbol? result = null;

                if (!type.IsErrorType())
                {
                    result = CSharpCompilation.GetRuntimeMember(type, ref descriptor, CSharpCompilation.SpecialMembersSignatureComparer.Instance, accessWithinOpt: null);
                }

                Interlocked.CompareExchange(ref _lazySpecialTypeMembers[(int)member], result, ErrorTypeSymbol.UnknownResultType);
            }

            return _lazySpecialTypeMembers[(int)member];
        }

        /// <summary>
        /// Determine whether this assembly has been granted access to <paramref name="potentialGiverOfAccess"></paramref>.
        /// Assumes that the public key has been determined. The result will be cached.
        /// </summary>
        /// <param name="potentialGiverOfAccess"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        protected IVTConclusion MakeFinalIVTDetermination(AssemblySymbol potentialGiverOfAccess)
        {
            IVTConclusion result;
            if (AssembliesToWhichInternalAccessHasBeenDetermined.TryGetValue(potentialGiverOfAccess, out result))
                return result;

            result = IVTConclusion.NoRelationshipClaimed;

            // returns an empty list if there was no IVT attribute at all for the given name
            // A name w/o a key is represented by a list with an entry that is empty
            IEnumerable<ImmutableArray<byte>> publicKeys = potentialGiverOfAccess.GetInternalsVisibleToPublicKeys(this.Name);

            // We have an easy out here. Suppose the assembly wanting access is 
            // being compiled as a module. You can only strong-name an assembly. So we are going to optimistically 
            // assume that it is going to be compiled into an assembly with a matching strong name, if necessary.
            if (publicKeys.Any() && this.IsNetModule())
            {
                return IVTConclusion.Match;
            }

            // look for one that works, if none work, then return the failure for the last one examined.
            foreach (var key in publicKeys)
            {
                // We pass the public key of this assembly explicitly so PerformIVTCheck does not need
                // to get it from this.Identity, which would trigger an infinite recursion.
                result = potentialGiverOfAccess.Identity.PerformIVTCheck(this.PublicKey, key);
                Debug.Assert(result != IVTConclusion.NoRelationshipClaimed);

                if (result == IVTConclusion.Match || result == IVTConclusion.OneSignedOneNot)
                {
                    break;
                }
            }

            AssembliesToWhichInternalAccessHasBeenDetermined.TryAdd(potentialGiverOfAccess, result);
            return result;
        }

        //EDMAURER This is a cache mapping from assemblies which we have analyzed whether or not they grant
        //internals access to us to the conclusion reached.
        private ConcurrentDictionary<AssemblySymbol, IVTConclusion>? _assembliesToWhichInternalAccessHasBeenAnalyzed;

        private ConcurrentDictionary<AssemblySymbol, IVTConclusion> AssembliesToWhichInternalAccessHasBeenDetermined
        {
            get
            {
                if (_assembliesToWhichInternalAccessHasBeenAnalyzed == null)
                    Interlocked.CompareExchange(ref _assembliesToWhichInternalAccessHasBeenAnalyzed, new ConcurrentDictionary<AssemblySymbol, IVTConclusion>(), null);
                return _assembliesToWhichInternalAccessHasBeenAnalyzed;
            }
        }
    }
}
