// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

// The formatting engine updates Razor text between passes and must regenerate both Razor and C# outputs. This abstraction
// lets Remote Razor use RemoteDocumentSnapshot while dotnet format uses a normal Project/AdditionalDocument snapshot.
internal interface IDocumentSnapshot
{
    string FilePath { get; }

    RazorFileKind FileKind { get; }

    IDocumentSnapshot WithText(SourceText text);

    ValueTask<RazorCodeDocument> GetGeneratedOutputAsync(CancellationToken cancellationToken);

    ValueTask<SyntaxTree> GetCSharpSyntaxTreeAsync(bool declarationDocument, CancellationToken cancellationToken);
}
