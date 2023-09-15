// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotConfigFeatures
{
    public const string CodeAnalysisRuleSuggestions = nameof(CodeAnalysisRuleSuggestions);
    public const string CodeAnalysisPackageSuggestions = nameof(CodeAnalysisPackageSuggestions);

    private const string CodeAnalysisRuleSuggestionsPrompt = @"
Read the text after 'Description:' part and list of available category intent tags after 'Categories:' part:

Description: {0}

Categories: {1}

Your task is to print a comma separated list of intent tags from this available categories intent list that are indicated as something of interest in the above Description in the following format:

Answer: <%Comma separated intent tags%>

Below you will find examples of how you should respond. Please follow the examples as closely as possible:

# EXAMPLE 1
Description: I am passionate about code style and performance.

Categories: Design, Performance, Security, Maintenance, Style

Answer: Style, Performance

# EXAMPLE 2
Description: I am interested in all categories.

Categories: Design, Performance, Security, Maintenance, Style

Answer: Design, Performance, Security, Maintenance, Style
";

    // TODO: 
    private const string CodeAnalysisSuggestionsPrompt2 = """
        You are an expert in C# code analysis and code style rules available in Visual Studio and .NET. Given a short description of user preferences and a list of available code analysis or code style rules, you can suggest the most relevant ones to use.

        Your task is to read the text after 'Description:' part and list of available rules after 'Rules:' part:

        Description: {0}

        Rules: {1}

        and perform the following steps:

            Step 1: First compute the list of intent tags from this available categories intent list that are indicated as something of interest in the above Description. Do not print any output for this step.

            Step 2: For each of the computed intent tags in step 1, compute an ordered list of all the related code quality rule CAxxxx IDs from https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ documentation page or an ordered list of all the related code style rule IDExxxx IDs from https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ documentation page. Do not print any output for this step.

            Step 3: Use the intent tags and corresponding ordered list of rule IDs computed in step 1 and 2 respectively to print out an answer containing list of strings, such that there is one string for each computed intent tag of interest in step 1 and each output string in the answer has the following format: ""<%Intent tag%>:<%Comma separated ordered list of rule IDs%>"". Print each string on a separate line.

        Below you will find examples of how you should respond. Please follow the examples as closely as possible:
        
        # EXAMPLE 1
        Description: 
        
        Rules: 
        
        Response:
        
        # EXAMPLE 2
        Description: 
        
        Rules: 
        
        Response:
        
        # END OF EXAMPLES
        
        Description: {0}
        
        Categories: {1}
        <|endofprompt|>
        [RESPONSE START]
        """;

    private const string CodeAnalysisPackageSuggestionPrompt = """
        You are an expert in C# code analysis and code style analyzer available for .NET developers. Given a short description of user preferences you will be able to suggest a single analyzer NuGet packages that they might be interested the most. You should only choose one package from the list below. Do not suggest package not in this list.

        {0}

        Your task is to suggest one Roslyn analyzer NuGet package based on the preference provided by a developer. Your should only suggest a single package, and the answer should only contain the name of suggested package and nothing else. However, if the developer indicated that they are not interested in installing any new analyzer package, you should not suggest any package and the answer should contain a single word "None".

        Below you will find examples of how you should respond. Please follow the examples as closely as possible:

        # EXAMPLE 1
        [Developer Preference] I am passionate about code style.

        [Suggestion]
        StyleCop.Analyzers

        # EXAMPLE 2
        [Developer Preference] I am interested in writing correct code.

        [Suggestion]
        SonarAnalyzer.CSharp
        
        # EXAMPLE 2
        [Developer Preference] I am interested in writing correct code. But I don't want to install any new analyzer package at the moment.
        
        [Suggestion]
        None

        # END OF EXAMPLES

        [Developer Preference] {1}

        <|endofprompt|>
        [Suggestion]
        """;

    public static string? GetPrompt(string feature, params string[] arguments)
    {
        var promptFormat = feature switch
        {
            CodeAnalysisRuleSuggestions => CodeAnalysisRuleSuggestionsPrompt,
            CodeAnalysisPackageSuggestions => CodeAnalysisPackageSuggestionPrompt,
            _ => null
        };

        if (promptFormat == null)
            return null;

        try
        {
            return string.Format(promptFormat, arguments);
        }
        catch (ArgumentNullException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
