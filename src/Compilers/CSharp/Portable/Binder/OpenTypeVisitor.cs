// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp;

internal partial class Binder
{
    /// <summary>
    /// This visitor walks over a type expression looking for open types.
    /// 
    /// Open types are allowed if an only if:
    /// <list type="number">
    /// <item>There is no constructed generic type elsewhere in the visited syntax; and</item>
    /// <item>The open type is not used as a type argument or array/pointer/nullable element type.</item>
    /// </list>
    /// 
    /// Open types can be used both in <c>typeof(...)</c> and <c>nameof(...)</c> expressions.
    /// </summary>
    protected sealed class OpenTypeVisitor : CSharpSyntaxVisitor
    {
        private Dictionary<GenericNameSyntax, bool>? _allowedMap;
        private bool _seenConstructed;

        /// <param name="typeSyntax">The argument to typeof.</param>
        /// <param name="allowedMap">
        /// Keys are GenericNameSyntax nodes representing unbound generic types.
        /// Values are false if the node should result in an error and true otherwise.
        /// </param>
        public static void Visit(ExpressionSyntax typeSyntax, out Dictionary<GenericNameSyntax, bool>? allowedMap)
        {
            OpenTypeVisitor visitor = new OpenTypeVisitor();
            visitor.Visit(typeSyntax);
            allowedMap = visitor._allowedMap;
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            SeparatedSyntaxList<TypeSyntax> typeArguments = node.TypeArgumentList.Arguments;
            if (node.IsUnboundGenericName)
            {
                _allowedMap ??= new Dictionary<GenericNameSyntax, bool>();
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
