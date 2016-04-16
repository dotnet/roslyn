// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ChangeSignature
{
    internal struct UnifiedArgumentSyntax : IUnifiedArgumentSyntax
    {
        private readonly SyntaxNode _argument;

        private UnifiedArgumentSyntax(SyntaxNode argument)
        {
            Debug.Assert(argument.IsKind(SyntaxKind.Argument) || argument.IsKind(SyntaxKind.AttributeArgument));
            _argument = argument;
        }

        public static IUnifiedArgumentSyntax Create(ArgumentSyntax argument)
        {
            return new UnifiedArgumentSyntax(argument);
        }

        public static IUnifiedArgumentSyntax Create(AttributeArgumentSyntax argument)
        {
            return new UnifiedArgumentSyntax(argument);
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
                ? Create(((ArgumentSyntax)_argument).WithNameColon((NameColonSyntax)nameColonSyntax))
                : Create(((AttributeArgumentSyntax)_argument).WithNameColon((NameColonSyntax)nameColonSyntax));
        }

        public string GetName()
        {
            return NameColon == null ? string.Empty : ((NameColonSyntax)NameColon).Name.Identifier.ToString();
        }

        public IUnifiedArgumentSyntax WithName(string name)
        {
            return _argument.IsKind(SyntaxKind.Argument)
                    ? Create(((ArgumentSyntax)_argument).WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(name))))
                    : Create(((AttributeArgumentSyntax)_argument).WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(name))));
        }

        public IUnifiedArgumentSyntax WithAdditionalAnnotations(SyntaxAnnotation annotation)
        {
            return new UnifiedArgumentSyntax(_argument.WithAdditionalAnnotations(annotation));
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

        public static explicit operator SyntaxNode(UnifiedArgumentSyntax unified)
        {
            return unified._argument;
        }
    }
}
