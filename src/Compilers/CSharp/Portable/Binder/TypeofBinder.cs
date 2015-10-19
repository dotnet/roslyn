// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Base type for nameof and typeof expression arguments.  Properly handles binding open types
    /// in the argument.
    /// </summary>
    internal abstract class TypeofOrNameofBinder : Binder
    {
        private readonly Dictionary<GenericNameSyntax, bool> _allowedMap;

        protected TypeofOrNameofBinder(ExpressionSyntax typeExpression, Binder next, BinderFlags flags)
            : base(next, flags)
        {
            _allowedMap = OpenTypeVisitor.Create(typeExpression);
        }

        protected override bool IsUnboundTypeAllowed(GenericNameSyntax syntax)
        {
            bool allowed;
            return _allowedMap != null && _allowedMap.TryGetValue(syntax, out allowed) && allowed;
        }

        /// <summary>
        /// This visitor walks over a type expression looking for open types.
        /// Open types are allowed if an only if:
        ///   1) There is no constructed generic type elsewhere in the visited syntax; and
        ///   2) The open type is not used as a type argument or array/pointer/nullable
        ///        element type.
        /// </summary>
        private class OpenTypeVisitor : CSharpSyntaxVisitor
        {
            private Dictionary<GenericNameSyntax, bool> _allowedMap;
            private bool _seenConstructed;

            /// <param name="typeSyntax">The argument to typeof.</param>
            /// <return>
            /// Keys are GenericNameSyntax nodes representing unbound generic types.
            /// Values are false if the node should result in an error and true otherwise.
            /// </return>
            public static Dictionary<GenericNameSyntax, bool> Create(ExpressionSyntax typeSyntax)
            {
                var visitor = new OpenTypeVisitor();
                visitor.Visit(typeSyntax);
                return visitor._allowedMap;
            }

            public override void VisitGenericName(GenericNameSyntax node)
            {
                SeparatedSyntaxList<TypeSyntax> typeArguments = node.TypeArgumentList.Arguments;
                if (node.IsUnboundGenericName)
                {
                    if (_allowedMap == null)
                    {
                        _allowedMap = new Dictionary<GenericNameSyntax, bool>();
                    }
                    _allowedMap[node] = !_seenConstructed;
                }
                else
                {
                    _seenConstructed = true;
                    foreach (TypeSyntax arg in typeArguments)
                    {
                        Visit(arg);
                    }
                }
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                {
                    VisitDottedExpression(node.Expression, node.Name);
                    return;
                }

                base.VisitMemberAccessExpression(node);
            }

            public override void VisitQualifiedName(QualifiedNameSyntax node)
            {
                VisitDottedExpression(node.Left, node.Right);
            }

            private void VisitDottedExpression(ExpressionSyntax left, SimpleNameSyntax right)
            {
                bool seenConstructedBeforeRight = _seenConstructed;

                // Visit Right first because it's smaller (to make backtracking cheaper).
                Visit(right);

                bool seenConstructedBeforeLeft = _seenConstructed;

                Visit(left);

                // If the first time we saw a constructed type was in Left, then we need to re-visit Right
                if (!seenConstructedBeforeRight && !seenConstructedBeforeLeft && _seenConstructed)
                {
                    Visit(right);
                }
            }

            public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            {
                Visit(node.Name);
            }

            public override void VisitArrayType(ArrayTypeSyntax node)
            {
                _seenConstructed = true;
                Visit(node.ElementType);
            }

            public override void VisitPointerType(PointerTypeSyntax node)
            {
                _seenConstructed = true;
                Visit(node.ElementType);
            }

            public override void VisitNullableType(NullableTypeSyntax node)
            {
                _seenConstructed = true;
                Visit(node.ElementType);
            }
        }
    }

    /// <summary>
    /// This binder is for binding the argument to typeof.  It traverses
    /// the syntax marking each open type ("unbound generic type" in the
    /// C# spec) as either allowed or not allowed, so that BindType can 
    /// appropriately return either the corresponding type symbol or an 
    /// error type.  It also indicates whether the argument as a whole 
    /// should be considered open so that the flag can be set 
    /// appropriately in BoundTypeOfOperator.
    /// </summary>
    internal sealed class TypeofBinder : TypeofOrNameofBinder
    {
        internal TypeofBinder(ExpressionSyntax typeExpression, Binder next)
            // Unsafe types are not unsafe in typeof, so it is effectively an unsafe region.
            // Since we only depend on existence of nameable members and nameof(x) produces a constant
            // string expression usable in an early attribute, we use early attribute binding.
            : base(typeExpression, next, next.Flags | BinderFlags.UnsafeRegion | BinderFlags.EarlyAttributeBinding)
        {
        }
    }
}
