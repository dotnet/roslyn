// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveDeclarationNearReference;

namespace Microsoft.CodeAnalysis.CSharp.MoveDeclarationNearReference
{
    [ExportLanguageServiceFactory(typeof(IMoveDeclarationNearReferenceService), LanguageNames.CSharp), Shared]
    internal partial class CSharpMoveDeclarationNearReferenceLanguageServiceFactory : ILanguageServiceFactory
    {
        private readonly CSharpMoveDeclarationNearReferenceCodeRefactoringProvider _refactoringProvider;

        [ImportingConstructor]
        public CSharpMoveDeclarationNearReferenceLanguageServiceFactory(CSharpMoveDeclarationNearReferenceCodeRefactoringProvider refactoringProvider)
        {
            _refactoringProvider = refactoringProvider;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices) => _refactoringProvider;
    }
}
