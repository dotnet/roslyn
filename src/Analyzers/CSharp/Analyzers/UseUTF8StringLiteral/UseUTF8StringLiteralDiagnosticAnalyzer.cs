// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseUTF8StringLiteral
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class UseUTF8StringLiteralDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public enum ArrayCreationOperationLocation
        {
            Ancestors,
            Descendants,
            Current
        }

        public UseUTF8StringLiteralDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseUTF8StringLiteralDiagnosticId,
                EnforceOnBuildValues.UseUTF8StringLiteral,
                CSharpCodeStyleOptions.PreferUTF8StringLiterals,
                LanguageNames.CSharp,
                new LocalizableResourceString(nameof(CSharpAnalyzersResources.Convert_to_UTF8_string_literal), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_UTF8_string_literal), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(context =>
            {
                if (!context.Compilation.LanguageVersion().IsCSharp11OrAbove())
                    return;

                var expressionType = context.Compilation.GetTypeByMetadataName(typeof(System.Linq.Expressions.Expression<>).FullName!);

                context.RegisterOperationAction(c => AnalyzeOperation(c, expressionType), OperationKind.ArrayCreation);
            });

        private void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol? expressionType)
        {
            var arrayCreationOperation = (IArrayCreationOperation)context.Operation;

            // Don't offer if the user doesn't want it
            var option = context.GetOption(CSharpCodeStyleOptions.PreferUTF8StringLiterals);
            if (!option.Value)
                return;

            // Only replace arrays with initializers
            if (arrayCreationOperation.Initializer is null)
                return;

            // Must be a byte array
            if (arrayCreationOperation.Type is not IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
                return;

            // UTF8 strings are not valid to use in attributes
            if (arrayCreationOperation.Syntax.Ancestors().OfType<AttributeSyntax>().Any())
                return;

            // Can't use a UTF8 string inside an expression tree.
            var semanticModel = context.Operation.SemanticModel;
            Contract.ThrowIfNull(semanticModel);
            if (arrayCreationOperation.Syntax.IsInExpressionTree(semanticModel, expressionType, context.CancellationToken))
                return;

            var elements = arrayCreationOperation.Initializer.ElementValues;

            // If the compiler has constructed this array creation, then we don't want to do anything
            // if there aren't any elements, as we could just end up inserting ""u8 somewhere.
            if (arrayCreationOperation.IsImplicit && elements.Length == 0)
                return;

            if (!TryConvertToUTF8String(builder: null, elements))
                return;

            if (arrayCreationOperation.Syntax is ImplicitArrayCreationExpressionSyntax or ArrayCreationExpressionSyntax)
            {
                ReportArrayCreationDiagnostic(context, arrayCreationOperation.Syntax, option.Notification.Severity);
            }
            else if (elements.Length > 0 && elements[0].Syntax.Parent is ArgumentSyntax)
            {
                // For regular parameter arrays the code fix will need to search down
                ReportParameterArrayDiagnostic(context, arrayCreationOperation.Syntax, elements, option.Notification.Severity, ArrayCreationOperationLocation.Descendants);
            }
            else if (elements.Length > 0 && elements[0].Syntax.Parent.IsKind(SyntaxKind.CollectionInitializerExpression))
            {
                // For collection initializers where the Add method takes a parameter array, the code fix
                // will have to search up
                ReportParameterArrayDiagnostic(context, arrayCreationOperation.Syntax, elements, option.Notification.Severity, ArrayCreationOperationLocation.Ancestors);
            }

            // Otherwise this is an unsupported case
        }

        private void ReportParameterArrayDiagnostic(OperationAnalysisContext context, SyntaxNode syntaxNode, ImmutableArray<IOperation> elements, ReportDiagnostic severity, ArrayCreationOperationLocation operationLocation)
        {
            // When the first elements parent is as argument, or an edge case for collection
            // initializers where the Add method takes a param array, it means we have a parameter array.
            // We raise the diagnostic on all of the parameters that make up the array. We could do just
            // the first element, but that might be odd seeing: M(1, 2, [|3|], 4, 5)
            var span = TextSpan.FromBounds(elements[0].Syntax.SpanStart, elements[^1].Syntax.Span.End);
            var location = Location.Create(syntaxNode.SyntaxTree, span);

            ReportDiagnostic(context, syntaxNode, severity, location, operationLocation);
        }

        private void ReportArrayCreationDiagnostic(OperationAnalysisContext context, SyntaxNode syntaxNode, ReportDiagnostic severity)
        {
            // When the user writes the array creation we raise the diagnostic on the first token, which will be the "new" keyword
            var location = syntaxNode.GetFirstToken().GetLocation();

            ReportDiagnostic(context, syntaxNode, severity, location, ArrayCreationOperationLocation.Current);
        }

        private void ReportDiagnostic(OperationAnalysisContext context, SyntaxNode syntaxNode, ReportDiagnostic severity, Location location, ArrayCreationOperationLocation operationLocation)
        {
            // Store the original syntax location so the code fix can find the operation again
            var additionalLocations = ImmutableArray.Create(syntaxNode.GetLocation());

            // Also let the code fix where to look to find the operation that originally trigger this diagnostic
            var properties = ImmutableDictionary<string, string?>.Empty.Add(nameof(ArrayCreationOperationLocation), operationLocation.ToString());

            context.ReportDiagnostic(
                DiagnosticHelper.Create(Descriptor, location, severity, additionalLocations, properties));
        }

        internal static bool TryConvertToUTF8String(StringBuilder? builder, ImmutableArray<IOperation> arrayCreationElements)
        {
            // Since we'll only ever need to use up to 4 bytes to check/convert to UTF8
            // we can just use one array and reuse it. Using an array pool would do the same
            // thing but with more locks.
            var array = new byte[4];
            for (var i = 0; i < arrayCreationElements.Length;)
            {
                // We only need max 4 elements for a single Rune
                var count = Math.Min(arrayCreationElements.Length - i, 4);

                // Need to copy to a regular array to get a ROS for Rune to process
                for (var j = 0; j < count; j++)
                {
                    var element = arrayCreationElements[i + j];

                    // First basic check is that the array element is actually a byte
                    if (element.ConstantValue.Value is not byte)
                        return false;

                    array[j] = (byte)element.ConstantValue.Value;
                }

                var ros = new ReadOnlySpan<byte>(array, 0, count);

                // If we can't decode a rune from the array then it can't be represented as a string
                if (Rune.DecodeFromUtf8(ros, out var rune, out var bytesConsumed) != System.Buffers.OperationStatus.Done)
                    return false;

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
        }
    }
}
