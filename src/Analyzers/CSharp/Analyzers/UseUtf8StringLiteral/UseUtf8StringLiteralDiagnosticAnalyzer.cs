// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseUtf8StringLiteral;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class UseUtf8StringLiteralDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public enum ArrayCreationOperationLocation
    {
        Ancestors,
        Descendants,
        Current
    }

    public UseUtf8StringLiteralDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseUtf8StringLiteralDiagnosticId,
            EnforceOnBuildValues.UseUtf8StringLiteral,
            CSharpCodeStyleOptions.PreferUtf8StringLiterals,
            new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_Utf8_string_literal), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
        {
            if (!context.Compilation.LanguageVersion().IsCSharp11OrAbove())
                return;

            if (context.Compilation.GetBestTypeByMetadataName(typeof(ReadOnlySpan<>).FullName!) is null)
                return;

            var expressionType = context.Compilation.GetTypeByMetadataName(typeof(System.Linq.Expressions.Expression<>).FullName!);

            context.RegisterOperationAction(c => AnalyzeOperation(c, expressionType), OperationKind.ArrayCreation);
        });

    private void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol? expressionType)
    {
        var arrayCreationOperation = (IArrayCreationOperation)context.Operation;

        // Don't offer if the user doesn't want it
        var option = context.GetCSharpAnalyzerOptions().PreferUtf8StringLiterals;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        // Only replace arrays with initializers
        if (arrayCreationOperation.Initializer is null)
            return;

        // Using UTF-8 string literals as nested array initializers is invalid
        if (arrayCreationOperation.DimensionSizes.Length > 1)
            return;

        // Must be a byte array
        if (arrayCreationOperation.Type is not IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
            return;

        // UTF-8 strings are not valid to use in attributes
        if (arrayCreationOperation.Syntax.Ancestors().OfType<AttributeSyntax>().Any())
            return;

        // Can't use a UTF-8 string inside an expression tree.
        var semanticModel = context.Operation.SemanticModel;
        Contract.ThrowIfNull(semanticModel);
        if (arrayCreationOperation.Syntax.IsInExpressionTree(semanticModel, expressionType, context.CancellationToken))
            return;

        var elements = arrayCreationOperation.Initializer.ElementValues;

        // If the compiler has constructed this array creation, then we don't want to do anything
        // if there aren't any elements, as we could just end up inserting ""u8 somewhere.
        if (arrayCreationOperation.IsImplicit && elements.Length == 0)
            return;

        if (!TryConvertToUtf8String(builder: null, elements))
            return;

        if (arrayCreationOperation.Syntax is ImplicitArrayCreationExpressionSyntax or ArrayCreationExpressionSyntax)
        {
            ReportArrayCreationDiagnostic(context, arrayCreationOperation.Syntax, option.Notification);
        }
        else if (elements is [{ Syntax.Parent: ArgumentSyntax }, ..])
        {
            // For regular parameter arrays the code fix will need to search down
            ReportParameterArrayDiagnostic(context, arrayCreationOperation.Syntax, elements, option.Notification, ArrayCreationOperationLocation.Descendants);
        }
        else if (elements is [{ Syntax.Parent: (kind: SyntaxKind.CollectionInitializerExpression) }, ..])
        {
            // For collection initializers where the Add method takes a parameter array, the code fix
            // will have to search up
            ReportParameterArrayDiagnostic(context, arrayCreationOperation.Syntax, elements, option.Notification, ArrayCreationOperationLocation.Ancestors);
        }
    }

    private void ReportParameterArrayDiagnostic(OperationAnalysisContext context, SyntaxNode syntaxNode, ImmutableArray<IOperation> elements, NotificationOption2 notificationOption, ArrayCreationOperationLocation operationLocation)
    {
        // When the first elements parent is as argument, or an edge case for collection
        // initializers where the Add method takes a param array, it means we have a parameter array.
        // We raise the diagnostic on all of the parameters that make up the array. We could do just
        // the first element, but that might be odd seeing: M(1, 2, [|3|], 4, 5)
        var span = TextSpan.FromBounds(elements[0].Syntax.SpanStart, elements[^1].Syntax.Span.End);
        var location = Location.Create(syntaxNode.SyntaxTree, span);

        ReportDiagnostic(context, syntaxNode, notificationOption, location, operationLocation);
    }

    private void ReportArrayCreationDiagnostic(OperationAnalysisContext context, SyntaxNode syntaxNode, NotificationOption2 notificationOption)
    {
        // When the user writes the array creation we raise the diagnostic on the first token, which will be the "new" keyword
        var location = syntaxNode.GetFirstToken().GetLocation();

        ReportDiagnostic(context, syntaxNode, notificationOption, location, ArrayCreationOperationLocation.Current);
    }

    private void ReportDiagnostic(OperationAnalysisContext context, SyntaxNode syntaxNode, NotificationOption2 notificationOption, Location location, ArrayCreationOperationLocation operationLocation)
    {
        // Store the original syntax location so the code fix can find the operation again
        var additionalLocations = ImmutableArray.Create(syntaxNode.GetLocation());

        // Also let the code fix where to look to find the operation that originally trigger this diagnostic
        var properties = ImmutableDictionary<string, string?>.Empty.Add(nameof(ArrayCreationOperationLocation), operationLocation.ToString());

        context.ReportDiagnostic(
            DiagnosticHelper.Create(Descriptor, location, notificationOption, context.Options, additionalLocations, properties));
    }

    internal static bool TryConvertToUtf8String(StringBuilder? builder, ImmutableArray<IOperation> arrayCreationElements)
    {
        for (var i = 0; i < arrayCreationElements.Length;)
        {
            // Need to call a method to do the actual rune decoding as it uses stackalloc, and stackalloc
            // in a loop is a bad idea. We also exclude any characters that are control or format chars
            if (!TryGetNextRune(arrayCreationElements, i, out var rune, out var bytesConsumed) ||
                IsControlOrFormatRune(rune))
            {
                return false;
            }

            i += bytesConsumed;

            if (builder is not null)
            {
                if (rune.TryGetEscapeCharacter(out var escapeChar))
                {
                    builder.Append('\\');
                    builder.Append(escapeChar);
                }
                else
                {
                    builder.Append(rune.ToString());
                }
            }
        }

        return true;

        // We allow the three control characters that users are familiar with and wouldn't be surprised to
        // see in a string literal
        static bool IsControlOrFormatRune(Rune rune)
            => Rune.GetUnicodeCategory(rune) is UnicodeCategory.Control or UnicodeCategory.Format
                && rune.Value switch
                {
                    '\r' => false,
                    '\n' => false,
                    '\t' => false,
                    _ => true
                };
    }

    private static bool TryGetNextRune(ImmutableArray<IOperation> arrayCreationElements, int startIndex, out Rune rune, out int bytesConsumed)
    {
        rune = default;
        bytesConsumed = 0;

        // We only need max 4 elements for a single Rune
        var length = Math.Min(arrayCreationElements.Length - startIndex, 4);

        Span<byte> array = stackalloc byte[length];
        for (var i = 0; i < length; i++)
        {
            var element = arrayCreationElements[startIndex + i];

            // First basic check is that the array element is actually a byte
            if (element.ConstantValue.Value is not byte b)
                return false;

            array[i] = b;
        }

        // If we can't decode a rune from the array then it can't be represented as a string
        return Rune.DecodeFromUtf8(array, out rune, out bytesConsumed) == System.Buffers.OperationStatus.Done;
    }
}
