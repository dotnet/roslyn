// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ChangeSignature
{
    internal struct UnifiedArgumentSyntax : IUnifiedArgumentSyntax
    {
        private readonly SyntaxNode _argument;

        private UnifiedArgumentSyntax(SyntaxNode argument, int index)
        {
            Debug.Assert(argument.IsKind(SyntaxKind.Argument) || argument.IsKind(SyntaxKind.AttributeArgument));
            _argument = argument;
            Index = index;
        }

        public static IUnifiedArgumentSyntax Create(ArgumentSyntax argument, int index)
        {
            return new UnifiedArgumentSyntax(argument, index);
        }

        public static IUnifiedArgumentSyntax Create(AttributeArgumentSyntax argument, int index)
        {
            return new UnifiedArgumentSyntax(argument, index);
        }

        public SyntaxNode NameColon
        {
            get
            {
                return _argument.IsKind(SyntaxKind.Argument)
                    ? ((ArgumentSyntax)_argument).NameColon
                    : ((AttributeArgumentSyntax)_argument).NameColon;
            }
        }

        public IUnifiedArgumentSyntax WithNameColon(SyntaxNode nameColonSyntax)
        {
            Debug.Assert(nameColonSyntax is NameColonSyntax);

            return _argument.IsKind(SyntaxKind.Argument)
                ? Create(((ArgumentSyntax)_argument).WithNameColon((NameColonSyntax)nameColonSyntax), Index)
                : Create(((AttributeArgumentSyntax)_argument).WithNameColon((NameColonSyntax)nameColonSyntax), Index);
        }

        public string GetName()
        {
            return NameColon == null ? string.Empty : ((NameColonSyntax)NameColon).Name.Identifier.ToString();
        }

        public IUnifiedArgumentSyntax WithName(string name)
        {
            return _argument.IsKind(SyntaxKind.Argument)
                    ? Create(((ArgumentSyntax)_argument).WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(name))), Index)
                    : Create(((AttributeArgumentSyntax)_argument).WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(name))), Index);
        }

        public IUnifiedArgumentSyntax WithAdditionalAnnotations(SyntaxAnnotation annotation)
        {
            return new UnifiedArgumentSyntax(_argument.WithAdditionalAnnotations(annotation), Index);
        }

        public SyntaxNode Expression
        {
            get
            {
                return _argument.IsKind(SyntaxKind.Argument)
                    ? ((ArgumentSyntax)_argument).Expression
                    : ((AttributeArgumentSyntax)_argument).Expression;
            }
        }

        public bool IsDefault
        {
            get
            {
                return _argument == null;
            }
        }

        public bool IsNamed
        {
            get
            {
                return NameColon != null;
            }
        }

        public int Index { get; }

        public static explicit operator SyntaxNode(UnifiedArgumentSyntax unified)
        {
            return unified._argument;
        }
    }
}
