// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseUTF8StringLiteral
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class UseUTF8StringLiteralDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public const string StringValuePropertyName = "StringValue";

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

            var values = arrayCreationExpression.Initializer.ElementValues.SelectAsArray(v => v.ConstantValue.HasValue && v.ConstantValue.Value is not null, v => v.ConstantValue.Value);

            // If we couldn't get constant values for all elements then we can't offer
            if (values.Length != arrayCreationExpression.Initializer.ElementValues.Length)
                return;

            var stringValue = GetStringValue(values);

            if (stringValue is null)
                return;

            var properties = ImmutableDictionary<string, string?>.Empty.Add(StringValuePropertyName, stringValue);

            context.ReportDiagnostic(
                DiagnosticHelper.Create(Descriptor, arrayCreationExpression.Syntax.GetFirstToken().GetLocation(), option.Notification.Severity, additionalLocations: null, properties));
        }

        private static string? GetStringValue(ImmutableArray<object?> values)
        {
            try
            {
                var byteValues = new byte[values.Length];
                for (var i = 0; i < values.Length; i++)
                {
                    // We shouldn't get nulls, but Convert.ToByte will return (byte)0 if there are any
                    // so better to crash if the analyzer has a bug, that output a buggy string in users code.
                    if (values[i] is null)
                        throw ExceptionUtilities.Unreachable;

                    byteValues[i] = Convert.ToByte(values[i]);
                }

                var pooledBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledBuilder.Builder;
                var ros = new ReadOnlySpan<byte>(byteValues);
                for (var i = 0; i < ros.Length;)
                {
                    // If we can't decode a rune from the array then it can't be represented as a string
                    if (Rune.DecodeFromUtf8(ros.Slice(i), out var rune, out var bytesConsumed) != OperationStatus.Done)
                        return null;

                    i += bytesConsumed;
                    builder.Append(GetStringLiteralRepresentation(rune));
                }

                return pooledBuilder.ToStringAndFree();
            }
            catch
            {
                // Ignore any conversion failures and just don't report the diagnostic
            }

            return null;
        }

        private static string GetStringLiteralRepresentation(Rune rune)
            => rune.Value switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\0' => "\\0",
                '\a' => "\\a",
                '\b' => "\\b",
                '\f' => "\\f",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\v' => "\\v",
                _ => rune.ToString()
            };
    }
}
