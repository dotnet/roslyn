// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editing
{
    public static class SyntaxEditorExtensions
    {
        public static void SetAccessibility(this SyntaxEditor editor, SyntaxNode declaration, Accessibility accessibility)
            => editor.ReplaceNode(declaration, (d, g) => g.WithAccessibility(d, accessibility));

        public static void SetModifiers(this SyntaxEditor editor, SyntaxNode declaration, DeclarationModifiers modifiers)
            => editor.ReplaceNode(declaration, (d, g) => g.WithModifiers(d, modifiers));

        internal static void RemoveAllAttributes(this SyntaxEditor editor, SyntaxNode declaration)
            => editor.ReplaceNode(declaration, (d, g) => g.RemoveAllAttributes(d));

        internal static void RemoveAllComments(this SyntaxEditor editor, SyntaxNode declaration)
            => editor.ReplaceNode(declaration, (d, g) => g.RemoveAllComments(d));

        internal static void RemoveAllTypeInheritance(this SyntaxEditor editor, SyntaxNode declaration)
            => editor.ReplaceNode(declaration, (d, g) => g.RemoveAllTypeInheritance(d));

        public static void SetName(this SyntaxEditor editor, SyntaxNode declaration, string name)
            => editor.ReplaceNode(declaration, (d, g) => g.WithName(d, name));

        public static void SetType(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode type)
            => editor.ReplaceNode(declaration, (d, g) => g.WithType(d, type));

        public static void SetTypeParameters(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<string> typeParameters)
            => editor.ReplaceNode(declaration, (d, g) => g.WithTypeParameters(d, typeParameters));

        public static void SetTypeConstraint(this SyntaxEditor editor, SyntaxNode declaration, string typeParameterName, SpecialTypeConstraintKind kind, IEnumerable<SyntaxNode> types)
            => editor.ReplaceNode(declaration, (d, g) => g.WithTypeConstraint(d, typeParameterName, kind, types));

        public static void SetExpression(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode expression)
            => editor.ReplaceNode(declaration, (d, g) => g.WithExpression(d, expression));

        public static void SetStatements(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
            => editor.ReplaceNode(declaration, (d, g) => g.WithStatements(d, statements));

        public static void SetGetAccessorStatements(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
            => editor.ReplaceNode(declaration, (d, g) => g.WithGetAccessorStatements(d, statements));

        public static void SetSetAccessorStatements(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
            => editor.ReplaceNode(declaration, (d, g) => g.WithSetAccessorStatements(d, statements));

        public static void AddParameter(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode parameter)
            => editor.ReplaceNode(declaration, (d, g) => g.AddParameters(d, new[] { parameter }));

        public static void InsertParameter(this SyntaxEditor editor, SyntaxNode declaration, int index, SyntaxNode parameter)
            => editor.ReplaceNode(declaration, (d, g) => g.InsertParameters(d, index, new[] { parameter }));

        public static void AddAttribute(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode attribute)
            => editor.ReplaceNode(declaration, (d, g) => g.AddAttributes(d, new[] { attribute }));

        public static void AddReturnAttribute(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode attribute)
            => editor.ReplaceNode(declaration, (d, g) => g.AddReturnAttributes(d, new[] { attribute }));

        public static void AddAttributeArgument(this SyntaxEditor editor, SyntaxNode attributeDeclaration, SyntaxNode attributeArgument)
            => editor.ReplaceNode(attributeDeclaration, (d, g) => g.AddAttributeArguments(d, new[] { attributeArgument }));

        public static void AddMember(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode member)
            => editor.ReplaceNode(declaration, (d, g) => g.AddMembers(d, new[] { member }));

        public static void InsertMembers(this SyntaxEditor editor, SyntaxNode declaration, int index, IEnumerable<SyntaxNode> members)
            => editor.ReplaceNode(declaration, (d, g) => g.InsertMembers(d, index, members));

        public static void AddInterfaceType(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode interfaceType)
            => editor.ReplaceNode(declaration, (d, g) => g.AddInterfaceType(d, interfaceType));

        public static void AddBaseType(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode baseType)
            => editor.ReplaceNode(declaration, (d, g) => g.AddBaseType(d, baseType));
    }
}
