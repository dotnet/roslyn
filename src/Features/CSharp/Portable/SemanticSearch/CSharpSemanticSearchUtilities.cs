// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal sealed class CSharpSemanticSearchUtilities
{
    public static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default;
    public static readonly CSharpCompilationOptions CompilationOptions = new(OutputKind.ConsoleApplication);

    public static readonly SemanticSearchProjectConfiguration Configuration = new()
    {
        Language = LanguageNames.CSharp,
        Query = """
            static IEnumerable<ISymbol> Find(Compilation compilation)
            {
                return compilation.Assembly.GlobalNamespace.GetMembers("C");
            }
            """,
        GlobalUsingsAndTools = $$"""
            global using System;
            global using System.Collections.Generic;
            global using System.Collections.Immutable;
            global using System.Linq;
            global using System.Threading;
            global using System.Threading.Tasks;
            global using Microsoft.CodeAnalysis;

            public static class {{SemanticSearchUtilities.ToolsTypeName}}
            {
                private static Func<ISymbol, IEnumerable<SyntaxNode>> {{SemanticSearchUtilities.FindReferencingSyntaxNodesImplName}};
                private static Func<SyntaxTree, Task<SemanticModel>> {{SemanticSearchUtilities.GetSemanticModelImplName}};

                /// <summary>
                /// Returns all syntax nodes that reference the given <paramref name="symbol" />.
                /// </summary>
                public static IEnumerable<SyntaxNode> FindReferencingSyntaxNodes(this ISymbol symbol)
                    => {{SemanticSearchUtilities.FindReferencingSyntaxNodesImplName}}(symbol);

                /// <summary>
                /// Returns the semantic model for the given <paramref name="tree" />.
                /// </summary>
                public static Task<SemanticModel> GetSemanticModelAsync(this SyntaxTree tree)
                    => {{SemanticSearchUtilities.GetSemanticModelImplName}}(tree);
            }
            """,
        EditorConfig = """
            is_global = true

            dotnet_analyzer_diagnostic.category-Documentation.severity = none
            dotnet_analyzer_diagnostic.category-Globalization.severity = none
            dotnet_analyzer_diagnostic.category-Interoperability.severity = none
            dotnet_analyzer_diagnostic.category-Design.severity = none
            dotnet_analyzer_diagnostic.category-Naming.severity = none
            dotnet_analyzer_diagnostic.category-Maintainability.severity = none
            dotnet_analyzer_diagnostic.category-Style.severity = none
        
            # CS8321: unused local function
            dotnet_diagnostic.CS8321.severity = none

            # IDE051: private member is unused
            dotnet_diagnostic.IDE051.severity = none
            """,
        ParseOptions = ParseOptions,
        CompilationOptions = CompilationOptions
    };
}
