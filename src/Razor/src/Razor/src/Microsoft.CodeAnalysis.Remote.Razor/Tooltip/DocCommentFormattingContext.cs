// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote.Razor.Tooltip;

internal sealed class DocCommentFormattingContext(
    SemanticModel semanticModel,
    IDocumentationCommentFormattingService formattingService,
    int position)
{
    public static async Task<DocCommentFormattingContext> CreateAsync(RemoteDocumentSnapshot documentSnapshot, CancellationToken cancellationToken)
    {
        var generatedDocument = await documentSnapshot.GetGeneratedDocumentAsync(declarationDocument: false, cancellationToken).ConfigureAwait(false);
        return await CreateAsync(generatedDocument, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<DocCommentFormattingContext> CreateAsync(Document document, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var formattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
        // Razor puts imports inside the generated namespace, so position zero is outside their scope.
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var position = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.SpanStart ?? 0;
        return new DocCommentFormattingContext(semanticModel, formattingService, position);
    }

    public SemanticModel SemanticModel { get; } = semanticModel;
    public IDocumentationCommentFormattingService FormattingService { get; } = formattingService;

    public int Position { get; } = position;

    public ISymbol? ResolveSymbol(string typeName, string? propertyName)
    {
        var metadataName = typeName.StartsWith("global::", StringComparison.Ordinal)
            ? typeName["global::".Length..]
            : typeName;

        // Tag helper descriptors normally use metadata names. Normalize the C# generic spelling used by
        // some descriptors so Compilation can resolve those types as well.
        var genericStart = metadataName.IndexOf('<');
        if (genericStart >= 0 && metadataName.EndsWith(">", StringComparison.Ordinal))
        {
            var arity = 1;
            for (var i = genericStart + 1; i < metadataName.Length - 1; i++)
            {
                if (metadataName[i] == ',')
                {
                    arity++;
                }
            }

            metadataName = metadataName[..genericStart] + "`" + arity;
        }

        var type = SemanticModel.Compilation.GetTypeByMetadataName(metadataName);
        if (type is null || propertyName is null)
        {
            return type;
        }

        for (var currentType = type; currentType is not null; currentType = currentType.BaseType)
        {
            if (currentType.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault() is { } property)
            {
                return property;
            }
        }

        return type;
    }
}
