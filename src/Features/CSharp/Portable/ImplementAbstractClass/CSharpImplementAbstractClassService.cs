// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ImplementAbstractClass;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass
{
    [ExportLanguageService(typeof(IImplementAbstractClassService), LanguageNames.CSharp), Shared]
    internal class CSharpImplementAbstractClassService :
        AbstractImplementAbstractClassService<ClassDeclarationSyntax>
    {
        [ImportingConstructor]
        public CSharpImplementAbstractClassService()
        {
        }

        protected override bool TryInitializeState(
            Document document, SemanticModel model, ClassDeclarationSyntax classNode, CancellationToken cancellationToken,
            out INamedTypeSymbol classType, out INamedTypeSymbol abstractClassType)
        {
            classType = model.GetDeclaredSymbol(classNode);
            abstractClassType = classType?.BaseType;

            return classType != null && abstractClassType != null && abstractClassType.IsAbstractClass();
        }
    }
}
