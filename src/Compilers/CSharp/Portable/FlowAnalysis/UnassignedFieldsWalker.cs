// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <remarks>
    /// https://github.com/dotnet/roslyn/issues/30067 Should
    /// UnassignedFieldsWalker inherit from <see
    /// cref="LocalDataFlowPass{TLocalState}"/> directly since it has simpler
    /// requirements than <see cref="DefiniteAssignmentPass"/>?
    /// </remarks>
    internal sealed class UnassignedFieldsWalker : DefiniteAssignmentPass
    {
        private readonly DiagnosticBag _diagnostics;

        private UnassignedFieldsWalker(CSharpCompilation compilation, MethodSymbol method, BoundNode node, DiagnosticBag diagnostics)
            : base(compilation, method, node, trackClassFields: true)
        {
            _diagnostics = diagnostics;
        }

        internal static void Analyze(CSharpCompilation compilation, MethodSymbol method, BoundNode node, DiagnosticBag diagnostics)
        {
            Debug.Assert(method.MethodKind == MethodKind.Constructor);

            if (compilation.LanguageVersion < MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion())
            {
                return;
            }

            if (HasThisConstructorInitializer(method))
            {
                return;
            }

            var walker = new UnassignedFieldsWalker(compilation, method, node, diagnostics);
            try
            {
                bool badRegion = false;
                walker.Analyze(ref badRegion, diagnostics: null);
            }
            finally
            {
                walker.Free();
            }
        }

        private static bool HasThisConstructorInitializer(MethodSymbol method)
        {
            Debug.Assert(method.DeclaringSyntaxReferences.Length <= 1);
            var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ConstructorDeclarationSyntax;
            return syntax?.Initializer?.Kind() == SyntaxKind.ThisConstructorInitializer;
        }

        protected override ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            var result = base.Scan(ref badRegion);
            ReportUninitializedNonNullableReferenceTypeFields();
            return result;
        }

        private void ReportUninitializedNonNullableReferenceTypeFields()
        {
            var thisParameter = MethodThisParameter;
            var location = thisParameter.ContainingSymbol.Locations.FirstOrDefault() ?? Location.None;

            int thisSlot = VariableSlot(thisParameter);
            if (thisSlot == -1)
            {
                return;
            }

            var thisType = thisParameter.Type.TypeSymbol;
            Debug.Assert(thisType.IsDefinition);

            foreach (var member in thisType.GetMembersUnordered())
            {
                if (member.Kind != SymbolKind.Field)
                {
                    // https://github.com/dotnet/roslyn/issues/30067 Handle events.
                    continue;
                }
                var field = (FieldSymbol)member;
                // https://github.com/dotnet/roslyn/issues/30067 Can the HasInitializer
                // call be removed, if the body already contains the initializers?
                if (field.IsStatic || HasInitializer(field))
                {
                    continue;
                }
                var fieldType = field.Type;
                if (fieldType.IsValueType || fieldType.IsErrorType())
                {
                    continue;
                }
                if (!fieldType.NullableAnnotation.IsAnyNotNullable() && !fieldType.TypeSymbol.IsUnconstrainedTypeParameter())
                {
                    continue;
                }
                int fieldSlot = VariableSlot(field, thisSlot);
                if (fieldSlot == -1 || !this.State.IsAssigned(fieldSlot))
                {
                    var symbol = (Symbol)(field.AssociatedSymbol as PropertySymbol) ?? field;
                    _diagnostics.Add(ErrorCode.WRN_UninitializedNonNullableField, location, symbol.Kind.Localize(), symbol.Name);
                }
            }
        }
    }
}
