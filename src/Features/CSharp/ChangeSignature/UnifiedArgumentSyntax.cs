// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ChangeSignature
{
    internal struct UnifiedArgumentSyntax : IUnifiedArgumentSyntax
    {
        private readonly SyntaxNode argument;

        private UnifiedArgumentSyntax(SyntaxNode argument)
        {
            Debug.Assert(argument.IsKind(SyntaxKind.Argument) || argument.IsKind(SyntaxKind.AttributeArgument));
            this.argument = argument;
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
                return this.argument.IsKind(SyntaxKind.Argument)
                    ? ((ArgumentSyntax)this.argument).NameColon
                    : ((AttributeArgumentSyntax)this.argument).NameColon;
            }
        }

        public IUnifiedArgumentSyntax WithNameColon(SyntaxNode nameColonSyntax)
        {
            Debug.Assert(nameColonSyntax is NameColonSyntax);

            return this.argument.IsKind(SyntaxKind.Argument)
                ? Create(((ArgumentSyntax)this.argument).WithNameColon((NameColonSyntax)nameColonSyntax))
                : Create(((AttributeArgumentSyntax)this.argument).WithNameColon((NameColonSyntax)nameColonSyntax));
        }

        public string GetName()
        {
            return NameColon == null ? string.Empty : ((NameColonSyntax)NameColon).Name.Identifier.ToString();
        }

        public IUnifiedArgumentSyntax WithName(string name)
        {
            return this.argument.IsKind(SyntaxKind.Argument)
                    ? Create(((ArgumentSyntax)this.argument).WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(name))))
                    : Create(((AttributeArgumentSyntax)this.argument).WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(name))));
        }

        public IUnifiedArgumentSyntax WithAdditionalAnnotations(SyntaxAnnotation annotation)
        {
            return new UnifiedArgumentSyntax(argument.WithAdditionalAnnotations(annotation));
        }

        public SyntaxNode Expression
        {
            get
            {
                return this.argument.IsKind(SyntaxKind.Argument)
                    ? ((ArgumentSyntax)this.argument).Expression
                    : ((AttributeArgumentSyntax)this.argument).Expression;
            }
        }

        public bool IsDefault
        {
            get
            {
                return this.argument == null;
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
            return unified.argument;
        }
    }
}
