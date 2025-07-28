// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editing;

public static class SyntaxEditorExtensions
{
    extension(SyntaxEditor editor)
    {
        public void SetAccessibility(SyntaxNode declaration, Accessibility accessibility)
        => editor.ReplaceNode(declaration, (d, g) => g.WithAccessibility(d, accessibility));

        public void SetModifiers(SyntaxNode declaration, DeclarationModifiers modifiers)
            => editor.ReplaceNode(declaration, (d, g) => g.WithModifiers(d, modifiers));

        internal void RemoveAllAttributes(SyntaxNode declaration)
            => editor.ReplaceNode(declaration, (d, g) => g.RemoveAllAttributes(d));

        internal void RemoveAllComments(SyntaxNode declaration)
            => editor.ReplaceNode(declaration, (d, g) => g.RemoveAllComments(d));

        internal void RemoveAllTypeInheritance(SyntaxNode declaration)
            => editor.ReplaceNode(declaration, (d, g) => g.RemoveAllTypeInheritance(d));

        public void SetName(SyntaxNode declaration, string name)
            => editor.ReplaceNode(declaration, (d, g) => g.WithName(d, name));

        public void SetType(SyntaxNode declaration, SyntaxNode type)
            => editor.ReplaceNode(declaration, (d, g) => g.WithType(d, type));

        public void SetTypeParameters(SyntaxNode declaration, IEnumerable<string> typeParameters)
            => editor.ReplaceNode(declaration, (d, g) => g.WithTypeParameters(d, typeParameters));

        public void SetTypeConstraint(SyntaxNode declaration, string typeParameterName, SpecialTypeConstraintKind kind, IEnumerable<SyntaxNode> types)
            => editor.ReplaceNode(declaration, (d, g) => g.WithTypeConstraint(d, typeParameterName, kind, types));

        public void SetExpression(SyntaxNode declaration, SyntaxNode expression)
            => editor.ReplaceNode(declaration, (d, g) => g.WithExpression(d, expression));

        public void SetStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
            => editor.ReplaceNode(declaration, (d, g) => g.WithStatements(d, statements));

        public void SetGetAccessorStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
            => editor.ReplaceNode(declaration, (d, g) => g.WithGetAccessorStatements(d, statements));

        public void SetSetAccessorStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
            => editor.ReplaceNode(declaration, (d, g) => g.WithSetAccessorStatements(d, statements));

        public void AddParameter(SyntaxNode declaration, SyntaxNode parameter)
            => editor.ReplaceNode(declaration, (d, g) => g.AddParameters(d, [parameter]));

        public void InsertParameter(SyntaxNode declaration, int index, SyntaxNode parameter)
            => editor.ReplaceNode(declaration, (d, g) => g.InsertParameters(d, index, [parameter]));

        public void AddAttribute(SyntaxNode declaration, SyntaxNode attribute)
            => editor.ReplaceNode(declaration, (d, g) => g.AddAttributes(d, [attribute]));

        public void AddReturnAttribute(SyntaxNode declaration, SyntaxNode attribute)
            => editor.ReplaceNode(declaration, (d, g) => g.AddReturnAttributes(d, [attribute]));

        public void AddAttributeArgument(SyntaxNode attributeDeclaration, SyntaxNode attributeArgument)
            => editor.ReplaceNode(attributeDeclaration, (d, g) => g.AddAttributeArguments(d, [attributeArgument]));

        public void AddMember(SyntaxNode declaration, SyntaxNode member)
            => editor.ReplaceNode(declaration, (d, g) => g.AddMembers(d, [member]));

        public void InsertMembers(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> members)
            => editor.ReplaceNode(declaration, (d, g) => g.InsertMembers(d, index, members));

        public void AddInterfaceType(SyntaxNode declaration, SyntaxNode interfaceType)
            => editor.ReplaceNode(declaration, (d, g) => g.AddInterfaceType(d, interfaceType));

        public void AddBaseType(SyntaxNode declaration, SyntaxNode baseType)
            => editor.ReplaceNode(declaration, (d, g) => g.AddBaseType(d, baseType));

        internal void RemovePrimaryConstructor(SyntaxNode declaration)
            => editor.ReplaceNode(declaration, (d, g) => g.RemovePrimaryConstructor(d));
    }
}
