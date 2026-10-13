// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

/// <summary>
///  Records whether a legacy (<c>.cshtml</c>) document's <c>@inherits</c> base type has a callable
///  UTF-8 <c>WriteLiteral(ReadOnlySpan&lt;byte&gt;)</c> overload on the code-generation options, so
///  emission can choose byte literals. Runs after optimization and before C# lowering.
/// </summary>
/// <remarks>
///  Only legacy (<c>.cshtml</c>) documents are considered; components and other file kinds are left
///  alone. A legacy document with no <c>@inherits</c> directive has no base type to probe.
/// </remarks>
internal sealed class Utf8WriteLiteralPhase : RazorEnginePhaseBase
{
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var documentNode = codeDocument.GetDocumentNode();
        ThrowForMissingDocumentDependency(documentNode);

        if (codeDocument.GetUtf8SupportMap() is { } supportMap &&
            documentNode.Options is { } options &&
            codeDocument.FileKind.IsLegacy())
        {
            var baseTypeName = documentNode.FindPrimaryClass()?.BaseType?.BaseType.Content;
            var supported = !string.IsNullOrWhiteSpace(baseTypeName) &&
                supportMap.IsSupported(codeDocument.Source.FilePath, baseTypeName!);

            documentNode.Options = options.WithFlags(writeHtmlUtf8StringLiterals: supported);
        }

        return codeDocument.WithDocumentNode(documentNode);
    }
}
