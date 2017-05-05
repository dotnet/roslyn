// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.DesignerAttributes
{
    [ExportLanguageService(typeof(IDesignerAttributeService), LanguageNames.CSharp), Shared]
    internal class CSharpDesignerAttributeService : AbstractDesignerAttributeService
    {
        protected override IEnumerable<SyntaxNode> GetAllTopLevelTypeDefined(SyntaxNode node)
        {
            var compilationUnit = node as CompilationUnitSyntax;
            if (compilationUnit == null)
            {
                return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
            }

            return compilationUnit.Members.SelectMany(GetAllTopLevelTypeDefined);
        }

        private IEnumerable<SyntaxNode> GetAllTopLevelTypeDefined(MemberDeclarationSyntax member)
        {
            var namespaceMember = member as NamespaceDeclarationSyntax;
            if (namespaceMember != null)
            {
                return namespaceMember.Members.SelectMany(GetAllTopLevelTypeDefined);
            }

            var type = member as ClassDeclarationSyntax;
            if (type != null)
            {
                return SpecializedCollections.SingletonEnumerable<SyntaxNode>(type);
            }

            return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        protected override bool ProcessOnlyFirstTypeDefined()
        {
            return true;
        }

        protected override bool HasAttributesOrBaseTypeOrIsPartial(SyntaxNode typeNode)
        {
            var classNode = typeNode as ClassDeclarationSyntax;
            if (classNode != null)
            {
                return classNode.AttributeLists.Count > 0 ||
                    classNode.BaseList != null ||
                    classNode.Modifiers.Any(SyntaxKind.PartialKeyword);
            }

            return false;
        }
    }
}
