// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    public static class SyntaxEditorExtensions
    {
        public static void SetAccessibility(this SyntaxEditor editor, SyntaxNode declaration, Accessibility accessibility)
        {
            editor.ReplaceNode(declaration, (d, g) => g.WithAccessibility(d, accessibility));
        }

        public static void SetModifiers(this SyntaxEditor editor, SyntaxNode declaration, DeclarationModifiers modifiers)
        {
            editor.ReplaceNode(declaration, (d, g) => g.WithModifiers(d, modifiers));
        }

        public static void SetName(this SyntaxEditor editor, SyntaxNode declaration, string name)
        {
            editor.ReplaceNode(declaration, (d, g) => g.WithName(d, name));
        }

        public static void SetType(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode type)
        {
            editor.ReplaceNode(declaration, (d, g) => g.WithType(d, type));
        }

        public static void SetTypeParameters(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<string> typeParameters)
        {
            editor.ReplaceNode(declaration, (d, g) => g.WithTypeParameters(d, typeParameters));
        }

        public static void SetTypeConstraint(this SyntaxEditor editor, SyntaxNode declaration, string typeParameterName, SpecialTypeConstraintKind kind, IEnumerable<SyntaxNode> types)
        {
            editor.ReplaceNode(declaration, (d, g) => g.WithTypeConstraint(d, typeParameterName, kind, types));
        }

        public static void SetExpression(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode expression)
        {
            editor.ReplaceNode(declaration, (d, g) => g.WithExpression(d, expression));
        }

        public static void SetStatements(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<SyntaxNode> statements)
        {
            editor.ReplaceNode(declaration, (d, g) => g.WithStatements(d, statements));
        }

        public static void InsertParameters(this SyntaxEditor editor, SyntaxNode declaration, int index, IEnumerable<SyntaxNode> parameters)
        {
            editor.ReplaceNode(declaration, (d, g) => g.InsertParameters(d, index, parameters));
        }

        public static void AddParameters(this SyntaxEditor editor, SyntaxNode declaration, int index, IEnumerable<SyntaxNode> parameters)
        {
            editor.ReplaceNode(declaration, (d, g) => g.AddParameters(d, parameters));
        }

        public static void InsertAttributes(this SyntaxEditor editor, SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes)
        {
            editor.ReplaceNode(declaration, (d, g) => g.InsertAttributes(d, index, attributes));
        }

        public static void AddAttributes(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<SyntaxNode> attributes)
        {
            editor.ReplaceNode(declaration, (d, g) => g.AddAttributes(d, attributes));
        }

        public static void InsertReturnAttributes(this SyntaxEditor editor, SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes)
        {
            editor.ReplaceNode(declaration, (d, g) => g.InsertReturnAttributes(d, index, attributes));
        }

        public static void AddReturnAttributes(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<SyntaxNode> attributes)
        {
            editor.ReplaceNode(declaration, (d, g) => g.AddAttributes(d, attributes));
        }

        public static void InsertMembers(this SyntaxEditor editor, SyntaxNode declaration, int index, IEnumerable<SyntaxNode> members)
        {
            editor.ReplaceNode(declaration, (d, g) => g.InsertMembers(d, index, members));
        }

        public static void AddMembers(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<SyntaxNode> members)
        {
            editor.ReplaceNode(declaration, (d, g) => g.AddMembers(d, members));
        }
    }
}
