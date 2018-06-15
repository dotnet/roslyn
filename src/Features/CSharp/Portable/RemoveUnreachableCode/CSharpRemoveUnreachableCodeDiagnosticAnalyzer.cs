// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Fading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnreachableCodeDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        private const string CS0162 = nameof(CS0162); // Unreachable code detected

        public const string IsSubsequentSection = nameof(IsSubsequentSection);
        private static readonly ImmutableDictionary<string, string> s_subsequentSectionProperties = ImmutableDictionary<string, string>.Empty.Add(IsSubsequentSection, "");

        public CSharpRemoveUnreachableCodeDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Unreachable_code_detected), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   configurable: false)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSemanticModelAction(AnalyzeSemanticModel);

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var options = context.Options as WorkspaceAnalyzerOptions;
            if (options == null)
            {
                return;
            }

            var fadeCode = options.Services.Workspace.Options.GetOption(FadingOptions.FadeOutUnreachableCode, LanguageNames.CSharp);

            var tree = context.SemanticModel.SyntaxTree;
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;

            // There is no good existing API to check if a statement is unreachable in an efficient
            // manner.  While there is SemanticModel.AnalyzeControlFlow, it can only operate on a 
            // statement at a time, and it will reanalyze and allocate on each call.  
            //
            // To avoid this, we simply ask the semantic model for all the diagnostics for this
            // block and we look for any reported "unreachable code detected" diagnostics.
            //
            // This is actually quite fast to do because the compiler does not actually need to
            // recompile things to determine the diagnostics.  It will have already stashed the
            // binding diagnostics directly on the SourceMethodSymbol containing this block, and
            // so it can retrieve the diagnostics at practically no cost.
            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            var diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (diagnostic.Id == CS0162)
                {
                    ProcessUnreachableDiagnostic(context, root, diagnostic.Location.SourceSpan, fadeCode);
                }
            }
        }

        private void ProcessUnreachableDiagnostic(
            SemanticModelAnalysisContext context, SyntaxNode root, TextSpan sourceSpan, bool fadeOutCode)
        {
            var node = root.FindNode(sourceSpan);

            // Note: this approach works as the language only supports the concept of 
            // unreachable statements.  If we ever get unreachable subexpressions, then
            // we'll need to revise this code accordingly.
            var firstUnreachableStatement = node.FirstAncestorOrSelf<StatementSyntax>();
            if (firstUnreachableStatement == null ||
                firstUnreachableStatement.SpanStart != sourceSpan.Start)
            {
                return;
            }

            // At a high level, we can think about us wanting to fade out a "section" of unreachable
            // statements.  However, the compiler only reports the first statement in that "section".
            // We want to figure out what other statements are in that section and fade them all out
            // along with the first statement.  This is made somewhat tricky due to the fact that
            // subsequent sibling statements possibly being reachable due to explicit gotos+labels.
            //
            // On top of this, an unreachable section might not be contiguous.  This is possible 
            // when there is unreachable code that contains a local function declaration in-situ.
            // This is legal, and the local function declaration may be called from other reachable code.
            //
            // As such, it's not possible to just get first unreachable statement, and the last, and
            // then report that whole region as unreachable.  Instead, when we are told about an
            // unreachable statement, we simply determine which other statements are also unreachable
            // and bucket them into contiguous chunks. 
            //
            // We then fade each of these contiguous chunks, while also having each diagnostic we
            // report point back to the first unreachable statement so that we can easily determine
            // what to remove if the user fixes the issue.  (The fix itself has to go recompute this
            // as the total set of statements to remove may be larger than the actual faded code
            // that that diagnostic corresponds to).

            // Get the location of this first unreachable statement.  It will be given to all
            // the diagnostics we create off of this single compiler diagnostic so that we always
            // know how to find it regardless of which of our diagnostics the user invokes the 
            // fix off of.
            var firstStatementLocation = root.SyntaxTree.GetLocation(firstUnreachableStatement.FullSpan);

            if (!firstUnreachableStatement.IsParentKind(SyntaxKind.Block) &&
                !firstUnreachableStatement.IsParentKind(SyntaxKind.SwitchSection))
            {
                // Can't actually remove this statement (it's an embedded statement in something 
                // like an 'if-statement').  Just fade the code out, but don't offer to remove it.
                if (fadeOutCode)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(UnnecessaryWithoutSuggestionDescriptor, firstStatementLocation));
                }
                return;
            }

            // 'additionalLocations' is how we always pass along the locaiton of the first unreachable
            // statement in this group.
            var additionalLocations = SpecializedCollections.SingletonEnumerable(firstStatementLocation);

            var descriptor = fadeOutCode ? UnnecessaryWithSuggestionDescriptor : Descriptor;

            context.ReportDiagnostic(
                Diagnostic.Create(descriptor, firstStatementLocation, additionalLocations));

            var sections = RemoveUnreachableCodeHelpers.GetSubsequentUnreachableSections(firstUnreachableStatement);
            foreach (var section in sections)
            {
                var span = TextSpan.FromBounds(section[0].FullSpan.Start, section.Last().FullSpan.End);
                var location = root.SyntaxTree.GetLocation(span);

                // Mark subsequent sections as being 'cascaded'.  We don't need to actually process them 
                // when doing a fix-all as they'll be scooped up when we process the fix for the first
                // section.
                context.ReportDiagnostic(
                    Diagnostic.Create(descriptor, location, additionalLocations, s_subsequentSectionProperties));
            }
        }
    }
}
