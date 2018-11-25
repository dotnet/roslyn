// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal interface IEmbeddedCodeFixProvider
    {
        string Title { get; }
        ImmutableArray<string> FixableDiagnosticIds { get; }

        void Fix(SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken);
    }
}
