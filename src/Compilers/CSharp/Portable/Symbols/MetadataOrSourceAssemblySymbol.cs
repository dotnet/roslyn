// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents source or metadata assembly.
    /// </summary>
    internal abstract class MetadataOrSourceAssemblySymbol
        : NonMissingAssemblySymbol
    {
        /// <summary>
        /// An array of cached Cor types defined in this assembly.
        /// Lazily filled by GetDeclaredSpecialType method.
        /// </summary>
        private NamedTypeSymbol[] _lazySpecialTypes;

        private TypeConversions _lazyTypeConversions;

        /// <summary>
        /// How many Cor types have we cached so far.
        /// </summary>
        private int _cachedSpecialTypes;

        private NativeIntegerTypeSymbol[] _lazyNativeIntegerTypes;

#nullable enable 

        /// <summary>
        /// Lookup declaration for predefined CorLib type in this Assembly.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal sealed override NamedTypeSymbol GetDeclaredSpecialType(ExtendedSpecialType type)
        {
#if DEBUG
            foreach (var module in this.Modules)
            {
                Debug.Assert(module.GetReferencedAssemblies().Length == 0);
            }
#endif

            if (_lazySpecialTypes == null || (object)_lazySpecialTypes[(int)type] == null)
            {
                MetadataTypeName emittedName = MetadataTypeName.FromFullName(type.GetMetadataName(), useCLSCompliantNameArityEncoding: true);
                ModuleSymbol module = this.Modules[0];
                NamedTypeSymbol? result = module.LookupTopLevelMetadataType(ref emittedName);

                Debug.Assert(result?.IsErrorType() != true);

                if (result is null || result.DeclaredAccessibility != Accessibility.Public)
                {
                    result = new MissingMetadataTypeSymbol.TopLevel(module, ref emittedName, type);
                }

                RegisterDeclaredSpecialType(result);
            }

            Debug.Assert(_lazySpecialTypes is not null);
            return _lazySpecialTypes[(int)type];
        }

#nullable disable

        /// <summary>
        /// Register declaration of predefined CorLib type in this Assembly.
        /// </summary>
        /// <param name="corType"></param>
        internal sealed override void RegisterDeclaredSpecialType(NamedTypeSymbol corType)
        {
            ExtendedSpecialType typeId = corType.ExtendedSpecialType;
            Debug.Assert(typeId != SpecialType.None);
            Debug.Assert(ReferenceEquals(corType.ContainingAssembly, this));
            Debug.Assert(corType.ContainingModule.Ordinal == 0);
            Debug.Assert(ReferenceEquals(this.CorLibrary, this));

            if (_lazySpecialTypes == null)
            {
                Interlocked.CompareExchange(ref _lazySpecialTypes,
                    new NamedTypeSymbol[(int)InternalSpecialType.NextAvailable], null);
            }

            if ((object)Interlocked.CompareExchange(ref _lazySpecialTypes[(int)typeId], corType, null) != null)
            {
                Debug.Assert(ReferenceEquals(corType, _lazySpecialTypes[(int)typeId]) ||
                                        (corType.Kind == SymbolKind.ErrorType &&
                                        _lazySpecialTypes[(int)typeId].Kind == SymbolKind.ErrorType));
            }
            else
            {
                Interlocked.Increment(ref _cachedSpecialTypes);
                Debug.Assert(_cachedSpecialTypes > 0 && _cachedSpecialTypes < (int)InternalSpecialType.NextAvailable);
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
                return ReferenceEquals(this.CorLibrary, this) && _cachedSpecialTypes < (int)InternalSpecialType.NextAvailable - 1;
            }
        }

        private ICollection<string> _lazyTypeNames;
        private ICollection<string> _lazyNamespaceNames;

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

        internal sealed override NamedTypeSymbol GetNativeIntegerType(NamedTypeSymbol underlyingType)
        {
            Debug.Assert(!underlyingType.IsNativeIntegerType);

            if (_lazyNativeIntegerTypes == null)
            {
                Interlocked.CompareExchange(ref _lazyNativeIntegerTypes, new NativeIntegerTypeSymbol[2], null);
            }

            int index = underlyingType.SpecialType switch
            {
                SpecialType.System_IntPtr => 0,
                SpecialType.System_UIntPtr => 1,
                _ => throw ExceptionUtilities.UnexpectedValue(underlyingType.SpecialType),
            };

            if (_lazyNativeIntegerTypes[index] is null)
            {
                Interlocked.CompareExchange(ref _lazyNativeIntegerTypes[index], new NativeIntegerTypeSymbol(underlyingType), null);
            }

            return _lazyNativeIntegerTypes[index];
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
        private Symbol[] _lazySpecialTypeMembers;

        /// <summary>
        /// Lookup member declaration in predefined CorLib type in this Assembly. Only valid if this 
        /// assembly is the Cor Library
        /// </summary>
        internal override Symbol GetDeclaredSpecialTypeMember(SpecialMember member)
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
                NamedTypeSymbol type = GetDeclaredSpecialType(descriptor.DeclaringSpecialType);
                Symbol result = null;

                if (!type.IsErrorType())
                {
                    result = CSharpCompilation.GetRuntimeMember(type, descriptor, CSharpCompilation.SpecialMembersSignatureComparer.Instance, accessWithinOpt: null);
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
        private ConcurrentDictionary<AssemblySymbol, IVTConclusion> _assembliesToWhichInternalAccessHasBeenAnalyzed;

        private ConcurrentDictionary<AssemblySymbol, IVTConclusion> AssembliesToWhichInternalAccessHasBeenDetermined
        {
            get
            {
                if (_assembliesToWhichInternalAccessHasBeenAnalyzed == null)
                    Interlocked.CompareExchange(ref _assembliesToWhichInternalAccessHasBeenAnalyzed, new ConcurrentDictionary<AssemblySymbol, IVTConclusion>(), null);
                return _assembliesToWhichInternalAccessHasBeenAnalyzed;
            }
        }

        internal sealed override TypeConversions TypeConversions
        {
            get
            {
                if (this != CorLibrary)
                {
                    return CorLibrary.TypeConversions;
                }

                if (_lazyTypeConversions is null)
                {
                    Interlocked.CompareExchange(ref _lazyTypeConversions, new TypeConversions(this), null);
                }

                return _lazyTypeConversions;
            }
        }

        internal virtual bool IsNetModule() => false;
    }
}
