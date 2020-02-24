// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpCodeRefactoringTest<TCodeRefactoring, TVerifier> : CodeRefactoringTest<TVerifier>
        where TCodeRefactoring : CodeRefactoringProvider, new()
        where TVerifier : IVerifier, new()
    {
        private static readonly LanguageVersion DefaultLanguageVersion =
            Enum.TryParse("Default", out LanguageVersion version) ? version : LanguageVersion.CSharp6;

        protected override IEnumerable<CodeRefactoringProvider> GetCodeRefactoringProviders()
            => new[] { new TCodeRefactoring() };

        protected override string DefaultFileExt => "cs";

        public override string Language => LanguageNames.CSharp;

        public override Type SyntaxKindType => typeof(SyntaxKind);

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

        protected override ParseOptions CreateParseOptions()
            => new CSharpParseOptions(DefaultLanguageVersion, DocumentationMode.Diagnose);
    }
}
