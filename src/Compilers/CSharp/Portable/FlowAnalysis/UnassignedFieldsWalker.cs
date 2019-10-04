// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
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
            : base(compilation, method, node, trackClassFields: true, trackStaticMembers: method.MethodKind == MethodKind.StaticConstructor)
        {
            _diagnostics = diagnostics;
        }

        internal static void Analyze(CSharpCompilation compilation, MethodSymbol method, BoundNode node, DiagnosticBag diagnostics)
        {
            Debug.Assert(method.IsConstructor());

            if (compilation.LanguageVersion < MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion())
            {
                return;
            }

            if (HasThisConstructorInitializer(method))
            {
                return;
            }

            if (!method.IsStatic && method.ContainingType.IsValueType)
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
            Debug.Assert(_symbol.IsDefinition);

            if (!State.Reachable)
            {
                return;
            }

            var method = (MethodSymbol)_symbol;
            bool isStatic = !method.RequiresInstanceReceiver();

            int thisSlot = -1;

            if (!isStatic)
            {
                method.TryGetThisParameter(out var thisParameter);
                thisSlot = VariableSlot(thisParameter);
                Debug.Assert(thisSlot > 0);
            }

            var containingType = method.ContainingType;
            var members = containingType.GetMembersUnordered();

            ReportUninitializedNonNullableReferenceTypeFields(
                this,
                thisSlot,
                isStatic,
                members,
                (walker, slot, member) => walker.GetIsSymbolAssigned(slot, member),
                (walker, member) => walker.GetSymbolForLocation(member),
                _diagnostics);
        }

        private bool GetIsSymbolAssigned(int thisSlot, Symbol symbol)
        {
            if (HasInitializer(symbol))
            {
                return true;
            }
            int slot = VariableSlot(symbol, symbol.IsStatic ? 0 : thisSlot);
            return slot > 0 && this.State.IsAssigned(slot);
        }

        private Symbol GetSymbolForLocation(Symbol symbol)
        {
            return topLevelMethod.DeclaringSyntaxReferences.IsEmpty
                ? symbol // default constructor, use the field location
                : topLevelMethod;
        }

        internal static void ReportUninitializedNonNullableReferenceTypeFields(
            UnassignedFieldsWalker walkerOpt,
            int thisSlot,
            bool isStatic,
            ImmutableArray<Symbol> members,
            Func<UnassignedFieldsWalker, int, Symbol, bool> getIsAssigned,
            Func<UnassignedFieldsWalker, Symbol, Symbol> getSymbolForLocation,
            DiagnosticBag diagnostics)
        {
            foreach (var member in members)
            {
                if (member.IsStatic != isStatic)
                {
                    continue;
                }
                TypeWithAnnotations fieldType;
                FieldSymbol field;
                switch (member)
                {
                    case FieldSymbol f:
                        fieldType = f.TypeWithAnnotations;
                        field = f;
                        break;
                    case EventSymbol e:
                        fieldType = e.TypeWithAnnotations;
                        field = e.AssociatedField;
                        if (field is null)
                        {
                            continue;
                        }
                        break;
                    default:
                        continue;
                }
                if (field.IsConst)
                {
                    continue;
                }
                if (fieldType.Type.IsValueType || fieldType.Type.IsErrorType())
                {
                    continue;
                }
                if (!fieldType.NullableAnnotation.IsNotAnnotated() && !fieldType.Type.IsTypeParameterDisallowingAnnotation())
                {
                    continue;
                }
                if (getIsAssigned(walkerOpt, thisSlot, field))
                {
                    continue;
                }
                var symbol = member switch
                {
                    FieldSymbol { AssociatedSymbol: PropertySymbol p } => p,
                    _ => member
                };
                var location = getSymbolForLocation(walkerOpt, symbol).Locations.FirstOrNone();
                diagnostics.Add(ErrorCode.WRN_UninitializedNonNullableField, location, symbol.Kind.Localize(), symbol.Name);
            }
        }

        public override BoundNode VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        {
            base.VisitEventAssignmentOperator(node);
            if (node.IsAddition && node is { Event: { AssociatedField: { } field } })
            {
                int slot = MakeMemberSlot(node.ReceiverOpt, field);
                if (slot != -1)
                {
                    SetSlotState(slot, assigned: true);
                }
            }
            return null;
        }
    }
}
