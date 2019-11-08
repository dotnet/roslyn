// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder is for binding the argument to typeof.  It traverses
    /// the syntax marking each open type ("unbound generic type" in the
    /// C# spec) as either allowed or not allowed, so that BindType can 
    /// appropriately return either the corresponding type symbol or an 
    /// error type.  It also indicates whether the argument as a whole 
    /// should be considered open so that the flag can be set 
    /// appropriately in BoundTypeOfOperator.
    /// </summary>
    internal sealed class TypeofBinder : Binder
    {
        private readonly Dictionary<GenericNameSyntax, bool> _allowedMap;
        private readonly bool _isTypeExpressionOpen;

        internal TypeofBinder(ExpressionSyntax typeExpression, Binder next)
            // Unsafe types are not unsafe in typeof, so it is effectively an unsafe region.
            : base(next, next.Flags | BinderFlags.UnsafeRegion)
        {
            OpenTypeVisitor.Visit(typeExpression, out _allowedMap, out _isTypeExpressionOpen);
        }

        internal bool IsTypeExpressionOpen { get { return _isTypeExpressionOpen; } }

        protected override bool IsUnboundTypeAllowed(GenericNameSyntax syntax)
        {
            bool allowed;
            return _allowedMap != null && _allowedMap.TryGetValue(syntax, out allowed) && allowed;
        }

        /////// <summary>
        /////// Returns the list of the symbols which represent the argument of the nameof operator. Ambiguities are not an error for the nameof.
        /////// </summary>
        ////internal ImmutableArray<Symbol> LookupForNameofArgument(ExpressionSyntax left, IdentifierNameSyntax right, string name, DiagnosticBag diagnostics, bool isAliasQualified, out bool hasErrors)
        ////{
        ////    ArrayBuilder<Symbol> symbols = ArrayBuilder<Symbol>.GetInstance();
        ////    Symbol container = null;
        ////    hasErrors = false;

        ////    // We treat the AliasQualified syntax different than the rest. We bind the whole part for the alias.
        ////    if (isAliasQualified)
        ////    {
        ////        container = BindNamespaceAliasSymbol((IdentifierNameSyntax)left, diagnostics);
        ////        var aliasSymbol = container as AliasSymbol;
        ////        if (aliasSymbol != null) container = aliasSymbol.Target;
        ////        if (container.Kind == SymbolKind.NamedType)
        ////        {
        ////            diagnostics.Add(ErrorCode.ERR_ColColWithTypeAlias, left.Location, left);
        ////            hasErrors = true;
        ////            return symbols.ToImmutableAndFree();
        ////        }
        ////    }
        ////    // If it isn't AliasQualified, we first bind the left part, and then bind the right part as a simple name.
        ////    else if (left != null)
        ////    {
        ////        // We use OriginalDefinition because of the unbound generic names such as List<>, Dictionary<,>.
        ////        container = BindNamespaceOrTypeSymbol(left, diagnostics, null, false).OriginalDefinition;
        ////    }

        ////    this.BindNonGenericSimpleName(right, diagnostics, null, false, (NamespaceOrTypeSymbol)container, isNameofArgument: true, symbols: symbols);
        ////    if (CheckUsedBeforeDeclarationIfLocal(symbols, right))
        ////    {
        ////        Error(diagnostics, ErrorCode.ERR_VariableUsedBeforeDeclaration, right, right);
        ////        hasErrors = true;
        ////    }
        ////    else if (symbols.Count == 0)
        ////    {
        ////        hasErrors = true;
        ////    }
        ////    return symbols.ToImmutableAndFree();
        ////}

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
            private bool _seenGeneric;

            /// <param name="typeSyntax">The argument to typeof.</param>
            /// <param name="allowedMap">
            /// Keys are GenericNameSyntax nodes representing unbound generic types.
            /// Values are false if the node should result in an error and true otherwise.
            /// </param>
            /// <param name="isUnboundGenericType">True if no constructed generic type was encountered.</param>
            public static void Visit(ExpressionSyntax typeSyntax, out Dictionary<GenericNameSyntax, bool> allowedMap, out bool isUnboundGenericType)
            {
                OpenTypeVisitor visitor = new OpenTypeVisitor();
                visitor.Visit(typeSyntax);
                allowedMap = visitor._allowedMap;
                isUnboundGenericType = visitor is { _seenGeneric: true, _seenConstructed: false };
            }

            public override void VisitGenericName(GenericNameSyntax node)
            {
                _seenGeneric = true;

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

            public override void VisitQualifiedName(QualifiedNameSyntax node)
            {
                bool seenConstructedBeforeRight = _seenConstructed;

                // Visit Right first because it's smaller (to make backtracking cheaper).
                Visit(node.Right);

                bool seenConstructedBeforeLeft = _seenConstructed;

                Visit(node.Left);

                // If the first time we saw a constructed type was in Left, then we need to re-visit Right
                if (!seenConstructedBeforeRight && !seenConstructedBeforeLeft && _seenConstructed)
                {
                    Visit(node.Right);
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
}
