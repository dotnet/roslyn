// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public abstract class EditingTestBase : CSharpTestBase
    {
        public static readonly string ReloadableAttributeSrc = @"
using System.Runtime.CompilerServices;
namespace System.Runtime.CompilerServices { class CreateNewOnMetadataUpdateAttribute : Attribute {} }
";

        internal static CSharpEditAndContinueAnalyzer CreateAnalyzer()
        {
            return new CSharpEditAndContinueAnalyzer(testFaultInjector: null);
        }

        internal enum MethodKind
        {
            Regular,
            Async,
            Iterator,
            ConstructorWithParameters
        }

        public static string GetResource(string keyword, string symbolDisplayName)
            => string.Format(FeaturesResources.member_kind_and_name, TryGetResource(keyword) ?? throw ExceptionUtilities.UnexpectedValue(keyword), symbolDisplayName);

        public static string GetResource(string keyword, string symbolDisplayName, string containerKeyword, string containerDisplayName)
            => string.Format(
                FeaturesResources.symbol_kind_and_name_of_member_kind_and_name,
                TryGetResource(keyword) ?? throw ExceptionUtilities.UnexpectedValue(keyword),
                symbolDisplayName,
                TryGetResource(containerKeyword) ?? throw ExceptionUtilities.UnexpectedValue(containerKeyword),
                containerDisplayName);

        public static string GetResource(string keyword)
            => TryGetResource(keyword) ?? throw ExceptionUtilities.UnexpectedValue(keyword);

        public static string? TryGetResource(string keyword)
            => keyword switch
            {
                "enum" => FeaturesResources.enum_,
                "class" => FeaturesResources.class_,
                "interface" => FeaturesResources.interface_,
                "delegate" => FeaturesResources.delegate_,
                "struct" => CSharpFeaturesResources.struct_,
                "record" or "record class" => CSharpFeaturesResources.record_,
                "record struct" => CSharpFeaturesResources.record_struct,
                "static constructor" => FeaturesResources.static_constructor,
                "constructor" => FeaturesResources.constructor,
                "field" => FeaturesResources.field,
                "method" => FeaturesResources.method,
                "property" => FeaturesResources.property_,
                "property getter" => CSharpFeaturesResources.property_getter,
                "property setter" => CSharpFeaturesResources.property_setter,
                "auto-property" => FeaturesResources.auto_property,
                "indexer" => CSharpFeaturesResources.indexer,
                "indexer getter" => CSharpFeaturesResources.indexer_getter,
                "indexer setter" => CSharpFeaturesResources.indexer_setter,
                "parameter" => FeaturesResources.parameter,
                "type parameter" => FeaturesResources.type_parameter,
                "lambda" => CSharpFeaturesResources.lambda,
                "local function" => FeaturesResources.local_function,
                "where clause" => CSharpFeaturesResources.where_clause,
                "select clause" => CSharpFeaturesResources.select_clause,
                "groupby clause" => CSharpFeaturesResources.groupby_clause,
                "top-level statement" => CSharpFeaturesResources.top_level_statement,
                "top-level code" => CSharpFeaturesResources.top_level_code,
                _ => null
            };

        internal static SemanticEditDescription[] NoSemanticEdits = Array.Empty<SemanticEditDescription>();

        internal static RudeEditDiagnosticDescription Diagnostic(RudeEditKind rudeEditKind, string squiggle, params string[] arguments)
            => new(rudeEditKind, squiggle, arguments, firstLine: null);

        internal static SemanticEditDescription SemanticEdit(SemanticEditKind kind, Func<Compilation, ISymbol> symbolProvider, IEnumerable<KeyValuePair<TextSpan, TextSpan>>? syntaxMap, string? partialType = null)
            => new(kind, symbolProvider, (partialType != null) ? c => c.GetMember<INamedTypeSymbol>(partialType) : null, syntaxMap, hasSyntaxMap: syntaxMap != null, deletedSymbolContainerProvider: null);

        internal static SemanticEditDescription SemanticEdit(SemanticEditKind kind, Func<Compilation, ISymbol> symbolProvider, string? partialType = null, bool preserveLocalVariables = false, Func<Compilation, ISymbol>? deletedSymbolContainerProvider = null)
            => new(kind, symbolProvider, (partialType != null) ? c => c.GetMember<INamedTypeSymbol>(partialType) : null, syntaxMap: null, preserveLocalVariables, deletedSymbolContainerProvider);

        internal static string DeletedSymbolDisplay(string kind, string displayName)
            => string.Format(FeaturesResources.member_kind_and_name, kind, displayName);

        internal static DocumentAnalysisResultsDescription DocumentResults(
            ActiveStatementsDescription? activeStatements = null,
            SemanticEditDescription[]? semanticEdits = null,
            RudeEditDiagnosticDescription[]? diagnostics = null)
            => new(activeStatements, semanticEdits, lineEdits: null, diagnostics);

        internal static string GetDocumentFilePath(int documentIndex)
            => Path.Combine(TempRoot.Root, documentIndex.ToString() + ".cs");

        private static SyntaxTree ParseSource(string markedSource, int documentIndex = 0)
            => SyntaxFactory.ParseSyntaxTree(
                SourceMarkers.Clear(markedSource),
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
                path: GetDocumentFilePath(documentIndex));

        internal static EditScript<SyntaxNode> GetTopEdits(string src1, string src2, int documentIndex = 0)
        {
            var tree1 = ParseSource(src1, documentIndex);
            var tree2 = ParseSource(src2, documentIndex);

            tree1.GetDiagnostics().Verify();
            tree2.GetDiagnostics().Verify();

            var match = SyntaxComparer.TopLevel.ComputeMatch(tree1.GetRoot(), tree2.GetRoot());
            return match.GetTreeEdits();
        }

        public static EditScript<SyntaxNode> GetTopEdits(EditScript<SyntaxNode> methodEdits)
        {
            var oldMethodSource = methodEdits.Match.OldRoot.ToFullString();
            var newMethodSource = methodEdits.Match.NewRoot.ToFullString();

            return GetTopEdits(WrapMethodBodyWithClass(oldMethodSource), WrapMethodBodyWithClass(newMethodSource));
        }

        /// <summary>
        /// Gets method edits on the current level of the source hierarchy. This means that edits on lower labeled levels of the hierarchy are not expected to be returned.
        /// </summary>
        internal static EditScript<SyntaxNode> GetMethodEdits(string src1, string src2, MethodKind kind = MethodKind.Regular)
        {
            var match = GetMethodMatch(src1, src2, kind);
            return match.GetTreeEdits();
        }

        internal static Match<SyntaxNode> GetMethodMatch(string src1, string src2, MethodKind kind = MethodKind.Regular)
        {
            var m1 = MakeMethodBody(src1, kind);
            var m2 = MakeMethodBody(src2, kind);

            var match = m1.ComputeMatch(m2, knownMatches: null);

            var stateMachineInfo1 = m1.GetStateMachineInfo();
            var stateMachineInfo2 = m2.GetStateMachineInfo();
            var needsSyntaxMap = stateMachineInfo1.HasSuspensionPoints && stateMachineInfo2.HasSuspensionPoints;

            Assert.Equal(kind is not MethodKind.Regular and not MethodKind.ConstructorWithParameters, needsSyntaxMap);

            return match;
        }

        internal static IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> GetMethodMatches(string src1, string src2, MethodKind kind = MethodKind.Regular)
        {
            var methodMatch = GetMethodMatch(src1, src2, kind);
            return EditAndContinueTestHelpers.GetMethodMatches(CreateAnalyzer(), methodMatch);
        }

        public static MatchingPairs ToMatchingPairs(Match<SyntaxNode> match)
            => EditAndContinueTestHelpers.ToMatchingPairs(match);

        public static MatchingPairs ToMatchingPairs(IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> matches)
            => EditAndContinueTestHelpers.ToMatchingPairs(matches);

        internal static MemberBody MakeMethodBody(
            string bodySource,
            MethodKind kind = MethodKind.Regular)
        {
            var source = WrapMethodBodyWithClass(bodySource, kind);

            var tree = ParseSource(source);
            var root = tree.GetRoot();

            tree.GetDiagnostics().Verify();

            var declaration = (BaseMethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)root).Members[0]).Members[0];

            if (kind == MethodKind.ConstructorWithParameters)
            {
                var body = SyntaxUtilities.TryGetDeclarationBody(declaration);
                Contract.ThrowIfNull(body);
                return body;
            }

            // We need to preserve the parent node to allow detection of state machine methods in the analyzer.
            // If we are not testing a state machine method we only use the body to avoid updating positions in all existing tests.
            var bodyNode = (kind != MethodKind.Regular)
                ? ((BaseMethodDeclarationSyntax)SyntaxFactory.SyntaxTree(declaration).GetRoot()).Body
                : (BlockSyntax)SyntaxFactory.SyntaxTree(declaration.Body!).GetRoot();

            return SyntaxUtilities.CreateSimpleBody(bodyNode)!;
        }

        internal static string WrapMethodBodyWithClass(string bodySource, MethodKind kind = MethodKind.Regular)
             => kind switch
             {
                 MethodKind.Iterator => "class C { IEnumerable<int> F() { " + bodySource + " } }",
                 MethodKind.Async => "class C { async Task<int> F() { " + bodySource + " } }",
                 MethodKind.ConstructorWithParameters => "class C { C" + bodySource + " }",
                 _ => "class C { void F() { " + bodySource + " } }",
             };

        internal static ActiveStatementsDescription GetActiveStatements(string oldSource, string newSource, ActiveStatementFlags[]? flags = null, int documentIndex = 0)
            => new(oldSource, newSource, source => SyntaxFactory.ParseSyntaxTree(source, path: GetDocumentFilePath(documentIndex)), flags);

        internal static SyntaxMapDescription GetSyntaxMap(string oldSource, string newSource)
            => new(oldSource, newSource);

        internal static void VerifyPreserveLocalVariables(EditScript<SyntaxNode> edits, bool preserveLocalVariables)
        {
            var oldDeclaration = (MethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)edits.Match.OldRoot).Members[0]).Members[0];
            var oldBody = SyntaxUtilities.TryGetDeclarationBody(oldDeclaration);
            Contract.ThrowIfNull(oldBody);

            var newDeclaration = (MethodDeclarationSyntax)((ClassDeclarationSyntax)((CompilationUnitSyntax)edits.Match.NewRoot).Members[0]).Members[0];
            var newBody = SyntaxUtilities.TryGetDeclarationBody(newDeclaration);
            Contract.ThrowIfNull(newBody);

            _ = oldBody.ComputeMatch(newBody, knownMatches: null);

            var oldStateMachineInfo = oldBody.GetStateMachineInfo();
            var newStateMachineInfo = newBody.GetStateMachineInfo();
            var needsSyntaxMap = oldStateMachineInfo.HasSuspensionPoints && newStateMachineInfo.HasSuspensionPoints;

            // Active methods are detected to preserve local variables for variable mapping and
            // edited async/iterator methods are considered active.
            Assert.Equal(preserveLocalVariables, needsSyntaxMap);
        }
    }
}
