// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
