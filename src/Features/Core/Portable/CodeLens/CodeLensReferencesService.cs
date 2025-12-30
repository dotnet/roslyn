// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeLens;

internal sealed class CodeLensReferencesService : ICodeLensReferencesService
{
    private static readonly SymbolDisplayFormat MethodDisplayFormat =
        new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

    /// <summary>
    /// Set ourselves as an implicit invocation of FindReferences.  This will cause the finding operation to operate
    /// in serial, not parallel.  We're running ephemerally in the BG and do not want to saturate the system with
    /// work that then slows the user down.  Also, only process the inheritance hierarchy unidirectionally.  We want
    /// to find references that could actually call into a particular, not references to other members that could
    /// never actually call into this member.
    /// </summary>
    private static readonly FindReferencesSearchOptions s_nonParallelSearch =
        FindReferencesSearchOptions.Default with
        {
            Explicit = false,
            UnidirectionalHierarchyCascade = true
        };

    private static async Task<T?> FindAsync<T>(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
        Func<CodeLensFindReferencesProgress, Task<T>> onResults, Func<CodeLensFindReferencesProgress, Task<T>> onCapped,
        int searchCap, CancellationToken cancellationToken) where T : struct
    {
        var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
        if (document == null)
        {
            return null;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
        if (symbol == null)
        {
            return null;
        }

        using var progress = new CodeLensFindReferencesProgress(symbol, syntaxNode, searchCap, cancellationToken);
        try
        {
            await SymbolFinder.FindReferencesAsync(
                symbol, solution, progress, documents: null, s_nonParallelSearch, progress.CancellationToken).ConfigureAwait(false);

            return await onResults(progress).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (onCapped != null && progress.SearchCapReached)
            {
                // search was cancelled, and it was cancelled by us because a cap was reached.
                return await onCapped(progress).ConfigureAwait(false);
            }

            // search was cancelled, but not because of cap.
            // this always throws.
            throw;
        }
    }

    public async ValueTask<VersionStamp> GetProjectCodeLensVersionAsync(Solution solution, ProjectId projectId, CancellationToken cancellationToken)
    {
        return await solution.GetRequiredProject(projectId).GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReferenceCount?> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, int maxSearchResults, CancellationToken cancellationToken)
    {
        var projectVersion = await GetProjectCodeLensVersionAsync(solution, documentId.ProjectId, cancellationToken).ConfigureAwait(false);
        return await FindAsync(solution, documentId, syntaxNode,
            async progress => new ReferenceCount(
                progress.SearchCap > 0
                    ? Math.Min(progress.ReferencesCount, progress.SearchCap)
                    : progress.ReferencesCount, progress.SearchCapReached, projectVersion.ToString()),
            async progress => new ReferenceCount(progress.SearchCap, IsCapped: true, projectVersion.ToString()),
            maxSearchResults, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ReferenceLocationDescriptorAndDocument> GetDescriptorOfEnclosingSymbolAsync(Solution solution, Location location, CancellationToken cancellationToken)
    {
        var document = solution.GetDocument(location.SourceTree);

        if (document == null)
        {
            return null;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var langServices = document.GetLanguageService<ICodeLensDisplayInfoService>();
        if (langServices == null)
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unsupported language '{0}'", semanticModel.Language), nameof(semanticModel));
        }

        var position = location.SourceSpan.Start;
        var token = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).FindToken(position, true);
        var node = GetEnclosingCodeElementNode(document, token, langServices, cancellationToken);
        var longName = langServices.GetDisplayName(semanticModel, node);

        // get the full line of source text on the line that contains this position
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        // get the actual span of text for the line containing reference
        var textLine = text.Lines.GetLineFromPosition(position);

        // turn the span from document relative to line relative
        var spanStart = token.Span.Start - textLine.Span.Start;
        var line = textLine.ToString();

        var beforeLine1 = GetLineTextOrEmpty(text.Lines, textLine.LineNumber - 1);
        var beforeLine2 = GetLineTextOrEmpty(text.Lines, textLine.LineNumber - 2);
        var afterLine1 = GetLineTextOrEmpty(text.Lines, textLine.LineNumber + 1);
        var afterLine2 = GetLineTextOrEmpty(text.Lines, textLine.LineNumber + 2);
        var referenceSpan = new TextSpan(spanStart, token.Span.Length);

        var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
        var glyph = symbol?.GetGlyph();
        var startLinePosition = location.GetLineSpan().StartLinePosition;

        return new ReferenceLocationDescriptorAndDocument
        {
            Descriptor = new ReferenceLocationDescriptor(
                longName,
                semanticModel.Language,
                glyph,
                token.Span.Start,
                token.Span.Length,
                startLinePosition.Line,
                startLinePosition.Character,
                document.FilePath,
                line.TrimEnd(),
                referenceSpan.Start,
                referenceSpan.Length,
                beforeLine1.TrimEnd(),
                beforeLine2.TrimEnd(),
                afterLine1.TrimEnd(),
                afterLine2.TrimEnd()),
            DocumentId = document.Id
        };
    }

    private static SyntaxNode GetEnclosingCodeElementNode(Document document, SyntaxToken token, ICodeLensDisplayInfoService langServices, CancellationToken cancellationToken)
    {
        var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

        var node = token.Parent;
        while (node != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (syntaxFactsService.IsDocumentationComment(node))
            {
                var structuredTriviaSyntax = (IStructuredTriviaSyntax)node;
                var parentTrivia = structuredTriviaSyntax.ParentTrivia;
                node = parentTrivia.Token.Parent;
            }
            else if (syntaxFactsService.IsDeclaration(node) ||
                     syntaxFactsService.IsUsingOrExternOrImport(node) ||
                     syntaxFactsService.IsGlobalAssemblyAttribute(node))
            {
                break;
            }
            else
            {
                node = node.Parent;
            }
        }

        node ??= token.Parent;

        return langServices.GetDisplayNode(node);
    }

    public async Task<ImmutableArray<ReferenceLocationDescriptorAndDocument>?> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return await FindAsync(solution, documentId, syntaxNode,
            async progress =>
            {
                var referenceTasks = progress.Locations
                    .Select(location => GetDescriptorOfEnclosingSymbolAsync(solution, location, cancellationToken))
                    .ToArray();

                var result = await Task.WhenAll(referenceTasks).ConfigureAwait(false);

                return result.ToImmutableArray();
            }, onCapped: null, searchCap: 0, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<ReferenceLocationDescriptor>> MapReferenceLocationsAsync(Solution solution, ImmutableArray<ReferenceLocationDescriptorAndDocument> referenceLocations, ClassificationOptions classificationOptions, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ReferenceLocationDescriptor>.GetInstance(out var list);
        foreach (var descriptorAndDocument in referenceLocations)
        {
            var document = await solution.GetDocumentAsync(descriptorAndDocument.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            if (document == null)
            {
                continue;
            }

            var descriptor = descriptorAndDocument.Descriptor;
            var span = new TextSpan(descriptor.SpanStart, descriptor.SpanLength);

            if (!SpanMappingHelper.CanMapSpans(document))
            {
                // for normal document, just add one as they are
                list.Add(descriptor);
                continue;
            }

            var results = await SpanMappingHelper.TryGetMappedSpanResultAsync(document, [span], cancellationToken).ConfigureAwait(false);
            if (results is null)
            {
                // If the document can map spans, but this span wasn't mapped, drop the result
                continue;
            }

            var mappedSpans = results.GetValueOrDefault();

            // external component violated contracts. the mapper should preserve input order/count. 
            // since we gave in 1 span, it should return 1 span back
            Contract.ThrowIfTrue(mappedSpans.IsDefaultOrEmpty);

            var result = mappedSpans[0];
            if (result.IsDefault)
            {
                // it is allowed for mapper to return default 
                // if it can't map the given span to any usable span
                continue;
            }

            if (!DocumentExcerptHelper.CanExcerpt(document))
            {
                continue;
            }

            var referenceExcerpt = await DocumentExcerptHelper.TryExcerptAsync(document, span, ExcerptMode.SingleLine, classificationOptions, cancellationToken).ConfigureAwait(false);
            var tooltipExcerpt = await DocumentExcerptHelper.TryExcerptAsync(document, span, ExcerptMode.Tooltip, classificationOptions, cancellationToken).ConfigureAwait(false);

            var (text, start, length) = GetReferenceInfo(referenceExcerpt, descriptor);
            var (before1, before2, after1, after2) = GetReferenceTexts(referenceExcerpt, tooltipExcerpt, descriptor);

            list.Add(new ReferenceLocationDescriptor(
                descriptor.LongDescription,
                descriptor.Language,
                descriptor.Glyph,
                result.Span.Start,
                result.Span.Length,
                result.LinePositionSpan.Start.Line,
                result.LinePositionSpan.Start.Character,
                result.FilePath,
                text,
                start,
                length,
                before1,
                before2,
                after1,
                after2));
        }

        return list.ToImmutableAndClear();
    }

    private static (string text, int start, int length) GetReferenceInfo(ExcerptResult? reference, ReferenceLocationDescriptor descriptor)
    {
        if (reference.HasValue)
        {
            return (reference.Value.Content.ToString().TrimEnd(),
                    reference.Value.MappedSpan.Start,
                    reference.Value.MappedSpan.Length);
        }

        return (descriptor.ReferenceLineText, descriptor.ReferenceStart, descriptor.ReferenceLength);
    }

    private static (string before1, string before2, string after1, string after2) GetReferenceTexts(ExcerptResult? reference, ExcerptResult? tooltip, ReferenceLocationDescriptor descriptor)
    {
        if (reference == null || tooltip == null)
        {
            return (descriptor.BeforeReferenceText1, descriptor.BeforeReferenceText2, descriptor.AfterReferenceText1, descriptor.AfterReferenceText2);
        }

        var lines = tooltip.Value.Content.Lines;
        var mappedLine = lines.GetLineFromPosition(tooltip.Value.MappedSpan.Start);
        var index = mappedLine.LineNumber;
        if (index < 0)
        {
            return (descriptor.BeforeReferenceText1, descriptor.BeforeReferenceText2, descriptor.AfterReferenceText1, descriptor.AfterReferenceText2);
        }

        return (GetLineTextOrEmpty(lines, index - 1), GetLineTextOrEmpty(lines, index - 2),
                GetLineTextOrEmpty(lines, index + 1), GetLineTextOrEmpty(lines, index + 2));
    }

    private static string GetLineTextOrEmpty(TextLineCollection lines, int index)
    {
        if (index < 0 || index >= lines.Count)
        {
            return string.Empty;
        }

        return lines[index].ToString().TrimEnd();
    }

    private static ISymbol GetEnclosingMethod(SemanticModel semanticModel, Location location, CancellationToken cancellationToken)
    {
        var enclosingSymbol = semanticModel.GetEnclosingSymbol(location.SourceSpan.Start, cancellationToken);

        for (var current = enclosingSymbol; current != null; current = current.ContainingSymbol)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (current.Kind != SymbolKind.Method)
            {
                continue;
            }

            var method = (IMethodSymbol)current;
            if (method.IsAccessor())
            {
                return method.AssociatedSymbol;
            }

            if (method.MethodKind != MethodKind.AnonymousFunction)
            {
                return method;
            }
        }

        return null;
    }

    private static async Task<ReferenceMethodDescriptor> TryGetMethodDescriptorAsync(Location commonLocation, Solution solution, CancellationToken cancellationToken)
    {
        var document = solution.GetDocument(commonLocation.SourceTree);
        if (document == null)
        {
            return null;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var fullName = GetEnclosingMethod(semanticModel, commonLocation, cancellationToken)?.ToDisplayString(MethodDisplayFormat);

        return !string.IsNullOrEmpty(fullName) ? new ReferenceMethodDescriptor(fullName, document.FilePath, document.Project.OutputFilePath) : null;
    }

    public Task<ImmutableArray<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return FindAsync(solution, documentId, syntaxNode,
            async progress =>
            {
                var descriptorTasks =
                    progress.Locations
                    .Select(location => TryGetMethodDescriptorAsync(location, solution, cancellationToken))
                    .ToArray();

                var result = await Task.WhenAll(descriptorTasks).ConfigureAwait(false);

                return result.OfType<ReferenceMethodDescriptor>().ToImmutableArray();
            }, onCapped: null, searchCap: 0, cancellationToken: cancellationToken);
    }

    public async Task<string> GetFullyQualifiedNameAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
        CancellationToken cancellationToken)
    {
        var document = solution.GetDocument(syntaxNode.GetLocation().SourceTree);

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var declaredSymbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);

        if (declaredSymbol == null)
        {
            return string.Empty;
        }

        var parts = declaredSymbol.ToDisplayParts(MethodDisplayFormat);
        var pool = PooledStringBuilder.GetInstance();

        try
        {
            var actualBuilder = pool.Builder;
            var previousWasClass = false;

            for (var index = 0; index < parts.Length; index++)
            {
                var part = parts[index];
                if (previousWasClass &&
                    part.Kind == SymbolDisplayPartKind.Punctuation &&
                    index < parts.Length - 1)
                {
                    switch (parts[index + 1].Kind)
                    {
                        case SymbolDisplayPartKind.ClassName:
                        case SymbolDisplayPartKind.RecordClassName:
                        case SymbolDisplayPartKind.DelegateName:
                        case SymbolDisplayPartKind.EnumName:
                        case SymbolDisplayPartKind.ErrorTypeName:
                        case SymbolDisplayPartKind.InterfaceName:
                        case SymbolDisplayPartKind.StructName:
                        case SymbolDisplayPartKind.RecordStructName:
                            actualBuilder.Append('+');
                            break;

                        default:
                            actualBuilder.Append(part);
                            break;
                    }
                }
                else
                {
                    actualBuilder.Append(part);
                }

                previousWasClass = part.Kind is SymbolDisplayPartKind.ClassName or
                                   SymbolDisplayPartKind.RecordClassName or
                                   SymbolDisplayPartKind.InterfaceName or
                                   SymbolDisplayPartKind.StructName or
                                   SymbolDisplayPartKind.RecordStructName;
            }

            return actualBuilder.ToString();
        }
        finally
        {
            pool.Free();
        }
    }
}
