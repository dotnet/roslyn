// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A small cache for remembering empty struct types for flow analysis.
    /// </summary>
    internal class EmptyStructTypeCache
    {
        private SmallDictionary<NamedTypeSymbol, bool> _cache;

        /// <summary>
        /// When set, we ignore private reference fields of structs loaded from metadata.
        /// </summary>
        private readonly bool _dev12CompilerCompatibility;

        private readonly SourceAssemblySymbol _sourceAssembly;

        private SmallDictionary<NamedTypeSymbol, bool> Cache
        {
            get
            {
                return _cache ?? (_cache = new SmallDictionary<NamedTypeSymbol, bool>(SymbolEqualityComparer.ConsiderEverything));
            }
        }

        public static EmptyStructTypeCache CreateForDev12Compatibility(Compilation compilation)
            => new EmptyStructTypeCache(compilation, dev12CompilerCompatibility: true);

        public static EmptyStructTypeCache CreatePrecise()
            => new EmptyStructTypeCache(null, false);

        public static EmptyStructTypeCache CreateNeverEmpty()
            => new NeverEmptyStructTypeCache();

        /// <summary>
        /// Create a cache for computing whether or not a struct type is "empty".
        /// </summary>
        /// <param name="dev12CompilerCompatibility">Enable compatibility with the native compiler, which
        ///  ignores inaccessible fields of reference type for structs loaded from metadata.</param>
        /// <param name="compilation">if <see cref="_dev12CompilerCompatibility"/> is true, set to the compilation from
        /// which to check accessibility.</param>
        private EmptyStructTypeCache(Compilation compilation, bool dev12CompilerCompatibility)
        {
            Debug.Assert(compilation != null || !dev12CompilerCompatibility);
            _dev12CompilerCompatibility = dev12CompilerCompatibility;
            _sourceAssembly = (SourceAssemblySymbol)compilation?.Assembly;
        }

        /// <summary>
        /// Specialized EmptyStructTypeCache that reports all structs as not empty
        /// </summary>
        private sealed class NeverEmptyStructTypeCache : EmptyStructTypeCache
        {
            public NeverEmptyStructTypeCache()
               : base(null, false)
            {
            }

            public override bool IsEmptyStructType(TypeSymbol type)
            {
                return false;
            }
        }

        /// <summary>
        /// Determine if the given type is an empty struct type.
        /// </summary>
        public virtual bool IsEmptyStructType(TypeSymbol type)
        {
            return IsEmptyStructType(type, ConsList<NamedTypeSymbol>.Empty);
        }

        /// <summary>
        /// Determine if the given type is an empty struct type,. "typesWithMembersOfThisType" contains
        /// a list of types that have members (directly or indirectly) of this type.
        /// to remove circularity.
        /// </summary>
        private bool IsEmptyStructType(TypeSymbol type, ConsList<NamedTypeSymbol> typesWithMembersOfThisType)
        {
            var nts = type as NamedTypeSymbol;
            if ((object)nts == null || !IsTrackableStructType(nts))
            {
                return false;
            }

            // Consult the cache.
            bool result;
            if (Cache.TryGetValue(nts, out result))
            {
                return result;
            }

            result = CheckStruct(typesWithMembersOfThisType, nts);
            Debug.Assert(!Cache.ContainsKey(nts) || Cache[nts] == result);
            Cache[nts] = result;

            return result;
        }

        private bool CheckStruct(ConsList<NamedTypeSymbol> typesWithMembersOfThisType, NamedTypeSymbol nts)
        {
            // Break recursive cycles. If we find a member that contains us, it is considered empty 
            if (!typesWithMembersOfThisType.ContainsReference(nts))
            {
                // Remember that we're in the process of doing this type while checking members.
                typesWithMembersOfThisType = new ConsList<NamedTypeSymbol>(nts, typesWithMembersOfThisType);
                return CheckStructInstanceFields(typesWithMembersOfThisType, nts);
            }

            return true;
        }

        public static bool IsTrackableStructType(TypeSymbol type)
        {
            if ((object)type == null) return false;
            var nts = type.OriginalDefinition as NamedTypeSymbol;
            if ((object)nts == null) return false;
            return nts.IsStructType() && nts is
            {
                SpecialType: SpecialType.None,
                KnownCircularStruct: false
            };
        }

        /// <summary>
        /// Get all instance fields of a struct. They are not necessarily returned in order.
        /// </summary>
        private bool CheckStructInstanceFields(ConsList<NamedTypeSymbol> typesWithMembersOfThisType, NamedTypeSymbol type)
        {
            // PERF: we get members of the OriginalDefinition to not create substituted members/types 
            //       unless necessary.
            foreach (var member in type.OriginalDefinition.GetMembersUnordered())
            {
                if (member.IsStatic)
                {
                    continue;
                }
                var field = GetActualField(member, type);
                if ((object)field != null)
                {
                    var actualFieldType = field.Type;
                    if (!IsEmptyStructType(actualFieldType, typesWithMembersOfThisType))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Get all instance fields of a struct. They are not necessarily returned in order.
        /// </summary>
        ///
        public IEnumerable<FieldSymbol> GetStructInstanceFields(TypeSymbol type)
        {
            var nts = type as NamedTypeSymbol;
            if ((object)nts == null)
            {
                return SpecializedCollections.EmptyEnumerable<FieldSymbol>();
            }

            return GetStructFields(nts, includeStatic: false);
        }

        public IEnumerable<FieldSymbol> GetStructFields(NamedTypeSymbol type, bool includeStatic)
        {
            // PERF: we get members of the OriginalDefinition to not create substituted members/types 
            //       unless necessary.
            foreach (var member in type.OriginalDefinition.GetMembersUnordered())
            {
                if (!includeStatic && member.IsStatic)
                {
                    continue;
                }
                var field = GetActualField(member, type);
                if ((object)field != null)
                {
                    yield return field;
                }
            }
        }

        private FieldSymbol GetActualField(Symbol member, NamedTypeSymbol type)
        {
            switch (member.Kind)
            {
                case SymbolKind.Field:
                    var field = (FieldSymbol)member;
                    // Do not report virtual tuple fields.
                    // They are additional aliases to the fields of the underlying struct or nested extensions.
                    // and as such are already accounted for via the nonvirtual fields.
                    if (field.IsVirtualTupleField)
                    {
                        return null;
                    }

                    return (field.IsFixedSizeBuffer || ShouldIgnoreStructField(field, field.Type)) ? null : field.AsMember(type);

                case SymbolKind.Event:
                    var eventSymbol = (EventSymbol)member;
                    return (!eventSymbol.HasAssociatedField || ShouldIgnoreStructField(eventSymbol, eventSymbol.Type)) ? null : eventSymbol.AssociatedField.AsMember(type);
            }

            return null;
        }

        private bool ShouldIgnoreStructField(Symbol member, TypeSymbol memberType)
        {
            return _dev12CompilerCompatibility &&                             // when we're trying to be compatible with the native compiler, we ignore
                   ((object)member.ContainingAssembly != _sourceAssembly ||   // imported fields
                    member.ContainingModule.Ordinal != 0) &&                      //     (an added module is imported)
                   IsIgnorableType(memberType) &&                                 // of reference type (but not type parameters, looking through arrays)
                   !IsAccessibleInAssembly(member, _sourceAssembly);          // that are inaccessible to our assembly.
        }

        /// <summary>
        /// When deciding what struct fields to drop on the floor, the native compiler looks
        /// through arrays, and does not ignore value types or type parameters.
        /// </summary>
        private static bool IsIgnorableType(TypeSymbol type)
        {
            while (true)
            {
                switch (type.TypeKind)
                {
                    case TypeKind.Enum:
                    case TypeKind.Struct:
                    case TypeKind.TypeParameter:
                        return false;
                    case TypeKind.Array:
                        type = ((ArrayTypeSymbol)type).BaseTypeNoUseSiteDiagnostics;
                        continue;
                    default:
                        return true;
                }
            }
        }

        /// <summary>
        /// Is it possible that the given symbol can be accessed somewhere in the given assembly?
        /// For the purposes of this test, we assume that code in the given assembly might derive from
        /// any type. So protected members are considered potentially accessible.
        /// </summary>
        private static bool IsAccessibleInAssembly(Symbol symbol, SourceAssemblySymbol assembly)
        {
            for (; symbol != null && symbol.Kind != SymbolKind.Namespace; symbol = symbol.ContainingSymbol)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Internal:
                    case Accessibility.ProtectedAndInternal:
                        if (!assembly.HasInternalAccessTo(symbol.ContainingAssembly)) return false;
                        break;

                    case Accessibility.Private:
                        return false;
                }
            }

            return true;
        }
    }
}
