// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

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

    internal class CSharpDesignerAttributeService : AbstractDesignerAttributeService<ClassDeclarationSyntax>
    {
        public CSharpDesignerAttributeService(Workspace workspace)
            : base(workspace)
        {
        }

        protected override ClassDeclarationSyntax GetFirstTopLevelClass(SyntaxNode root)
            => GetFirstTopLevelClass(((CompilationUnitSyntax)root).Members);

        private ClassDeclarationSyntax GetFirstTopLevelClass(SyntaxList<MemberDeclarationSyntax> members)
        {
            foreach (var member in members)
            {
                if (member is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    var classNode = GetFirstTopLevelClass(namespaceDeclaration.Members);
                    if (classNode != null)
                        return classNode;
                }
                else if (member is ClassDeclarationSyntax classDeclaration)
                {
                    return classDeclaration;
                }
            }

            return null;
        }

        protected override bool HasAttributesOrBaseTypeOrIsPartial(ClassDeclarationSyntax classNode)
        {
            return classNode.AttributeLists.Count > 0 ||
                classNode.BaseList != null ||
                classNode.Modifiers.Any(SyntaxKind.PartialKeyword);
        }
    }
}
