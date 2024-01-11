// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal static class LocalSymbolExtensions
    {
        internal static LocalSymbol ToOtherMethod(this LocalSymbol local, MethodSymbol method, TypeMap typeMap)
        {
            var l = local as EELocalSymbolBase;
            if ((object)l != null)
            {
                return l.ToOtherMethod(method, typeMap);
            }
            var type = typeMap.SubstituteType(local.TypeWithAnnotations);
            return new EELocalSymbol(method, local.Locations, local.Name, -1, local.DeclarationKind, type, local.RefKind, local.IsPinned, local.IsCompilerGenerated, local.CanScheduleToStack);
        }
    }

    internal abstract class EELocalSymbolBase : LocalSymbol
    {
        internal static readonly ImmutableArray<Location> NoLocations = ImmutableArray.Create(NoLocation.Singleton);

        internal abstract EELocalSymbolBase ToOtherMethod(MethodSymbol method, TypeMap typeMap);

        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, BindingDiagnosticBag diagnostics)
        {
            return null;
        }

        internal override ReadOnlyBindingDiagnostic<AssemblySymbol> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return ReadOnlyBindingDiagnostic<AssemblySymbol>.Empty;
        }

        internal sealed override SynthesizedLocalKind SynthesizedKind
        {
            get { return SynthesizedLocalKind.UserDefined; }
        }

        internal override SyntaxNode ScopeDesignatorOpt
        {
            get { return null; }
        }

        internal sealed override LocalSymbol WithSynthesizedLocalKindAndSyntax(
            SynthesizedLocalKind kind, SyntaxNode syntax
#if DEBUG
            ,
            [CallerLineNumber] int createdAtLineNumber = 0,
            [CallerFilePath] string createdAtFilePath = null
#endif
            )
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override bool IsImportedFromMetadata
        {
            get { return true; }
        }

        internal override SyntaxNode GetDeclaratorSyntax()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool HasSourceLocation => false;

        internal sealed override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            var type = this.TypeWithAnnotations;
            UseSiteInfo<AssemblySymbol> result = default;
            if (!DeriveUseSiteInfoFromType(ref result, type, AllowedRequiredModifierType.None) && this.ContainingModule.HasUnifiedReferences)
            {
                // If the member is in an assembly with unified references, 
                // we check if its definition depends on a type from a unified reference.
                HashSet<TypeSymbol> unificationCheckedTypes = null;
                var diagnosticInfo = result.DiagnosticInfo;
                type.GetUnificationUseSiteDiagnosticRecursive(ref diagnosticInfo, this, ref unificationCheckedTypes);
                result = result.AdjustDiagnosticInfo(diagnosticInfo);
            }

            return result;
        }

        internal override ScopedKind Scope => ScopedKind.None;
    }
}
