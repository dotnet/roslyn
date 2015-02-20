// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV1;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public static class DiagnosticProviderTestUtilities
    {
        private static IEnumerable<Diagnostic> GetDiagnostics(DiagnosticAnalyzer analyzerOpt, Document document, TextSpan span, Project project, bool getDocumentDiagnostics, bool getProjectDiagnostics, bool donotCatchAnalyzerExceptions)
        {
            var documentDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();
            var projectDiagnostics = SpecializedCollections.EmptyEnumerable<Diagnostic>();

            // If no user diagnostic analyzer, then test compiler diagnostics.
            var analyzer = analyzerOpt ?? DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(project.Language);

            if (getDocumentDiagnostics)
            {
                var tree = document.GetSyntaxTreeAsync().Result;
                var root = document.GetSyntaxRootAsync().Result;
                var semanticModel = document.GetSemanticModelAsync().Result;

                var builder = new List<Diagnostic>();
                var nodeInBodyAnalyzerService = document.Project.Language == LanguageNames.CSharp ?
                    (ISyntaxNodeAnalyzerService)new CSharpSyntaxNodeAnalyzerService() :
                    new VisualBasicSyntaxNodeAnalyzerService();

                // Lets replicate the IDE diagnostic incremental analyzer behavior to determine span to test:
                //  (a) If the span is contained within a method level member and analyzer supports semantic in span: analyze in member span.
                //  (b) Otherwise, analyze entire syntax tree span.
                var spanToTest = root.FullSpan;

                var driver = new DiagnosticAnalyzerDriver(document, spanToTest, root, syntaxNodeAnalyzerService: nodeInBodyAnalyzerService, cancellationToken: CancellationToken.None, testOnly_DonotCatchAnalyzerExceptions: donotCatchAnalyzerExceptions);
                var diagnosticAnalyzerCategory = analyzer.GetDiagnosticAnalyzerCategory(driver);
                bool supportsSemanticInSpan = (diagnosticAnalyzerCategory & DiagnosticAnalyzerCategory.SemanticSpanAnalysis) != 0;
                if (supportsSemanticInSpan)
                {
                    var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                    if (syntaxFacts != null)
                    {
                        var member = syntaxFacts.GetContainingMemberDeclaration(root, span.Start);
                        if (member != null && syntaxFacts.IsMethodLevelMember(member) && member.FullSpan.Contains(span))
                        {
                            spanToTest = member.FullSpan;
                        }
                    }
                }

                if ((diagnosticAnalyzerCategory & DiagnosticAnalyzerCategory.SyntaxAnalysis) != 0)
                {
                    builder.AddRange(driver.GetSyntaxDiagnosticsAsync(analyzer).Result ?? SpecializedCollections.EmptyEnumerable<Diagnostic>());
                }

                if (supportsSemanticInSpan || (diagnosticAnalyzerCategory & DiagnosticAnalyzerCategory.SemanticDocumentAnalysis) != 0)
                {
                    builder.AddRange(driver.GetSemanticDiagnosticsAsync(analyzer).Result ?? SpecializedCollections.EmptyEnumerable<Diagnostic>());
                }

                documentDiagnostics = builder.Where(d => d.Location == Location.None ||
                    (d.Location.SourceTree == tree && d.Location.SourceSpan.IntersectsWith(span)));
            }

            if (getProjectDiagnostics)
            {
                var nodeInBodyAnalyzerService = project.Language == LanguageNames.CSharp ?
                    (ISyntaxNodeAnalyzerService)new CSharpSyntaxNodeAnalyzerService() :
                    new VisualBasicSyntaxNodeAnalyzerService();
                var driver = new DiagnosticAnalyzerDriver(project, nodeInBodyAnalyzerService, CancellationToken.None);

                if (analyzer.SupportsProjectDiagnosticAnalysis(driver))
                {
                    projectDiagnostics = driver.GetProjectDiagnosticsAsync(analyzer, null).Result ?? SpecializedCollections.EmptyEnumerable<Diagnostic>();
                }
            }

            return documentDiagnostics.Concat(projectDiagnostics);
        }

        public static IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer providerOpt, Document document, TextSpan span, bool donotCatchAnalyzerExceptions = true)
        {
            return GetDiagnostics(providerOpt, document, span, document.Project, getDocumentDiagnostics: true, getProjectDiagnostics: true, donotCatchAnalyzerExceptions: donotCatchAnalyzerExceptions);
        }

        public static IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer providerOpt, Project project, bool donotCatchAnalyzerExceptions = true)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var document in project.Documents)
            {
                var span = document.GetSyntaxRootAsync().Result.FullSpan;
                var documentDiagnostics = GetDocumentDiagnostics(providerOpt, document, span, donotCatchAnalyzerExceptions);
                diagnostics.AddRange(documentDiagnostics);
            }

            var projectDiagnostics = GetProjectDiagnostics(providerOpt, project, donotCatchAnalyzerExceptions);
            diagnostics.AddRange(projectDiagnostics);
            return diagnostics;
        }

        public static IEnumerable<Diagnostic> GetAllDiagnostics(DiagnosticAnalyzer providerOpt, Solution solution, bool donotCatchAnalyzerExceptions = true)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var project in solution.Projects)
            {
                var projectDiagnostics = GetAllDiagnostics(providerOpt, project, donotCatchAnalyzerExceptions);
                diagnostics.AddRange(projectDiagnostics);
            }

            return diagnostics;
        }

        public static IEnumerable<Diagnostic> GetDocumentDiagnostics(DiagnosticAnalyzer providerOpt, Document document, TextSpan span, bool donotCatchAnalyzerExceptions = true)
        {
            return GetDiagnostics(providerOpt, document, span, document.Project, getDocumentDiagnostics: true, getProjectDiagnostics: false, donotCatchAnalyzerExceptions: donotCatchAnalyzerExceptions);
        }

        public static IEnumerable<Diagnostic> GetProjectDiagnostics(DiagnosticAnalyzer providerOpt, Project project, bool donotCatchAnalyzerExceptions = true)
        {
            return GetDiagnostics(providerOpt, null, default(TextSpan), project, getDocumentDiagnostics: false, getProjectDiagnostics: true, donotCatchAnalyzerExceptions: donotCatchAnalyzerExceptions);
        }
    }
}
