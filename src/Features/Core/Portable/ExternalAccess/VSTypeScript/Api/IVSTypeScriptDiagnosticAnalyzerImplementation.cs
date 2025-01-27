// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal interface IVSTypeScriptDiagnosticAnalyzerImplementation
{
    Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(Project project, CancellationToken cancellationToken);
    Task<ImmutableArray<Diagnostic>> AnalyzeDocumentSyntaxAsync(Document document, CancellationToken cancellationToken);
    Task<ImmutableArray<Diagnostic>> AnalyzeDocumentSemanticsAsync(Document document, CancellationToken cancellationToken);
}
