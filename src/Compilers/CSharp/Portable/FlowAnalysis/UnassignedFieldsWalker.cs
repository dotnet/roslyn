// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    // PROTOTYPE(NullableReferenceTypes): Should UnassignedFieldsWalker
    // inherit from DataFlowPassBase<LocalState> directly since it has simpler
    // requirements than DataFlowPass?
    internal sealed class UnassignedFieldsWalker : DataFlowPass
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

            var flags = ((CSharpParseOptions)node.SyntaxTree.Options).GetNullableReferenceFlags();
            if ((flags & NullableReferenceFlags.Enabled) == 0)
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
                    // PROTOTYPE(NullableReferenceTypes): Handle events.
                    continue;
                }
                var field = (FieldSymbol)member;
                // PROTOTYPE(NullableReferenceTypes): Can the HasInitializer
                // call be removed, if the body already contains the initializers?
                if (field.IsStatic || HasInitializer(field))
                {
                    continue;
                }
                var fieldType = field.Type;
                if (!fieldType.IsReferenceType || fieldType.IsNullable != false)
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
