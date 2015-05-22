// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class ObsoleteAttributeHelpers
    {
        /// <summary>
        /// Initialize the ObsoleteAttributeData by fetching attributes and decoding ObsoleteAttributeData. This can be 
        /// done for Metadata symbol easily whereas trying to do this for source symbols could result in cycles.
        /// </summary>
        internal static void InitializeObsoleteDataFromMetadata(ref ObsoleteAttributeData data, EntityHandle token, PEModuleSymbol containingModule)
        {
            if (ReferenceEquals(data, ObsoleteAttributeData.Uninitialized))
            {
                ObsoleteAttributeData obsoleteAttributeData = GetObsoleteDataFromMetadata(token, containingModule);
                Interlocked.CompareExchange(ref data, obsoleteAttributeData, ObsoleteAttributeData.Uninitialized);
            }
        }

        /// <summary>
        /// Get the ObsoleteAttributeData by fetching attributes and decoding ObsoleteAttributeData. This can be 
        /// done for Metadata symbol easily whereas trying to do this for source symbols could result in cycles.
        /// </summary>
        internal static ObsoleteAttributeData GetObsoleteDataFromMetadata(EntityHandle token, PEModuleSymbol containingModule)
        {
            ObsoleteAttributeData obsoleteAttributeData;
            bool isObsolete = containingModule.Module.HasDeprecatedOrObsoleteAttribute(token, out obsoleteAttributeData);
            Debug.Assert(isObsolete == (obsoleteAttributeData != null));
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
        internal static ThreeState GetObsoleteContextState(Symbol symbol, bool forceComplete = false)
        {
            if ((object)symbol == null)
                return ThreeState.False;

            // For Property or Event accessors, check the associated property or event instead.
            if (symbol.IsAccessor())
            {
                symbol = ((MethodSymbol)symbol).AssociatedSymbol;
            }
            // If this is the backing field of an event, look at the event instead.
            else if (symbol.Kind == SymbolKind.Field && (object)((FieldSymbol)symbol).AssociatedSymbol != null)
            {
                symbol = ((FieldSymbol)symbol).AssociatedSymbol;
            }

            if (forceComplete)
            {
                symbol.ForceCompleteObsoleteAttribute();
            }

            if (symbol.ObsoleteState != ThreeState.False)
            {
                return symbol.ObsoleteState;
            }

            return GetObsoleteContextState(symbol.ContainingSymbol, forceComplete);
        }

        /// <summary>
        /// Create a diagnostic for the given symbol. This could be an error or a warning based on
        /// the ObsoleteAttribute's arguments.
        /// </summary>
        internal static DiagnosticInfo CreateObsoleteDiagnostic(Symbol symbol, BinderFlags location)
        {
            var data = symbol.ObsoleteAttributeData;

            if (data == null)
            {
                // ObsoleteAttribute had errors.
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

            // Issue a specialized diagnostic for add methods of collection initializers
            bool isColInit = location.Includes(BinderFlags.CollectionInitializerAddMethod);

            if (data.Message == null)
            {
                // It seems like we should be able to assert that data.IsError is false, but we can't because dev11 had
                // a bug in this area (i.e. always produce a warning when there's no message) and we have to match it.
                // Debug.Assert(!data.IsError);
                return new CSDiagnosticInfo(isColInit ? ErrorCode.WRN_DeprecatedCollectionInitAdd : ErrorCode.WRN_DeprecatedSymbol, symbol);
            }
            else
            {
                ErrorCode errorCode = data.IsError
                    ? (isColInit ? ErrorCode.ERR_DeprecatedCollectionInitAddStr : ErrorCode.ERR_DeprecatedSymbolStr)
                    : (isColInit ? ErrorCode.WRN_DeprecatedCollectionInitAddStr : ErrorCode.WRN_DeprecatedSymbolStr);
                return new CSDiagnosticInfo(errorCode, symbol, data.Message);
            }
        }
    }
}
