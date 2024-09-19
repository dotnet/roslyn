// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            return VisitPropertyAccess(node, isLeftOfAssignment: false);
        }

        private BoundExpression VisitPropertyAccess(BoundPropertyAccess node, bool isLeftOfAssignment)
        {
            var rewrittenReceiverOpt = VisitExpression(node.ReceiverOpt);
            return MakePropertyAccess(node.Syntax, rewrittenReceiverOpt, node.PropertySymbol, node.ResultKind, node.Type, isLeftOfAssignment, node);
        }

        private BoundExpression MakePropertyAccess(
            SyntaxNode syntax,
            BoundExpression? rewrittenReceiverOpt,
            PropertySymbol propertySymbol,
            LookupResultKind resultKind,
            TypeSymbol type,
            bool isLeftOfAssignment,
            BoundPropertyAccess? oldNodeOpt = null)
        {
            // check for System.Array.[Length|LongLength] on a single dimensional array,
            // we have a special node for such cases.
            if (rewrittenReceiverOpt is { Type: { TypeKind: TypeKind.Array } } && !isLeftOfAssignment)
            {
                var asArrayType = (ArrayTypeSymbol)rewrittenReceiverOpt.Type;
                if (asArrayType.IsSZArray)
                {
                    // NOTE: we are not interested in potential badness of Array.Length property.
                    // If it is bad reference compare will not succeed.
                    if (ReferenceEquals(propertySymbol, _compilation.GetSpecialTypeMember(SpecialMember.System_Array__Length)) ||
                        !_inExpressionLambda && ReferenceEquals(propertySymbol, _compilation.GetSpecialTypeMember(SpecialMember.System_Array__LongLength)))
                    {
                        return new BoundArrayLength(syntax, rewrittenReceiverOpt, type);
                    }
                }
            }

            if (isLeftOfAssignment && propertySymbol.RefKind == RefKind.None)
            {
                // This is a property set access. We return a BoundPropertyAccess node here.
                // This node will be rewritten with MakePropertyAssignment when rewriting the enclosing BoundAssignmentOperator.

                return oldNodeOpt != null ?
                    oldNodeOpt.Update(rewrittenReceiverOpt, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, propertySymbol, useBackingField: AccessorKind.Unknown, resultKind, type) :
                    new BoundPropertyAccess(syntax, rewrittenReceiverOpt, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, propertySymbol, useBackingField: AccessorKind.Unknown, resultKind, type);
            }
            else
            {
                // This is a property get access
                return MakePropertyGetAccess(syntax, rewrittenReceiverOpt, propertySymbol, oldNodeOpt);
            }
        }

        private BoundExpression MakePropertyGetAccess(SyntaxNode syntax, BoundExpression? rewrittenReceiver, PropertySymbol property, BoundPropertyAccess? oldNodeOpt)
        {
            return MakePropertyGetAccess(syntax, rewrittenReceiver, property, ImmutableArray<BoundExpression>.Empty, default, null, oldNodeOpt);
        }

        private BoundExpression MakePropertyGetAccess(
            SyntaxNode syntax,
            BoundExpression? rewrittenReceiver,
            PropertySymbol property,
            ImmutableArray<BoundExpression> rewrittenArguments,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            MethodSymbol? getMethodOpt = null,
            BoundPropertyAccess? oldNodeOpt = null)
        {
            if (_inExpressionLambda && rewrittenArguments.IsEmpty)
            {
                Debug.Assert(argumentRefKindsOpt.IsDefaultOrEmpty);
                return oldNodeOpt != null ?
                    oldNodeOpt.Update(rewrittenReceiver, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, property, useBackingField: AccessorKind.Unknown, LookupResultKind.Viable, property.Type) :
                    new BoundPropertyAccess(syntax, rewrittenReceiver, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, property, useBackingField: AccessorKind.Unknown, LookupResultKind.Viable, property.Type);
            }
            else
            {
                var getMethod = getMethodOpt ?? property.GetOwnOrInheritedGetMethod();

                Debug.Assert(getMethod is { });
                Debug.Assert(getMethod.ParameterCount == rewrittenArguments.Length);
                Debug.Assert(getMethodOpt is null || ReferenceEquals(getMethod, getMethodOpt));

                return BoundCall.Synthesized(
                    syntax,
                    rewrittenReceiver,
                    initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                    getMethod,
                    rewrittenArguments,
                    argumentRefKindsOpt);
            }
        }
    }
}
