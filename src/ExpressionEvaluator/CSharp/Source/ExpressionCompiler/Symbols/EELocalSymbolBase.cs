// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            var type = typeMap.SubstituteType(local.Type);
            return new EELocalSymbol(method, local.Locations, local.Name, -1, local.DeclarationKind, type.Type, local.RefKind, local.IsPinned, local.IsCompilerGenerated, local.CanScheduleToStack);
        }
    }

    internal abstract class EELocalSymbolBase : LocalSymbol
    {
        internal static readonly ImmutableArray<Location> NoLocations = ImmutableArray.Create(NoLocation.Singleton);

        internal abstract EELocalSymbolBase ToOtherMethod(MethodSymbol method, TypeMap typeMap);

        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, DiagnosticBag diagnostics)
        {
            return null;
        }

        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return ImmutableArray<Diagnostic>.Empty;
        }

        internal sealed override SynthesizedLocalKind SynthesizedKind
        {
            get { return SynthesizedLocalKind.UserDefined; }
        }

        internal sealed override LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal sealed override bool IsImportedFromMetadata
        {
            get { return true; }
        }

        internal override SyntaxNode GetDeclaratorSyntax()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal sealed override DiagnosticInfo GetUseSiteDiagnostic()
        {
            var type = this.Type;
            DiagnosticInfo result = null;
            if (!DeriveUseSiteDiagnosticFromType(ref result, type) && this.ContainingModule.HasUnifiedReferences)
            {
                // If the member is in an assembly with unified references, 
                // we check if its definition depends on a type from a unified reference.
                HashSet<TypeSymbol> unificationCheckedTypes = null;
                type.GetUnificationUseSiteDiagnosticRecursive(ref result, this, ref unificationCheckedTypes);
            }
            return result;
        }
    }
}
