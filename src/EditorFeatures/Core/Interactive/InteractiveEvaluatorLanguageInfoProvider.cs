// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal abstract class InteractiveEvaluatorLanguageInfoProvider
    {
        public abstract string LanguageName { get; }
        public abstract CompilationOptions GetSubmissionCompilationOptions(string name, MetadataReferenceResolver metadataReferenceResolver, SourceReferenceResolver sourceReferenceResolver, ImmutableArray<string> imports);
        public abstract ParseOptions ParseOptions { get; }
        public abstract CommandLineParser CommandLineParser { get; }
        public abstract bool IsCompleteSubmission(string text);
        public abstract string InteractiveResponseFileName { get; }
        public abstract Type ReplServiceProviderType { get; }
        public abstract string Extension { get; }
    }
}
