// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.MoveToResx;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpMoveToResxDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = IDEDiagnosticIds.MoveToResxDiagnosticId;

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(CSharpFeaturesResources.Move_to_Resx), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(CSharpFeaturesResources.Use_Resx_for_user_facing_strings), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor s_moveToResxRule = new DiagnosticDescriptor(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_moveToResxRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
    }

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var stringLiteral = (LiteralExpressionSyntax)context.Node;
        var valueText = stringLiteral.Token.ValueText;

        // Quick early filters for obviously non-user-facing strings
        if (string.IsNullOrWhiteSpace(valueText) || valueText.Length < 3)
            return;

        // Filter 1: Basic content-based filters
        if (IsLikelyNonUserFacingContent(valueText))
            return;

        // Filter 2: Context-based filters - check where the string is being used
        if (IsInNonUserFacingContext(stringLiteral))
            return;

        // Filter 3: Pattern-based filters for common non-user-facing patterns
        if (MatchesNonUserFacingPatterns(valueText))
            return;

        // If it passes all filters, it's likely a user-facing string
        var diagnostic = Diagnostic.Create(s_moveToResxRule, stringLiteral.GetLocation(), valueText);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsLikelyNonUserFacingContent(string valueText)
    {
        // 1. Ignore strings that look like identifiers or code (but allow single common words)
        if (valueText.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
            // Allow common user-facing single words that might be identifiers
            var commonUserWords = new[] { "Ready", "Done", "OK", "Yes", "No", "Save", "Load", "Open", "Close", "New", "Edit", "Delete", "Cancel", "Submit", "Start", "Stop", "Pause", "Play", "Next", "Previous", "Home", "Back", "Forward", "Up", "Down", "Left", "Right", "Help", "About", "Settings", "Options", "Preferences", "File", "View", "Tools", "Window", "Search", "Find", "Replace", "Print", "Copy", "Cut", "Paste", "Undo", "Redo" };
            if (!commonUserWords.Contains(valueText, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        // 2. Ignore file paths, URLs, and system identifiers (enhanced)
        if (valueText.Contains("\\") || valueText.Contains("/") ||
            valueText.Contains(".cs") || valueText.Contains(".dll") || valueText.Contains(".exe") ||
            valueText.Contains(".json") || valueText.Contains(".xml") || valueText.Contains(".config") ||
            valueText.Contains(".txt") || valueText.Contains(".log") || valueText.Contains(".tmp"))
        {
            return true;
        }

        // Check for URLs and paths more specifically
        if (valueText.Contains("://") || valueText.Contains("www.") ||
            (valueText.Contains(":") && valueText.Length > 2 && char.IsLetter(valueText[0]) && valueText[1] == ':'))
        {
            return true;
        }

        // 3. Enhanced namespace-like strings detection
        if (valueText.StartsWith("System.") || valueText.StartsWith("Microsoft.") ||
            valueText.StartsWith("System:") || valueText.StartsWith("Microsoft:") ||
            valueText.Contains("::"))
        {
            return true;
        }

        // More sophisticated namespace detection
        if (valueText.Contains(".") && valueText.Split('.').Length > 2)
        {
            var parts = valueText.Split('.');
            // Check if it looks like a namespace (multiple PascalCase parts)
            if (parts.Length >= 3 && parts.All(part => part.Length > 0 && char.IsUpper(part[0])))
                return true;
        }

        // 4. Ignore numbers, GUIDs, and technical identifiers
        if (Guid.TryParse(valueText, out _) || double.TryParse(valueText, out _) ||
            long.TryParse(valueText, out _) || DateTime.TryParse(valueText, out _))
        {
            return true;
        }

        // 5. Enhanced resource keys and constants detection
        if (valueText.All(c => char.IsUpper(c) || c == '_' || char.IsDigit(c)) && valueText.Length > 3 ||
            valueText.StartsWith("HKEY_") || valueText.StartsWith("REG_"))
        {
            return true;
        }

        // 6. Ignore strings with only punctuation or special characters
        if (valueText.All(c => char.IsPunctuation(c) || char.IsWhiteSpace(c) || char.IsSymbol(c)))
            return true;

        // 7. Enhanced technical prefixes/suffixes
        if (valueText.StartsWith("ERR_") || valueText.StartsWith("WRN_") || valueText.StartsWith("INF_") ||
            valueText.StartsWith("LOG_") || valueText.StartsWith("DBG_") || valueText.StartsWith("TRC_") ||
            valueText.StartsWith("ID_") || valueText.StartsWith("CMD_") || valueText.StartsWith("CTL_") ||
            valueText.StartsWith("API_") || valueText.StartsWith("SQL_") || valueText.StartsWith("DB_") ||
            valueText.EndsWith("_ID") || valueText.EndsWith("_KEY") || valueText.EndsWith("_CODE") ||
            valueText.EndsWith("_TYPE") || valueText.EndsWith("_NAME"))
        {
            return true;
        }

        // 8. Enhanced regex patterns and format strings
        if ((valueText.Contains("\\") && (valueText.Contains("\\d") || valueText.Contains("\\w") || valueText.Contains("\\s"))) ||
            (valueText.Contains("{") && valueText.Contains("}") && valueText.All(c => char.IsDigit(c) || c == '{' || c == '}' || c == ',')))
        {
            return true;
        }

        // 9. Ignore SQL-like strings
        if (valueText.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase) ||
            valueText.StartsWith("INSERT ", StringComparison.OrdinalIgnoreCase) ||
            valueText.StartsWith("UPDATE ", StringComparison.OrdinalIgnoreCase) ||
            valueText.StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase) ||
            valueText.StartsWith("CREATE ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 10. Ignore base64-like strings and hashes
        if (valueText.Length > 20 && valueText.All(c => char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '='))
            return true;

        // 11. Enhanced technical environment/configuration values
        var technicalValues = new[] { "production", "development", "staging", "test", "debug", "release",
            "true", "false", "enabled", "disabled", "on", "off", "yes", "no", "1", "0" };
        if (technicalValues.Contains(valueText, StringComparer.OrdinalIgnoreCase) && valueText.Length <= 12)
            return true;

        // 12. Common search/query patterns
        if (valueText.Contains("-") && valueText.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_') &&
            (valueText.Contains("search") || valueText.Contains("query") || valueText.Contains("filter")))
        {
            return true;
        }

        return false;
    }

    private static bool IsInNonUserFacingContext(LiteralExpressionSyntax stringLiteral)
    {
        var parent = stringLiteral.Parent;

        // Check if string is used as an argument to a method call
        if (parent is ArgumentSyntax arg)
        {
            var invocation = arg.Parent?.Parent as InvocationExpressionSyntax;
            if (invocation != null)
            {
                var invoked = invocation.Expression.ToString();

                // Comprehensive list of non-user-facing method patterns
                if (IsNonUserFacingMethodCall(invoked))
                    return true;

                // Check parameter name context
                if (IsNonUserFacingParameterContext(arg, invocation))
                    return true;
            }
        }

        // Check if string is used in attribute
        if (IsInAttributeContext(stringLiteral))
            return true;

        // Check if string is used in conditional compilation or preprocessor
        if (IsInPreprocessorContext(stringLiteral))
            return true;

        // Check if string is used in exception throwing
        if (IsInExceptionContext(stringLiteral))
            return true;

        return false;
    }

    private static bool IsNonUserFacingMethodCall(string invoked)
    {
        // Logging and debugging
        if (invoked.Contains("Log") || invoked.Contains("Debug") || invoked.Contains("Trace") ||
            invoked.Contains("Assert") || invoked.Contains("WriteLine") || invoked.Contains("Write"))
        {
            return true;
        }

        // Exception handling
        if (invoked.Contains("Throw") || invoked.Contains("Exception") || invoked.Contains("Error"))
            return true;

        // Parsing and conversion
        if (invoked.Contains("Parse") || invoked.Contains("TryParse") || invoked.Contains("Convert") ||
            invoked.Contains("ToString") || invoked.Contains("Format"))
        {
            return true;
        }

        // Reflection and type operations
        if (invoked.Contains("Type.GetType") || invoked.Contains("typeof") || invoked.Contains("nameof") ||
            invoked.Contains("Activator.CreateInstance") || invoked.Contains("GetType") ||
            invoked.Contains("Assembly.Load") || invoked.Contains("Assembly.GetType"))
        {
            return true;
        }

        // Property/field access via reflection or configuration
        if (invoked.Contains("SetProperty") || invoked.Contains("GetProperty") ||
            invoked.Contains("SetField") || invoked.Contains("GetField") ||
            invoked.Contains("SetValue") || invoked.Contains("GetValue") ||
            invoked.Contains("ConfigurationManager") || invoked.Contains("AppSettings") ||
            invoked.Contains("SetConfiguration") || invoked.Contains("GetConfiguration"))
        {
            return true;
        }

        // Comparison and validation (be more specific to avoid user validation messages)
        if (invoked.Contains("Equals") || invoked.Contains("Compare") || invoked.Contains("Hash") ||
            invoked.Contains("Assert.") || invoked.Contains("Validate.") || invoked.Contains("Check."))
        {
            return true;
        }

        // Serialization and data access
        if (invoked.Contains("Serialize") || invoked.Contains("Deserialize") ||
            invoked.Contains("JsonConvert") || invoked.Contains("XmlSerializer") ||
            invoked.Contains("DataReader") || invoked.Contains("SqlCommand") ||
            invoked.Contains("ExecuteScalar") || invoked.Contains("ExecuteNonQuery"))
        {
            return true;
        }

        // String manipulation utilities (be more specific to avoid user text)
        if (invoked.Contains("string.Concat") || invoked.Contains("string.Join") ||
            invoked.Contains("string.Replace") || invoked.Contains("Regex") || invoked.Contains("Match"))
        {
            return true;
        }

        // StringBuilder - only filter if it's clearly for technical purposes
        // Remove overly broad StringBuilder filtering to allow user content

        // File and IO operations
        if (invoked.Contains("File.") || invoked.Contains("Directory.") ||
            invoked.Contains("Path.") || invoked.Contains("Stream") ||
            invoked.Contains("Reader") || invoked.Contains("Writer") ||
            invoked.Contains("LoadFile") || invoked.Contains("SaveFile"))
        {
            return true;
        }

        // HTTP and web operations
        if (invoked.Contains("HttpClient") || invoked.Contains("WebRequest") ||
            invoked.Contains("RestClient") || invoked.Contains("HttpGet") ||
            invoked.Contains("HttpPost") || invoked.Contains("Uri"))
        {
            return true;
        }

        // Diagnostic and telemetry
        if (invoked.Contains("Diagnostic") || invoked.Contains("Telemetry") ||
            invoked.Contains("Counter") || invoked.Contains("Meter") ||
            invoked.Contains("EventSource") || invoked.Contains("Activity"))
        {
            return true;
        }

        // Search and query operations (technical)
        if (invoked.Contains("ExecuteSearch") || invoked.Contains("ExecuteQuery") ||
            invoked.Contains("Search") && (invoked.Contains("Database") || invoked.Contains("Index") || invoked.Contains("Engine")))
        {
            return true;
        }

        return false;
    }

    private static bool IsNonUserFacingParameterContext(ArgumentSyntax arg, InvocationExpressionSyntax invocation)
    {
        // Try to get parameter name from argument syntax
        var argumentList = invocation.ArgumentList;
        if (argumentList != null)
        {
            var argumentIndex = argumentList.Arguments.IndexOf(arg);
            if (argumentIndex >= 0)
            {
                // Check if this is a named argument
                if (arg.NameColon != null)
                {
                    var paramName = arg.NameColon.Name.Identifier.ValueText.ToLowerInvariant();
                    return IsNonUserFacingParameterName(paramName);
                }
            }
        }

        return false;
    }

    private static bool IsNonUserFacingParameterName(string paramName)
    {
        return paramName switch
        {
            "key" or "name" or "id" or "identifier" or "code" or "type" or
            "path" or "filename" or "filepath" or "directory" or "folder" or
            "url" or "uri" or "endpoint" or "connectionstring" or
            "sql" or "query" or "command" or "script" or
            "pattern" or "regex" or "format" or "template" or
            "namespace" or "assembly" or "typename" or "methodname" or
            "propertyname" or "fieldname" or "eventname" or
            "category" or "source" or "level" or "severity" or
            "configuration" or "setting" or "option" or "parameter" or
            "value" or "config" or "env" or "environment" => true,
            _ => false
        };
    }

    private static bool IsInAttributeContext(LiteralExpressionSyntax stringLiteral)
    {
        var current = stringLiteral.Parent;
        while (current != null)
        {
            if (current is AttributeSyntax)
                return true;
            current = current.Parent;
        }
        return false;
    }

    private static bool IsInPreprocessorContext(LiteralExpressionSyntax stringLiteral)
    {
        // Check if we're in a preprocessor directive
        var token = stringLiteral.GetFirstToken();
        return token.HasLeadingTrivia && token.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.PreprocessingMessageTrivia));
    }

    private static bool IsInExceptionContext(LiteralExpressionSyntax stringLiteral)
    {
        var parent = stringLiteral.Parent;
        while (parent != null)
        {
            if (parent is ThrowStatementSyntax)
                return true;

            if (parent is ObjectCreationExpressionSyntax objCreation)
            {
                var typeName = objCreation.Type?.ToString();
                if (typeName != null && typeName.Contains("Exception"))
                    return true;
            }

            parent = parent.Parent;
        }
        return false;
    }

    private static bool MatchesNonUserFacingPatterns(string valueText)
    {
        // 1. Version numbers (e.g., "1.0.0.0", "v2.1.3")
        if (System.Text.RegularExpressions.Regex.IsMatch(valueText, @"^v?\d+\.\d+(\.\d+)*$"))
            return true;

        // 2. Environment variables and configuration keys (enhanced)
        if (valueText.All(c => char.IsUpper(c) || c == '_') && valueText.Length > 2)
            return true;

        // Technical configuration patterns
        if (valueText.All(c => char.IsLower(c) || c == '_' || c == '-' || char.IsDigit(c)) &&
            (valueText.Contains("_") || valueText.Contains("-")) && valueText.Length > 4)
        {
            return true;
        }

        // 3. MIME types (e.g., "application/json", "text/html")
        if (valueText.Contains("/") && valueText.Split('/').Length == 2 &&
            !valueText.Contains(" ")) // Ensure it's not a sentence with "/"
        {
            return true;
        }

        // 4. Color codes and hex values
        if (valueText.StartsWith("#") && valueText.Length > 1 &&
            valueText.Skip(1).All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
        {
            return true;
        }

        // 5. Connection strings patterns (enhanced)
        if (valueText.Contains("=") && (valueText.Contains(";") || valueText.Contains("&")) &&
            !valueText.Contains(" ")) // Avoid sentences that might contain = and ;
        {
            return true;
        }

        // 6. JSON/XML-like patterns (be more specific)
        if ((valueText.StartsWith("{") && valueText.EndsWith("}") && valueText.Contains(":")) ||
            (valueText.StartsWith("[") && valueText.EndsWith("]") && valueText.Contains(",")) ||
            (valueText.StartsWith("<") && valueText.EndsWith(">") && valueText.Contains("/")))
        {
            return true;
        }

        // 7. Base64 padding patterns (more specific)
        if ((valueText.EndsWith("=") || valueText.EndsWith("==")) && valueText.Length > 8 &&
            valueText.All(c => char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '='))
        {
            return true;
        }

        // 8. Common technical separators (only if it's ONLY separators)
        if (valueText.Length <= 10 && valueText.All(c => ".,;:|!@#$%^&*()_+-=[]{}\\|`~".Contains(c)))
            return true;

        // 9. Technical hyphenated terms (but allow user-facing hyphenated terms)
        if (valueText.Contains("-") && valueText.Split('-').Length > 2 &&
            valueText.All(c => char.IsLetterOrDigit(c) || c == '-') &&
            (valueText.Contains("config") || valueText.Contains("api") || valueText.Contains("db") ||
             valueText.Contains("sql") || valueText.Contains("json") || valueText.Contains("xml")))
        {
            return true;
        }

        // 10. Common single technical words (but preserve user-facing ones)
        var technicalOnlyWords = new[] { "localhost", "admin", "root", "null", "undefined", "void", "temp", "tmp" };
        if (technicalOnlyWords.Contains(valueText, StringComparer.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
