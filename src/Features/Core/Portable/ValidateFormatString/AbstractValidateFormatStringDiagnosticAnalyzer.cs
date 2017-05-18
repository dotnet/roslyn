// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.ValidateFormatString
{
    internal abstract class AbstractValidateFormatStringDiagnosticAnalyzer<TSyntaxKind> :
        DiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        private static Regex s_removeEscapedBracketsRegex = new Regex("{{");
        private static Regex s_extractPlaceholdersRegex = new Regex("{(.*?)}");

        private const string DiagnosticId = IDEDiagnosticIds.ValidateFormatStringDiagnosticID;
        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(FeaturesResources.Invalid_format_string),
            FeaturesResources.ResourceManager,
            typeof(FeaturesResources));

        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
            nameof(FeaturesResources.Format_string_0_contains_invalid_placeholder_1),
            FeaturesResources.ResourceManager,
            typeof(FeaturesResources));

        private static readonly LocalizableString Description = new LocalizableResourceString(
            nameof(FeaturesResources.Invalid_format_string),
            FeaturesResources.ResourceManager,
            typeof(FeaturesResources));

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            DiagnosticCategory.Compiler,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract TSyntaxKind GetInvocationExpressionSyntaxKind();
        protected abstract string GetLiteralExpressionSyntaxAsString(SyntaxNode syntaxNode);
        protected abstract int GetLiteralExpressionSyntaxSpanStart(SyntaxNode syntaxNode);
        protected abstract bool TryGetFormatStringLiteralExpressionSyntax(
            SemanticModel semanticModel,
            SeparatedSyntaxList<SyntaxNode> arguments,
            ImmutableArray<IParameterSymbol> parameters,
            out SyntaxNode formatStringLiteralExpressionSyntax);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(startContext =>
            {
                var formatProviderType = startContext.Compilation.GetTypeByMetadataName(
                    "System.IFormatProvider");
                if (formatProviderType == null)
                {
                    return;
                }

                startContext.RegisterSyntaxNodeAction(c => AnalyzeNode(c, formatProviderType),
                    GetInvocationExpressionSyntaxKind());
            });
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol formatProviderType)
        {
            var syntaxFacts = GetSyntaxFactsService();
            var expression = syntaxFacts.GetExpressionOfInvocationExpression(context.Node);

            if (GetMethodName(syntaxFacts, expression) != nameof(string.Format))
            {
                return;
            }

            var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(context.Node);
            var numberOfArguments = arguments.Count;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);

            if (!TryGetValidFormatMethodSymbol(context, numberOfArguments, symbolInfo, out var method))
            {
                return;
            }

            var parameters = method.Parameters;

            // If the chosen overload has a params object[] that is being passed an array of
            // reference types then an actual array must be being passed instead of passing one
            // argument at a time because we know the set of overloads and how overload resolution
            // will work.  This array may have been created somewhere we can't analyze, so never
            // squiggle in this case.
            if (ArgsIsArrayOfReferenceTypes(context.SemanticModel, arguments, parameters))
            {
                return;
            }

            if (!TryGetFormatStringLiteralExpressionSyntax(
                context.SemanticModel,
                arguments,
                parameters,
                out var formatStringLiteralExpressionSyntax))
            {
                return;
            }

            if (parameters.Length == 0)
            {
                return;
            }

            var hasIFormatProvider = parameters[0].Type.Equals(formatProviderType);

            // We know the format string parameter exists so numberOfArguments is at least one,
            // and at least 2 if there is also an IFormatProvider.  The result can be zero if 
            // calling string.Format("String only with no placeholders"), where there is an 
            // empty params array.
            var numberOfPlaceholderArguments = numberOfArguments - (hasIFormatProvider ? 2 : 1);

            var formatString = GetLiteralExpressionSyntaxAsString(formatStringLiteralExpressionSyntax);

            if (FormatCallWorksAtRuntime(formatString, numberOfPlaceholderArguments))
            {
                return;
            }
            
            ValidateAndReportDiagnostic(context, numberOfPlaceholderArguments,
                formatString, GetLiteralExpressionSyntaxSpanStart(formatStringLiteralExpressionSyntax));
        }

        private string GetMethodName(ISyntaxFactsService syntaxFacts, SyntaxNode expression)
        {
            if (syntaxFacts.IsSimpleMemberAccessExpression(expression))
            {
                var nameOfMemberAccessExpression = syntaxFacts.GetNameOfMemberAccessExpression(expression);
                return syntaxFacts.GetIdentifierOfSimpleName(nameOfMemberAccessExpression).ValueText;
            }

            if (syntaxFacts.IsIdentifierName(expression))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(expression).ValueText;
            }

            return null;
        }

        private bool ArgsIsArrayOfReferenceTypes(
            SemanticModel semanticModel,
            SeparatedSyntaxList<SyntaxNode> arguments,
            ImmutableArray<IParameterSymbol> parameters)
        {
            if (!TryGetArgsArgumentType(semanticModel, arguments, parameters, out var argsArgumentType))
            {
                return false;
            }

            if (argsArgumentType.TypeKind != TypeKind.Array)
            {
                return false;
            }

            var argsArray = (IArrayTypeSymbol)argsArgumentType;
            return argsArray.ElementType.IsReferenceType;
        }

        internal abstract bool TryGetArgsArgumentType(
            SemanticModel semanticModel,
            SeparatedSyntaxList<SyntaxNode> arguments,
            ImmutableArray<IParameterSymbol> parameters,
            out ITypeSymbol argsArgumentType);

        protected static bool TryGetValidFormatMethodSymbol(
            SyntaxNodeAnalysisContext context,
            int numberOfArguments,
            SymbolInfo symbolInfo,
            out IMethodSymbol method)
        {
            method = null;

            if (symbolInfo.Symbol == null)
            {
                return false;
            }

            if (symbolInfo.Symbol.Kind != SymbolKind.Method)
            {
                return false;
            }

            if (!(symbolInfo.Symbol.ContainingSymbol is INamedTypeSymbol containingType))
            {
                return false;
            }

            if (containingType.SpecialType != SpecialType.System_String)
            {
                return false;
            }

            method = (IMethodSymbol)symbolInfo.Symbol;

            if (method.Arity > 0)
            {
                return false;
            }

            return true;
        }

        private bool FormatCallWorksAtRuntime(string formatString, int numberOfPlaceholderArguments)
        {
            var testArray = new object[numberOfPlaceholderArguments];
            for (var i = 0; i < numberOfPlaceholderArguments; i++)
            {
                testArray[i] = "test";
            }

            try
            {
                string.Format(formatString, testArray);
            }
            catch
            {
                return false;
            }

            return true;
        }

        protected static void ValidateAndReportDiagnostic(
            SyntaxNodeAnalysisContext context,
            int numberOfPlaceholderArguments,
            string formatString,
            int formatStringPosition)
        {
            var formatStringWithEscapedBracketsRemoved = RemoveEscapedBrackets(formatString);

            var matches = s_extractPlaceholdersRegex.Matches(formatStringWithEscapedBracketsRemoved);
            foreach (Match match in matches)
            {
                var textInsideBrackets = match.Groups[1].Value;

                if (!PlaceholderIndexIsValid(textInsideBrackets, numberOfPlaceholderArguments))
                {
                    var invalidPlaceholderText = "{" + textInsideBrackets + "}";
                    var invalidPlaceholderLocation = Location.Create(
                        context.Node.SyntaxTree,
                        new Text.TextSpan(formatStringPosition + match.Index, invalidPlaceholderText.Length));
                    var diagnostic = Diagnostic.Create(
                        Rule, 
                        invalidPlaceholderLocation, 
                        formatString.Replace("\n", "\\n").Replace("\r", "\\r"),
                        invalidPlaceholderText);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static string RemoveEscapedBrackets(string formatString)
            => s_removeEscapedBracketsRegex.Replace(formatString, "  ");

        private static bool PlaceholderIndexIsValid(string textInsideBrackets, int numberOfPlaceholderArguments)
        {
            var placeholderIndexText = (textInsideBrackets.IndexOf(",") > 0)
                ? textInsideBrackets.Split(',')[0]
                : textInsideBrackets.Split(':')[0];

            // placeholders cannot begin with whitespace
            if (placeholderIndexText.Length > 0 && char.IsWhiteSpace(placeholderIndexText, 0))
            {
                return false;
            }

            if (!int.TryParse(placeholderIndexText, out var placeholderIndex))
            {
                return false;
            }

            if (placeholderIndex >= numberOfPlaceholderArguments)
            {
                return false;
            }

            return true;
        }
    }
}