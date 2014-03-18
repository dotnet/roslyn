// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        // These are relative to an accessing symbol, as inaccessible fields are
        // not "considered" for flow analysis (C# language design decision April 2013)
        private readonly NamedTypeSymbol accessor;

        private SmallDictionary<NamedTypeSymbol, bool> cache;
        private SmallDictionary<NamedTypeSymbol, bool> Cache
        {
            get
            {
                return this.cache ?? (this.cache = new SmallDictionary<NamedTypeSymbol, bool>());
            }
        }

        internal EmptyStructTypeCache(NamedTypeSymbol accessor)
        {
            this.accessor = accessor;
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
            var nts = type.OriginalDefinition as NamedTypeSymbol;
            if ((object)nts == null) return false;
            return nts.IsStructType() && nts.SpecialType == SpecialType.None && !nts.KnownCircularStruct;
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
                var field = GetActualInstanceField(member, type);

                if ((object)field != null)
                {
                    var actualFiledType = field.Type;
                    if (!IsEmptyStructType(actualFiledType, typesWithMembersOfThisType))
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

            return GetStructInstanceFields(nts);
        }

        public IEnumerable<FieldSymbol> GetStructInstanceFields(NamedTypeSymbol type)
        {
            // PERF: we get members of the OriginalDefinition to not create substituted members/types 
            //       unless necessary.
            foreach (var member in type.OriginalDefinition.GetMembersUnordered())
            {
                var field = GetActualInstanceField(member, type);

                if ((object)field != null)
                {
                    yield return field;
                }
            }
        }

        private FieldSymbol GetActualInstanceField(Symbol member, NamedTypeSymbol type)
        {
            if (!member.IsStatic)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                switch (member.Kind)
                {
                    case SymbolKind.Field:
                        var field = (FieldSymbol)member;
                        if (!field.IsFixed && ((object)this.accessor == null || AccessCheck.IsSymbolAccessible(field, this.accessor, ref useSiteDiagnostics)))
                        {
                            return field.AsMember(type);
                        }
                        break;

                    case SymbolKind.Event:
                        EventSymbol eventSymbol = (EventSymbol)member;
                        if (eventSymbol.HasAssociatedField && ((object)this.accessor == null || AccessCheck.IsSymbolAccessible(eventSymbol, this.accessor, ref useSiteDiagnostics)))
                        {
                            return eventSymbol.AssociatedField.AsMember(type);
                        }
                        break;
                }
            }
            return null;
        }

    }

    /// <summary>
    /// Specialized EmptyStructTypeCache that reports all structs as not empty
    /// </summary>
    internal sealed class CaptureWalkerEmptyStructTypeCache : EmptyStructTypeCache
    {
        public CaptureWalkerEmptyStructTypeCache()
           : base(null)
        {
        }

        public override bool IsEmptyStructType(TypeSymbol type)
        {
            return false;
        }
    }
}
