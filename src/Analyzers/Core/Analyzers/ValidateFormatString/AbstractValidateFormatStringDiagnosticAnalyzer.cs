// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ValidateFormatString;

internal abstract class AbstractValidateFormatStringDiagnosticAnalyzer<TSyntaxKind>
    : DiagnosticAnalyzer
    where TSyntaxKind : struct
{
    private const string DiagnosticID = IDEDiagnosticIds.ValidateFormatStringDiagnosticID;

    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(AnalyzersResources.Invalid_format_string),
        AnalyzersResources.ResourceManager,
        typeof(AnalyzersResources));

    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
        nameof(AnalyzersResources.Format_string_contains_invalid_placeholder),
        AnalyzersResources.ResourceManager,
        typeof(AnalyzersResources));

#pragma warning disable RS0030 // Do not used banned APIs - We cannot use AbstractBuiltInCodeStyleDiagnosticAnalyzer nor AbstractCodeQualityDiagnosticAnalyzer.
    // This analyzer is run against generated code while the abstract base classes mentioned doesn't. The rule is also not documented.
    // There is even a current work to remove the rule completely in favor of CA2241.
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticID,
        Title,
        MessageFormat,
        DiagnosticCategory.Compiler,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        customTags: EnforceOnBuildValues.ValidateFormatString.ToCustomTag());
