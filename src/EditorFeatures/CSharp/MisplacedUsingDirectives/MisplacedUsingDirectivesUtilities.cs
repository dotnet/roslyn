// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives
{
    internal static class MisplacedUsingsUtilities
    {
        public static readonly LocalizableResourceString LocalizableTitle = new LocalizableResourceString(
           nameof(CSharpEditorResources.Misplaced_using_directive), CSharpEditorResources.ResourceManager, typeof(CSharpEditorResources));

        public static async Task<CodeStyleOption<AddImportPlacement>> GetPreferredPlacementOptionAsync(SyntaxNodeAnalysisContext context)
        {
            var options = await context.Options.GetDocumentOptionSetAsync(context.Node.SyntaxTree, context.CancellationToken);
            return options.GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement);
        }

        public static void ReportDiagnostics(
           SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor,
           IEnumerable<UsingDirectiveSyntax> usingDirectives, CodeStyleOption<AddImportPlacement> option)
        {
            foreach (var usingDirective in usingDirectives)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    descriptor,
                    usingDirective.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: null,
                    properties: null));
            }
        }
    }
}
