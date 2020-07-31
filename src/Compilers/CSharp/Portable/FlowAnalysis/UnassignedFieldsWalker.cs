// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    /// cref="LocalDataFlowPass{TLocalState, TLocalFunctionState}"/> directly since it has simpler
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

        public override BoundNode VisitCall(BoundCall node)
        {
            var result = base.VisitCall(node);
            var method = node.Method;
            if (method.IsStatic || node.ReceiverOpt is BoundThisReference)
            {
                ApplyMemberPostConditions(method.ContainingType,
                    method.NotNullMembers, method.NotNullWhenTrueMembers, method.NotNullWhenFalseMembers);
            }
            return result;
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            var result = base.VisitPropertyAccess(node);
            var property = node.PropertySymbol;
            if (property.IsStatic || node.ReceiverOpt is BoundThisReference)
            {
                var accessor = property.GetMethod;
                if (!(accessor is null))
                {
                    ApplyMemberPostConditions(property.ContainingType,
                        accessor.NotNullMembers, accessor.NotNullWhenTrueMembers, accessor.NotNullWhenFalseMembers);
                }
            }

            return result;
        }

        protected override void PropertySetter(BoundExpression node, BoundExpression receiver, MethodSymbol setter, BoundExpression value = null)
        {
            base.PropertySetter(node, receiver, setter, value);

            if (receiver is null || receiver is BoundThisReference)
            {
                ApplyMemberPostConditions(setter.ContainingType, setter.NotNullMembers, notNullWhenTrueMembers: default, notNullWhenFalseMembers: default);
            }
        }

        private void ApplyMemberPostConditions(
            TypeSymbol containingType,
            ImmutableArray<string> notNullMembers,
            ImmutableArray<string> notNullWhenTrueMembers,
            ImmutableArray<string> notNullWhenFalseMembers)
        {
            applyMemberPostConditions(notNullMembers, ref State);

            if (!notNullWhenTrueMembers.IsDefaultOrEmpty || !notNullWhenFalseMembers.IsDefaultOrEmpty)
            {
                Split();
                applyMemberPostConditions(notNullWhenTrueMembers, ref StateWhenTrue);
                applyMemberPostConditions(notNullWhenFalseMembers, ref StateWhenFalse);
            }

            void applyMemberPostConditions(ImmutableArray<string> notNullMembers, ref LocalState state)
            {
                if (notNullMembers.IsEmpty)
                {
                    return;
                }

                foreach (var notNullMember in notNullMembers)
                {
                    markMemberAsAssigned(notNullMember, ref state);
                }
            }

            void markMemberAsAssigned(string notNullMember, ref LocalState state)
            {
                foreach (Symbol member in containingType.GetMembers(notNullMember))
                {
                    switch (member.Kind)
                    {
                        case SymbolKind.Field:
                        case SymbolKind.Property:
                            int thisSlot = -1;
                            bool isStatic = member.IsStatic;
                            if (!isStatic)
                            {
                                thisSlot = GetOrCreateSlot(MethodThisParameter);
                                if (thisSlot < 0)
                                {
                                    continue;
                                }
                                Debug.Assert(thisSlot > 0);
                            }

                            var memberSlot = GetOrCreateSlot(member, isStatic ? 0 : thisSlot);
                            if (memberSlot >= 0)
                            {
                                SetSlotAssigned(memberSlot, ref state);
                            }

                            break;
                        case SymbolKind.Event:
                        case SymbolKind.Method:
                            break;
                    }
                }
            }
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
                if (!fieldType.NullableAnnotation.IsNotAnnotated())
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
                if ((symbol.GetFlowAnalysisAnnotations() & FlowAnalysisAnnotations.MaybeNull) != 0)
                {
                    continue;
                }
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
