// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class HostObjectModelBinder : Binder
    {
        public HostObjectModelBinder(Binder next)
            : base(next)
        {
        }

        private TypeSymbol GetHostObjectType()
        {
            TypeSymbol result = this.Compilation.GetHostObjectTypeSymbol();

            // This binder shouldn't be created if the compilation doesn't have host object type:
            Debug.Assert((object)result != null);

            return result;
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var hostObjectType = GetHostObjectType();
            if (hostObjectType.Kind == SymbolKind.ErrorType)
            {
                // The name '{0}' does not exist in the current context (are you missing a reference to assembly '{1}'?)
                result.SetFrom(new CSDiagnosticInfo(
                    ErrorCode.ERR_NameNotInContextPossibleMissingReference,
                    new object[] { name, ((MissingMetadataTypeSymbol)hostObjectType).ContainingAssembly.Identity },
                    ImmutableArray<Symbol>.Empty,
                    ImmutableArray<Location>.Empty
                ));
            }
            else
            {
                LookupMembersInternal(result, hostObjectType, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteInfo);
            }
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            var hostObjectType = GetHostObjectType();
            if (hostObjectType.Kind != SymbolKind.ErrorType)
            {
                AddMemberLookupSymbolsInfo(result, hostObjectType, options, originalBinder);
            }
        }
    }
}
