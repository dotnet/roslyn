// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

internal sealed class Utf8WriteLiteralDetectionPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    private IUtf8WriteLiteralFeature? _utf8Feature;

    protected override void OnInitialized()
    {
        Engine.TryGetFeature(out _utf8Feature);
    }

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (_utf8Feature is null ||
            !codeDocument.FileKind.IsLegacy() ||
            documentNode.Options is null ||
            documentNode.Options.DesignTime ||
            documentNode.Options.WriteHtmlUtf8StringLiterals)
        {
            return;
        }

        var @class = documentNode.FindPrimaryClass();
        var baseType = @class?.BaseType;
        if (baseType is null || string.IsNullOrWhiteSpace(baseType.BaseType.Content))
        {
            // No explicit @inherits directive. The default Razor base classes don't currently
            // support WriteLiteral(ReadOnlySpan<byte>). When they do, this check should be
            // expanded to also probe the default base type.
            return;
        }

        var baseTypeName = baseType.BaseType.Content;
        if (_utf8Feature.IsSupported(codeDocument.Source.FilePath, baseTypeName))
        {
            documentNode.Options = documentNode.Options.WithFlags(writeHtmlUtf8StringLiterals: true);
        }
    }
}
