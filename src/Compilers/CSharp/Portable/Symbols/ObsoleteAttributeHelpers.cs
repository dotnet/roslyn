﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal enum ObsoleteDiagnosticKind
    {
        NotObsolete,
        Suppressed,
        Diagnostic,
        Lazy,
        LazyPotentiallySuppressed,
    }

    internal static class ObsoleteAttributeHelpers
    {
        /// <summary>
        /// Initialize the ObsoleteAttributeData by fetching attributes and decoding ObsoleteAttributeData. This can be 
        /// done for Metadata symbol easily whereas trying to do this for source symbols could result in cycles.
        /// </summary>
        internal static void InitializeObsoleteDataFromMetadata(ref ObsoleteAttributeData data, EntityHandle token, PEModuleSymbol containingModule, bool ignoreByRefLikeMarker)
        {
            if (ReferenceEquals(data, ObsoleteAttributeData.Uninitialized))
            {
                ObsoleteAttributeData obsoleteAttributeData = GetObsoleteDataFromMetadata(token, containingModule, ignoreByRefLikeMarker);
                Interlocked.CompareExchange(ref data, obsoleteAttributeData, ObsoleteAttributeData.Uninitialized);
            }
        }

        /// <summary>
        /// Get the ObsoleteAttributeData by fetching attributes and decoding ObsoleteAttributeData. This can be 
        /// done for Metadata symbol easily whereas trying to do this for source symbols could result in cycles.
        /// </summary>
        internal static ObsoleteAttributeData GetObsoleteDataFromMetadata(EntityHandle token, PEModuleSymbol containingModule, bool ignoreByRefLikeMarker)
        {
            var obsoleteAttributeData = containingModule.Module.TryGetDeprecatedOrExperimentalOrObsoleteAttribute(token, new MetadataDecoder(containingModule), ignoreByRefLikeMarker);
            Debug.Assert(obsoleteAttributeData == null || !obsoleteAttributeData.IsUninitialized);
            return obsoleteAttributeData;
        }

        /// <summary>
        /// This method checks to see if the given symbol is Obsolete or if any symbol in the parent hierarchy is Obsolete.
        /// </summary>
        /// <returns>
        /// True if some symbol in the parent hierarchy is known to be Obsolete. Unknown if any
        /// symbol's Obsoleteness is Unknown. False, if we are certain that no symbol in the parent
        /// hierarchy is Obsolete.
        /// </returns>
        private static ThreeState GetObsoleteContextState(Symbol symbol, bool forceComplete)
        {
            while ((object)symbol != null)
            {
                if (symbol.Kind == SymbolKind.Field)
                {
                    // If this is the backing field of an event, look at the event instead.
                    var associatedSymbol = ((FieldSymbol)symbol).AssociatedSymbol;
                    if ((object)associatedSymbol != null)
                    {
                        symbol = associatedSymbol;
                    }
                }

                if (forceComplete)
                {
                    symbol.ForceCompleteObsoleteAttribute();
                }

                var state = symbol.ObsoleteState;
                if (state != ThreeState.False)
                {
                    return state;
                }

                // For property or event accessors, check the associated property or event next.
                if (symbol.IsAccessor())
                {
                    symbol = ((MethodSymbol)symbol).AssociatedSymbol;
                }
                else
                {
                    symbol = symbol.ContainingSymbol;
                }
            }

            return ThreeState.False;
        }

        internal static ObsoleteDiagnosticKind GetObsoleteDiagnosticKind(Symbol symbol, Symbol containingMember, bool forceComplete = false)
        {
            switch (symbol.ObsoleteKind)
            {
                case ObsoleteAttributeKind.None:
                    return ObsoleteDiagnosticKind.NotObsolete;
                case ObsoleteAttributeKind.Experimental:
                    return ObsoleteDiagnosticKind.Diagnostic;
                case ObsoleteAttributeKind.Uninitialized:
                    // If we haven't cracked attributes on the symbol at all or we haven't
                    // cracked attribute arguments enough to be able to report diagnostics for
                    // ObsoleteAttribute, store the symbol so that we can report diagnostics at a 
                    // later stage.
                    return ObsoleteDiagnosticKind.Lazy;
            }

            switch (GetObsoleteContextState(containingMember, forceComplete))
            {
                case ThreeState.False:
                    return ObsoleteDiagnosticKind.Diagnostic;
                case ThreeState.True:
                    // If we are in a context that is already obsolete, there is no point reporting
                    // more obsolete diagnostics.
                    return ObsoleteDiagnosticKind.Suppressed;
                default:
                    // If the context is unknown, then store the symbol so that we can do this check at a
                    // later stage
                    return ObsoleteDiagnosticKind.LazyPotentiallySuppressed;
            }
        }

        /// <summary>
        /// Create a diagnostic for the given symbol. This could be an error or a warning based on
        /// the ObsoleteAttribute's arguments.
        /// </summary>
        internal static DiagnosticInfo CreateObsoleteDiagnostic(Symbol symbol, BinderFlags location)
        {
            var data = symbol.ObsoleteAttributeData;
            Debug.Assert(data != null);

            if (data == null)
            {
                return null;
            }

            // At this point, we are going to issue diagnostics and therefore the data shouldn't be
            // uninitialized.
            Debug.Assert(!data.IsUninitialized);

            // The native compiler suppresses Obsolete diagnostics in these locations.
            if (location.Includes(BinderFlags.SuppressObsoleteChecks))
            {
                return null;
            }

            if (data.Kind == ObsoleteAttributeKind.Experimental)
            {
                Debug.Assert(data.Message == null);
                Debug.Assert(!data.IsError);
                // Provide an explicit format for fully-qualified type names.
                return new CSDiagnosticInfo(ErrorCode.WRN_Experimental, new FormattedSymbol(symbol, SymbolDisplayFormat.CSharpErrorMessageFormat));
            }

            // Issue a specialized diagnostic for add methods of collection initializers
            var isColInit = location.Includes(BinderFlags.CollectionInitializerAddMethod);
            var errorCode = (message: data.Message, isError: data.IsError, isColInit) switch
            {
                // dev11 had a bug in this area (i.e. always produce a warning when there's no message) and we have to match it.
                (message: null, isError: _, isColInit: true) => ErrorCode.WRN_DeprecatedCollectionInitAdd,
                (message: null, isError: _, isColInit: false) => ErrorCode.WRN_DeprecatedSymbol,
                (message: { }, isError: true, isColInit: true) => ErrorCode.ERR_DeprecatedCollectionInitAddStr,
                (message: { }, isError: true, isColInit: false) => ErrorCode.ERR_DeprecatedSymbolStr,
                (message: { }, isError: false, isColInit: true) => ErrorCode.WRN_DeprecatedCollectionInitAddStr,
                (message: { }, isError: false, isColInit: false) => ErrorCode.WRN_DeprecatedSymbolStr
            };

            var arguments = data.Message is string message
                ? new object[] { symbol, message }
                : new object[] { symbol };

            return new CustomObsoleteDiagnosticInfo(MessageProvider.Instance, (int)errorCode, data, arguments);
        }
    }
}
