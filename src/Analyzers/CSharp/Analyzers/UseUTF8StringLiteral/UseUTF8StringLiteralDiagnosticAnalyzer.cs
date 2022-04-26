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

            // Only raise the diagnostic on the first token (usually "new")
            var location = arrayCreationOperation.Syntax.GetFirstToken().GetLocation();

            // Store the original syntax location so the code fix can find the operation again
            var additionalLocations = ImmutableArray.Create(arrayCreationOperation.Syntax.GetLocation());

            // If this array creation is not an array creation expression, then it must be from
            // a parameter array, and the Syntax will be the entire invocation. To issue better
            // diagnostics construct a different location for this case.
            if (arrayCreationOperation.Syntax is not (ImplicitArrayCreationExpressionSyntax or ArrayCreationExpressionSyntax))
            {
                // Issue the diagnostic for all of the parameters that make up the array. We could do just
                // the first element, but that might be odd seeing: M(1, 2, [|3|], 4, 5)
                var span = TextSpan.FromBounds(elements.First().Syntax.SpanStart, elements.Last().Syntax.Span.End);
                location = Location.Create(location.SourceTree!, span);
            }

            context.ReportDiagnostic(
                DiagnosticHelper.Create(Descriptor, location, option.Notification.Severity, additionalLocations, properties: null));
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
