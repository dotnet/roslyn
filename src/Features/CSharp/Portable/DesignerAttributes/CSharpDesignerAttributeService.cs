// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.DesignerAttributes
{
    [ExportLanguageServiceFactory(typeof(IDesignerAttributeService), LanguageNames.CSharp), Shared]
    internal class CSharpDesignerAttributeServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpDesignerAttributeServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new CSharpDesignerAttributeService(languageServices.WorkspaceServices.Workspace);
    }

    internal class CSharpDesignerAttributeService : AbstractDesignerAttributeService
    {
        public CSharpDesignerAttributeService(Workspace workspace) : base(workspace)
        {
        }

        protected override IEnumerable<SyntaxNode> GetAllTopLevelTypeDefined(SyntaxNode node)
        {
            if (!(node is CompilationUnitSyntax compilationUnit))
            {
                return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
            }

            return compilationUnit.Members.SelectMany(GetAllTopLevelTypeDefined);
        }

        private IEnumerable<SyntaxNode> GetAllTopLevelTypeDefined(MemberDeclarationSyntax member)
        {
            switch (member)
            {
                case NamespaceDeclarationSyntax namespaceMember:
                    return namespaceMember.Members.SelectMany(GetAllTopLevelTypeDefined);
                case ClassDeclarationSyntax type:
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
            if (typeNode is ClassDeclarationSyntax classNode)
            {
                return classNode.AttributeLists.Count > 0 ||
                    classNode.BaseList != null ||
                    classNode.Modifiers.Any(SyntaxKind.PartialKeyword);
            }

            return false;
        }
    }
}
