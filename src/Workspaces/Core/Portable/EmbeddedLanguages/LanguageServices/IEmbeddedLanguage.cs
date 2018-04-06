// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal interface IEmbeddedLanguage
    {
        IEmbeddedBraceMatcher BraceMatcher { get; }
        IEmbeddedClassifier Classifier { get; }
        IEmbeddedDiagnosticAnalyzer DiagnosticAnalyzer { get; }
    }
}
