// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal abstract class SnippetFunctionService : ILanguageService
{
    /// <summary>
    /// Language specific format for switch cases.
    /// </summary>
    public abstract string SwitchCaseFormat { get; }

    /// <summary>
    /// Language specific format for default switch case.
    /// </summary>
    public abstract string SwitchDefaultCaseForm { get; }

    /// <summary>
    /// Gets the name of the class that contains the specified position.
    /// </summary>
    public abstract Task<string?> GetContainingClassNameAsync(Document document, int position, CancellationToken cancellationToken);

    /// <summary>
    /// For a specified snippet field, replace it with the fully qualified name then simplify in the context of the document
    /// in order to retrieve the simplified type name.
    /// </summary>
    public static async Task<string?> GetSimplifiedTypeNameAsync(Document document, TextSpan fieldSpan, string fullyQualifiedTypeName, CancellationToken cancellationToken)
    {
        // Insert the function parameter (fully qualified type name) into the document.
        var updatedTextSpan = new TextSpan(fieldSpan.Start, fullyQualifiedTypeName.Length);

        var textChange = new TextChange(fieldSpan, fullyQualifiedTypeName);
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var documentWithFullyQualifiedTypeName = document.WithText(text.WithChanges(textChange));

        // Simplify
        var simplifiedTypeName = await GetSimplifiedTypeNameAtSpanAsync(documentWithFullyQualifiedTypeName, updatedTextSpan, cancellationToken).ConfigureAwait(false);
        return simplifiedTypeName;
    }

    /// <summary>
    /// For a document with the default switch snippet inserted, generate the expanded set of cases based on the value
    /// of the field currently inserted into the switch statement.
    /// </summary>
    public async Task<string?> GetSwitchExpansionAsync(Document document, TextSpan caseGenerationLocation, TextSpan switchExpressionLocation, CancellationToken cancellationToken)
    {
        var typeSymbol = await GetEnumSymbolAsync(document, switchExpressionLocation, cancellationToken).ConfigureAwait(false);
        if (typeSymbol?.TypeKind != TypeKind.Enum)
        {
            return null;
        }

        var enumFields = typeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Field && m.IsStatic);
        if (!enumFields.Any())
        {
            return null;
        }

        // Find and use the most simplified legal version of the enum type name in this context
        var fullyQualifiedEnumName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var simplifiedTypeName = await GetSimplifiedEnumNameAsync(document, fullyQualifiedEnumName, enumFields.First().Name, caseGenerationLocation, cancellationToken).ConfigureAwait(false);
        if (simplifiedTypeName == null)
        {
            return null;
        }

        using var _ = PooledStringBuilder.GetInstance(out var casesBuilder);
        foreach (var member in enumFields)
        {
            casesBuilder.AppendFormat(SwitchCaseFormat, simplifiedTypeName, member.Name);
        }

        casesBuilder.Append(SwitchDefaultCaseForm);
        return casesBuilder.ToString();
    }

    /// <summary>
    /// Parse the XML snippet function attribute to determine the function name and parameter.
    /// </summary>
    public static bool TryGetSnippetFunctionInfo(
            string? xmlFunctionText,
            [NotNullWhen(true)] out string? snippetFunctionName,
            [NotNullWhen(true)] out string? param)
    {
        if (string.IsNullOrEmpty(xmlFunctionText))
        {
            snippetFunctionName = null;
            param = null;
            return false;
        }

        if (!xmlFunctionText.Contains('(') ||
            !xmlFunctionText.Contains(')') ||
            xmlFunctionText.IndexOf(')') < xmlFunctionText.IndexOf('('))
        {
            snippetFunctionName = null;
            param = null;
            return false;
        }

        snippetFunctionName = xmlFunctionText[..xmlFunctionText.IndexOf('(')];

        var paramStart = xmlFunctionText.IndexOf('(') + 1;
        var paramLength = xmlFunctionText.LastIndexOf(')') - xmlFunctionText.IndexOf('(') - 1;
        param = xmlFunctionText.Substring(paramStart, paramLength);
        return true;
    }

    protected abstract Task<ITypeSymbol?> GetEnumSymbolAsync(Document document, TextSpan switchExpressionSpan, CancellationToken cancellationToken);

    protected abstract Task<(Document, TextSpan)> GetDocumentWithEnumCaseAsync(Document document, string fullyQualifiedTypeName, string firstEnumMemberName, TextSpan caseGenerationLocation, CancellationToken cancellationToken);

    private async Task<string?> GetSimplifiedEnumNameAsync(
        Document document,
        string fullyQualifiedTypeName,
        string firstEnumMemberName,
        TextSpan caseGenerationLocation,
        CancellationToken cancellationToken)
    {
        // Insert switch with enum case into the document.
        var (documentWithFullyQualified, fullyQualifiedTypeLocation) = await GetDocumentWithEnumCaseAsync(document, fullyQualifiedTypeName, firstEnumMemberName, caseGenerationLocation, cancellationToken).ConfigureAwait(false);

        // Simplify enum case.
        var simplifiedEnum = await GetSimplifiedTypeNameAtSpanAsync(documentWithFullyQualified, fullyQualifiedTypeLocation, cancellationToken).ConfigureAwait(false);
        return simplifiedEnum;
    }

    private static async Task<string?> GetSimplifiedTypeNameAtSpanAsync(Document documentWithFullyQualifiedTypeName, TextSpan fullyQualifiedTypeSpan, CancellationToken cancellationToken)
    {
        // Simplify
        var typeAnnotation = new SyntaxAnnotation();
        var syntaxRoot = await documentWithFullyQualifiedTypeName.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var nodeToReplace = syntaxRoot.DescendantNodes().FirstOrDefault(n => n.Span == fullyQualifiedTypeSpan);

        if (nodeToReplace == null)
        {
            return null;
        }

        var updatedRoot = syntaxRoot.ReplaceNode(nodeToReplace, nodeToReplace.WithAdditionalAnnotations(typeAnnotation, Simplifier.Annotation));
        var documentWithAnnotations = documentWithFullyQualifiedTypeName.WithSyntaxRoot(updatedRoot);

        var simplifiedDocument = await Simplifier.ReduceAsync(documentWithAnnotations, cancellationToken).ConfigureAwait(false);
        var simplifiedRoot = await simplifiedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var simplifiedTypeName = simplifiedRoot.GetAnnotatedNodesAndTokens(typeAnnotation).Single().ToString();
        return simplifiedTypeName;
    }
}
