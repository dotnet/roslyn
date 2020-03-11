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

    internal class CSharpDesignerAttributeService : AbstractDesignerAttributeService<
        CompilationUnitSyntax,
        NamespaceDeclarationSyntax,
        ClassDeclarationSyntax>
    {
        public CSharpDesignerAttributeService(Workspace workspace)
            : base(workspace)
        {
        }

        protected override bool HasAttributesOrBaseTypeOrIsPartial(ClassDeclarationSyntax classNode)
        {
            return classNode.AttributeLists.Count > 0 ||
                classNode.BaseList != null ||
                classNode.Modifiers.Any(SyntaxKind.PartialKeyword);
        }
    }
}
