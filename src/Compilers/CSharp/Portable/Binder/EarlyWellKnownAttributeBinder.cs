// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This is a special binder used for decoding some special well-known attributes very early in the attribute binding phase.
    /// It only binds those attribute argument syntax which can produce valid attribute arguments, but doesn't report any diagnostics.
    /// Subsequent binding phase will rebind such erroneous attributes and generate appropriate diagnostics.
    /// </summary>
    internal sealed class EarlyWellKnownAttributeBinder : Binder
    {
        internal EarlyWellKnownAttributeBinder(Binder enclosing)
            : base(enclosing, enclosing.Flags | BinderFlags.EarlyAttributeBinding)
        {
        }

        internal CSharpAttributeData GetAttribute(AttributeSyntax node, NamedTypeSymbol boundAttributeType, out bool generatedDiagnostics)
        {
            var dummyDiagnosticBag = DiagnosticBag.GetInstance();
            var boundAttribute = base.GetAttribute(node, boundAttributeType, dummyDiagnosticBag);
            generatedDiagnostics = !dummyDiagnosticBag.IsEmptyWithoutResolution;
            dummyDiagnosticBag.Free();
            return boundAttribute;
        }

        // Hide the GetAttribute overload which takes a diagnostic bag.
        // This ensures that diagnostics from the early bound attributes are never preserved.
        [Obsolete("EarlyWellKnownAttributeBinder has a better overload - GetAttribute(AttributeSyntax, NamedTypeSymbol, out bool)", true)]
        internal new CSharpAttributeData GetAttribute(AttributeSyntax node, NamedTypeSymbol boundAttributeType, DiagnosticBag diagnostics)
        {
            Debug.Assert(false, "Don't call this overload.");
            return base.GetAttribute(node, boundAttributeType, diagnostics);
        }

        /// <remarks>
        /// Since this method is expected to be called on every nested expression of the argument, it doesn't
        /// need to recurse (directly).
        /// </remarks>
        internal static bool CanBeValidAttributeArgument(ExpressionSyntax node, Binder typeBinder)
        {
            Debug.Assert(node != null);
            switch (node.Kind())
            {
                // ObjectCreationExpression for primitive types, such as "new int()", are treated as constants and allowed in attribute arguments.
                case SyntaxKind.ObjectCreationExpression:
                    {
                        var objectCreation = (ObjectCreationExpressionSyntax)node;
                        if (objectCreation.Initializer == null)
                        {
                            var unusedDiagnostics = DiagnosticBag.GetInstance();
                            var type = typeBinder.BindType(objectCreation.Type, unusedDiagnostics).Type;
                            unusedDiagnostics.Free();

                            var kind = TypedConstant.GetTypedConstantKind(type, typeBinder.Compilation);
                            switch (kind)
                            {
                                case TypedConstantKind.Primitive:
                                case TypedConstantKind.Enum:
                                    switch (type.TypeKind)
                                    {
                                        case TypeKind.Struct:
                                        case TypeKind.Class:
                                        case TypeKind.Enum:
                                            return true;
                                        default:
                                            return false;
                                    }
                            }
                        }

                        return false;
                    }

                // sizeof(int)
                case SyntaxKind.SizeOfExpression:

                // typeof(int)
                case SyntaxKind.TypeOfExpression:

                // constant expressions

                // SPEC:    Section 7.19: Only the following constructs are permitted in constant expressions:

                //  Literals (including the null literal).
                case SyntaxKind.NumericLiteralExpression:
                case SyntaxKind.StringLiteralExpression:
                case SyntaxKind.CharacterLiteralExpression:
                case SyntaxKind.TrueLiteralExpression:
                case SyntaxKind.FalseLiteralExpression:
                case SyntaxKind.NullLiteralExpression:

                //  References to const members of class and struct types.
                //  References to members of enumeration types.
                case SyntaxKind.IdentifierName:
                case SyntaxKind.GenericName:
                case SyntaxKind.AliasQualifiedName:
                case SyntaxKind.QualifiedName:
                case SyntaxKind.PredefinedType:
                case SyntaxKind.SimpleMemberAccessExpression:

                //  References to const parameters or local variables. Not valid for attribute arguments, so skipped here.

                //  Parenthesized sub-expressions, which are themselves constant expressions.
                case SyntaxKind.ParenthesizedExpression:

                //  Cast expressions, provided the target type is one of the types listed above.
                case SyntaxKind.CastExpression:

                //  checked and unchecked expressions
                case SyntaxKind.UncheckedExpression:
                case SyntaxKind.CheckedExpression:

                //  Default value expressions
                case SyntaxKind.DefaultExpression:

                //  The predefined +, –, !, and ~ unary operators.
                case SyntaxKind.UnaryPlusExpression:
                case SyntaxKind.UnaryMinusExpression:
                case SyntaxKind.LogicalNotExpression:
                case SyntaxKind.BitwiseNotExpression:

                //  The predefined +, –, *, /, %, <<, >>, &, |, ^, &&, ||, ==, !=, <, >, <=, and >= binary operators, provided each operand is of a type listed above.
                case SyntaxKind.AddExpression:
                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.SubtractExpression:
                case SyntaxKind.DivideExpression:
                case SyntaxKind.ModuloExpression:
                case SyntaxKind.LeftShiftExpression:
                case SyntaxKind.RightShiftExpression:
                case SyntaxKind.BitwiseAndExpression:
                case SyntaxKind.BitwiseOrExpression:
                case SyntaxKind.ExclusiveOrExpression:
                case SyntaxKind.LogicalAndExpression:
                case SyntaxKind.LogicalOrExpression:
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.InvocationExpression: //  To support nameof(); anything else will be a compile-time error
                case SyntaxKind.ConditionalExpression: //  The ?: conditional operator.
                    return true;

                default:
                    return false;
            }
        }
    }
}