#pragma warning restore RS0030 // Do not used banned APIs

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Rule];

    /// <summary>
    /// this regex is used to remove escaped brackets from
    /// the format string before looking for valid {} pairs
    /// </summary>
    private static readonly Regex s_removeEscapedBracketsRegex = new("{{");

    /// <summary>
    /// this regex is used to extract the text between the
    /// brackets and save the contents in a MatchCollection
    /// </summary>
    private static readonly Regex s_extractPlaceholdersRegex = new("{(.*?)}");

    private const string NameOfArgsParameter = "args";
    private const string NameOfFormatStringParameter = "format";

    protected abstract ISyntaxFacts GetSyntaxFacts();
    protected abstract SyntaxNode GetArgumentExpression(SyntaxNode syntaxNode);
    protected abstract SyntaxNode? TryGetMatchingNamedArgument(SeparatedSyntaxList<SyntaxNode> arguments, string searchArgumentName);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(startContext =>
        {
            var formatProviderType = startContext.Compilation.GetTypeByMetadataName(typeof(System.IFormatProvider).FullName!);
            if (formatProviderType == null)
            {
                return;
            }

            var syntaxKinds = GetSyntaxFacts().SyntaxKinds;
            startContext.RegisterSyntaxNodeAction(
                c => AnalyzeNode(c, formatProviderType),
                syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.InvocationExpression));
        });
    }

    [PerformanceSensitive(
        "https://github.com/dotnet/roslyn/issues/23583",
        Constraint = "Reading editorconfig options is expensive and should be avoided if a syntax-based fast path exists.")]
    private void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol formatProviderType)
    {
        var syntaxFacts = GetSyntaxFacts();
        var expression = syntaxFacts.GetExpressionOfInvocationExpression(context.Node);

        if (!IsValidFormatMethod(syntaxFacts, expression))
        {
            return;
        }

        if (!context.GetIdeAnalyzerOptions().ReportInvalidPlaceholdersInStringDotFormatCalls)
        {
            return;
        }

        var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(context.Node);
        var symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);

        var method = TryGetValidFormatMethodSymbol(symbolInfo);
        if (method == null)
        {
            return;
        }

        var parameters = method.Parameters;

        // If the chosen overload has a params object[] that is being passed an array of
        // reference types then an actual array must be being passed instead of passing one
        // argument at a time because we know the set of overloads and how overload resolution
        // will work.  This array may have been created somewhere we can't analyze, so never
        // squiggle in this case.
        if (ArgsIsArrayOfReferenceTypes(context.SemanticModel, arguments, parameters, syntaxFacts))
        {
            return;
        }

        var formatStringLiteralExpressionSyntax = TryGetFormatStringLiteralExpressionSyntax(
            arguments, parameters, syntaxFacts);

        if (formatStringLiteralExpressionSyntax == null)
        {
            return;
        }

        if (parameters.Length == 0)
        {
            return;
        }

        var numberOfArguments = arguments.Count;
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

    private static bool IsValidFormatMethod(ISyntaxFacts syntaxFacts, SyntaxNode expression)
    {
        // When calling string.Format(...), the expression will be MemberAccessExpressionSyntax
        if (syntaxFacts.IsSimpleMemberAccessExpression(expression))
        {
            var nameOfMemberAccessExpression = syntaxFacts.GetNameOfMemberAccessExpression(expression);
            return !syntaxFacts.IsGenericName(nameOfMemberAccessExpression)
                && syntaxFacts.GetIdentifierOfSimpleName(nameOfMemberAccessExpression).ValueText
                == (nameof(string.Format));
        }

        // When using static System.String and calling Format(...), the expression will be
        // IdentifierNameSyntax
        if (syntaxFacts.IsIdentifierName(expression))
        {
            return syntaxFacts.GetIdentifierOfSimpleName(expression).ValueText
                == (nameof(string.Format));
        }

        return false;
    }

    private bool ArgsIsArrayOfReferenceTypes(
        SemanticModel semanticModel,
        SeparatedSyntaxList<SyntaxNode> arguments,
        ImmutableArray<IParameterSymbol> parameters,
        ISyntaxFacts syntaxFacts)
    {
        var argsArgumentType = TryGetArgsArgumentType(semanticModel, arguments, parameters, syntaxFacts);
        return argsArgumentType is IArrayTypeSymbol arrayType && arrayType.ElementType.IsReferenceType;
    }

    private ITypeSymbol? TryGetArgsArgumentType(
        SemanticModel semanticModel,
        SeparatedSyntaxList<SyntaxNode> arguments,
        ImmutableArray<IParameterSymbol> parameters,
        ISyntaxFacts syntaxFacts)
    {
        var argsArgument = TryGetArgument(NameOfArgsParameter, arguments, parameters);
        if (argsArgument == null)
        {
            return null;
        }

        Debug.Assert(syntaxFacts.IsArgument(argsArgument));
        var expression = syntaxFacts.GetExpressionOfArgument(argsArgument)!;
        return semanticModel.GetTypeInfo(expression).ConvertedType;
    }

    protected SyntaxNode? TryGetArgument(
        string searchArgumentName,
        SeparatedSyntaxList<SyntaxNode> arguments,
        ImmutableArray<IParameterSymbol> parameters)
    {
        // First, look for a named argument that matches
        var matchingNamedArgument = TryGetMatchingNamedArgument(arguments, searchArgumentName);
        if (matchingNamedArgument != null)
        {
            return matchingNamedArgument;
        }

        // If no named argument exists, look for the named parameter
        // and return the corresponding argument
        var parameterWithMatchingName = GetParameterWithMatchingName(parameters, searchArgumentName);
        if (parameterWithMatchingName == null)
        {
            return null;
        }

        // For the case string.Format("Test string"), there is only one argument
        // but the compiler created an empty parameter array to bind to an overload
        if (parameterWithMatchingName.Ordinal >= arguments.Count)
        {
            return null;
        }

        // Multiple arguments could have been converted to a single params array, 
        // so there wouldn't be a corresponding argument
        if (parameterWithMatchingName.IsParams && parameters.Length != arguments.Count)
        {
            return null;
        }

        return arguments[parameterWithMatchingName.Ordinal];
    }

    private static IParameterSymbol? GetParameterWithMatchingName(ImmutableArray<IParameterSymbol> parameters, string searchArgumentName)
    {
        foreach (var p in parameters)
        {
            if (p.Name == searchArgumentName)
            {
                return p;
            }
        }

        return null;
    }

    protected SyntaxNode? TryGetFormatStringLiteralExpressionSyntax(
        SeparatedSyntaxList<SyntaxNode> arguments,
        ImmutableArray<IParameterSymbol> parameters,
        ISyntaxFacts syntaxFacts)
    {
        var formatArgumentSyntax = TryGetArgument(
            NameOfFormatStringParameter,
            arguments,
            parameters);

        if (formatArgumentSyntax == null)
        {
            return null;
        }

        if (!syntaxFacts.IsStringLiteralExpression(syntaxFacts.GetExpressionOfArgument(formatArgumentSyntax)))
        {
            return null;
        }

        return GetArgumentExpression(formatArgumentSyntax);
    }

    protected static IMethodSymbol? TryGetValidFormatMethodSymbol(SymbolInfo symbolInfo)
    {
        if (symbolInfo.Symbol == null)
        {
            return null;
        }

        if (symbolInfo.Symbol.Kind != SymbolKind.Method)
        {
            return null;
        }

        if (((IMethodSymbol)symbolInfo.Symbol).MethodKind == MethodKind.LocalFunction)
        {
            return null;
        }

        var containingType = symbolInfo.Symbol.ContainingType;
        if (containingType == null)
        {
            return null;
        }

        if (containingType.SpecialType != SpecialType.System_String)
        {
            return null;
        }

        return (IMethodSymbol)symbolInfo.Symbol;
    }

    private static bool FormatCallWorksAtRuntime(string formatString, int numberOfPlaceholderArguments)
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
        // removing escaped left brackets and replacing with space characters so they won't
        // impede the extraction of placeholders, yet the locations of the placeholders are
        // the same as in the original string.
        var formatStringWithEscapedBracketsChangedToSpaces = RemoveEscapedBrackets(formatString);

        var matches = s_extractPlaceholdersRegex.Matches(formatStringWithEscapedBracketsChangedToSpaces);
        foreach (Match? match in matches)
        {
            RoslynDebug.AssertNotNull(match);

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

    /// <summary>
    /// removing escaped left brackets and replacing with space characters so they won't
    /// impede the extraction of placeholders, yet the locations of the placeholders are
    /// the same as in the original string.
    /// </summary>
    /// <param name="formatString"></param>
    /// <returns>string with left brackets removed and replaced by spaces</returns>
    private static string RemoveEscapedBrackets(string formatString)
        => s_removeEscapedBracketsRegex.Replace(formatString, "  ");

    private static bool PlaceholderIndexIsValid(
        string textInsideBrackets,
        int numberOfPlaceholderArguments)
    {
        var placeholderIndexText = textInsideBrackets.IndexOf(",") > 0
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
