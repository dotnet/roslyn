// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.ValidateFormatString
{
    internal abstract class AbstractValidateFormatStringDiagnosticAnalyzer<TSyntaxKind> 
        : DiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        private const string DiagnosticID = IDEDiagnosticIds.ValidateFormatStringDiagnosticID;

        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(FeaturesResources.Invalid_format_string),
            FeaturesResources.ResourceManager,
            typeof(FeaturesResources));

        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
            nameof(FeaturesResources.Format_string_contains_invalid_placeholder_0),
            FeaturesResources.ResourceManager,
            typeof(FeaturesResources));

        private static readonly LocalizableString Description = new LocalizableResourceString(
            nameof(FeaturesResources.Invalid_format_string),
            FeaturesResources.ResourceManager,
            typeof(FeaturesResources));

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticID,
            Title,
            MessageFormat,
            DiagnosticCategory.Compiler,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        // this regex is used to remove escaped brackets from
        // the format string before looking for valid {} pairs
        private static Regex s_removeEscapedBracketsRegex = new Regex("{{");

        // this regex is used to extract the text between the
        // brackets and save the contents in a MatchCollection
        private static Regex s_extractPlaceholdersRegex = new Regex("{(.*?)}");

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract TSyntaxKind GetInvocationExpressionSyntaxKind();
        protected abstract SyntaxNode GetArgumentExpression(SyntaxNode syntaxNode);
        protected abstract ITypeSymbol GetArgumentExpressionType(SemanticModel semanticModel, SyntaxNode argsArgument);
        protected abstract SyntaxNode GetMatchingNamedArgument(SeparatedSyntaxList<SyntaxNode> arguments, string searchArgumentName);
        protected abstract bool ArgumentExpressionIsStringLiteral(SyntaxNode syntaxNode);

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

                startContext.RegisterSyntaxNodeAction(
                    c => AnalyzeNode(c, formatProviderType),
                    GetInvocationExpressionSyntaxKind());
            });
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol formatProviderType)
        {
            var optionSet = context.Options.GetDocumentOptionSetAsync(
                context.Node.SyntaxTree, context.CancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            if (optionSet.GetOption(
                ValidateFormatStringOption.WarnOnInvalidStringDotFormatCalls, 
                context.SemanticModel.Language) == false)
            {
                return;
            };

            var syntaxFacts = GetSyntaxFactsService();
            var expression = syntaxFacts.GetExpressionOfInvocationExpression(context.Node);

            if (!IsValidFormatMethod(syntaxFacts, expression))
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

            var formatStringLiteralExpressionSyntax = TryGetFormatStringLiteralExpressionSyntax(
                context.SemanticModel,
                arguments,
                parameters);

            if (formatStringLiteralExpressionSyntax == null)
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

            var formatString = formatStringLiteralExpressionSyntax.ToString();

            if (FormatCallWorksAtRuntime(formatString, numberOfPlaceholderArguments))
            {
                return;
            }
            
            ValidateAndReportDiagnostic(context, numberOfPlaceholderArguments,
                formatString, formatStringLiteralExpressionSyntax.SpanStart);
        }

        private bool IsValidFormatMethod(ISyntaxFactsService syntaxFacts, SyntaxNode expression)
        {
            // When calling string.Format(...), the expression will be MemberAccessExpressionSyntax
            if (syntaxFacts.IsSimpleMemberAccessExpression(expression))
            {
                var nameOfMemberAccessExpression = syntaxFacts.GetNameOfMemberAccessExpression(expression);
                return !syntaxFacts.IsGenericName(nameOfMemberAccessExpression)
                    && syntaxFacts.GetIdentifierOfSimpleName(nameOfMemberAccessExpression).
                    ValueText.Equals(nameof(string.Format));
            }

            // When using static System.String and calling Format(...), the expression will be 
            // IdentifierNameSyntax
            if (syntaxFacts.IsIdentifierName(expression))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(expression).ValueText.Equals(
                    nameof(string.Format));
            }
            
            return false;
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

        private bool TryGetArgsArgumentType(
            SemanticModel semanticModel,
            SeparatedSyntaxList<SyntaxNode> arguments,
            ImmutableArray<IParameterSymbol> parameters,
            out ITypeSymbol argsArgumentType)
        {
            var nameOfArgsParameter = "args";
            if (!TryGetArgument(
                semanticModel, 
                nameOfArgsParameter, 
                arguments, 
                parameters, 
                out var argsArgument))
            {
                argsArgumentType = null;
                return false;
            }

            argsArgumentType = GetArgumentExpressionType(semanticModel, argsArgument);
            return true;
        }

        protected bool TryGetArgument(
            SemanticModel semanticModel,
            string searchArgumentName,
            SeparatedSyntaxList<SyntaxNode> arguments,
            ImmutableArray<IParameterSymbol> parameters,
            out SyntaxNode argumentSyntax)
        {
            argumentSyntax = null;

            // First, look for a named argument that matches
            var namedArguments = GetMatchingNamedArgument(arguments, searchArgumentName);

            if (namedArguments != null)
            {
                argumentSyntax = namedArguments;
                return true;
            }

            // If no named argument exists, look for the named parameter
            // and return the corresponding argument
            var namedParameters = parameters.Where(p => p.Name.Equals(searchArgumentName));

            if (namedParameters.Count() != 1)
            {
                return false;
            }

            var namedParameter = namedParameters.Single();

            // For the case string.Format("Test string"), there is only one argument
            // but the compiler created an empty parameter array to bind to an overload
            if (namedParameter.Ordinal >= arguments.Count)
            {
                return false;
            }

            // Multiple arguments could have been converted to a single params array, 
            // so there wouldn't be a corresponding argument
            if (namedParameter.IsParams && parameters.Length != arguments.Count)
            {
                return false;
            }

            argumentSyntax = arguments[namedParameter.Ordinal];
            return true;
        }

        protected SyntaxNode TryGetFormatStringLiteralExpressionSyntax(
            SemanticModel semanticModel,
            SeparatedSyntaxList<SyntaxNode> arguments,
            ImmutableArray<IParameterSymbol> parameters)
        {
            var nameOfFormatParameter = "format";

            if (!TryGetArgument(
                semanticModel, 
                nameOfFormatParameter, 
                arguments, 
                parameters, 
                out var formatArgumentSyntax))
            {
                return null;
            }

            if (!ArgumentExpressionIsStringLiteral(formatArgumentSyntax))
            {
                return null;
            }

            return GetArgumentExpression(formatArgumentSyntax);
        }

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

            var containingType = (INamedTypeSymbol)symbolInfo.Symbol.ContainingSymbol;

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

        protected void ValidateAndReportDiagnostic(
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
                        new Text.TextSpan(
                            formatStringPosition + match.Index, 
                            invalidPlaceholderText.Length));
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        invalidPlaceholderLocation, 
                        invalidPlaceholderText);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static string RemoveEscapedBrackets(string formatString)
            => s_removeEscapedBracketsRegex.Replace(formatString, "  ");

        private static bool PlaceholderIndexIsValid(
            string textInsideBrackets, 
            int numberOfPlaceholderArguments)
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