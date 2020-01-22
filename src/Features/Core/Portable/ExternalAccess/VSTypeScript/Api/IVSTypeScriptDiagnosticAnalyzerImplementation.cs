// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptDiagnosticAnalyzerImplementation
    {
        Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(Project project, CancellationToken cancellationToken);
        Task<ImmutableArray<Diagnostic>> AnalyzeDocumentSyntaxAsync(Document document, CancellationToken cancellationToken);
        Task<ImmutableArray<Diagnostic>> AnalyzeDocumentSemanticsAsync(Document document, CancellationToken cancellationToken);
    }
}
