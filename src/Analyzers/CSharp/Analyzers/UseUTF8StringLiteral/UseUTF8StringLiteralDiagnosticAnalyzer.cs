// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
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
                CSharpCodeStyleOptions.PreferUTF8StringLiteral,
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
                if (context.Compilation.LanguageVersion() < LanguageVersion.Preview)
                    return;

                var expressionType = context.Compilation.GetTypeByMetadataName(typeof(System.Linq.Expressions.Expression<>).FullName!);

                context.RegisterOperationAction(c => AnalyzeOperation(c, expressionType), OperationKind.ArrayCreation);
            });

        private void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol? expressionType)
        {
            var arrayCreationExpression = (IArrayCreationOperation)context.Operation;

            // Don't offer if the user doesn't want it
            var option = context.GetOption(CSharpCodeStyleOptions.PreferUTF8StringLiteral);
            if (!option.Value)
                return;

            // Only replace arrays with initializers
            if (arrayCreationExpression.Initializer is null)
                return;

            // Must be a byte array
            if (arrayCreationExpression.Type is not IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
                return;

            // UTF8 strings are not valid to use in attributes
            if (arrayCreationExpression.Syntax.Ancestors().OfType<AttributeSyntax>().Any())
                return;

            // Can't use a UTF8 string inside an expression tree.
            var semanticModel = context.Operation.SemanticModel;
            Contract.ThrowIfNull(semanticModel);
            if (arrayCreationExpression.Syntax.IsInExpressionTree(semanticModel, expressionType, context.CancellationToken))
                return;

            var elements = arrayCreationExpression.Initializer.ElementValues;

            // If the compiler has constructed this array creation, then we don't want to do anything
            // if there aren't any elements, as we could just end up inserting ""u8 somewhere.
            if (arrayCreationExpression.IsImplicit && elements.Length == 0)
                return;

            // We need to ensure that each element is a byte, and that they are representable as a string
            // but to avoid LOH allocations from large user data, we use a SegmentedList here, and
            // only create a small array when necessary later.
            var values = new SegmentedList<byte>(elements.Where(v => v.ConstantValue.Value is byte).Select(v => (byte)v.ConstantValue.Value!));

            // If we couldn't get constant values for all elements then we can't offer
            if (values.Count != elements.Length)
                return;

            if (!TryConvertToUTF8String(builder: null, values))
                return;

            // Only raise the diagnostic on the first token (usually "new")
            var location = arrayCreationExpression.Syntax.GetFirstToken().GetLocation();

            // Store the original syntax location so the code fix can find the operation again
            var additionalLocations = ImmutableArray.Create(arrayCreationExpression.Syntax.GetLocation());

            // If this array creation is not an array creation expression, then it must be from
            // a parameter array, and the Syntax will be the entire invocation. To issue better
            // diagnostics construct a different location for this case.
            if (arrayCreationExpression.Syntax is not (ImplicitArrayCreationExpressionSyntax or ArrayCreationExpressionSyntax))
            {
                // Issue the diagnostic for all of the parameters that make up the array. We could do just
                // the first element, but that might be odd seeing: M(1, 2, [|3|], 4, 5)
                var span = TextSpan.FromBounds(elements.First().Syntax.SpanStart, elements.Last().Syntax.Span.End);
                location = Location.Create(location.SourceTree!, span);
            }

            context.ReportDiagnostic(
                DiagnosticHelper.Create(Descriptor, location, option.Notification.Severity, additionalLocations, properties: null));
        }

        internal static bool TryConvertToUTF8String(StringBuilder? builder, SegmentedList<byte> values)
        {
            for (var i = 0; i < values.Count;)
            {
                var ros = GetBytesForNextRune(values, i);
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

            static ReadOnlySpan<byte> GetBytesForNextRune(SegmentedList<byte> values, int index)
            {
                // We only need max 4 elements for a single Rune
                var count = Math.Min(values.Count - index, 4);

                // Need to copy to a regular array to get a ROS for Rune to process
                var array = ArrayPool<byte>.GetArray(count);
                values.CopyTo(index, array, 0, count);

                return new ReadOnlySpan<byte>(array);
            }
        }
    }
}
