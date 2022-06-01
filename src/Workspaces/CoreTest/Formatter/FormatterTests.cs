// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.VisualBasic.Formatting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Formating;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Formatting)]
public class FormatterTests
{
    private static readonly TestComposition s_composition = FeaturesTestCompositions.Features;

    [ExportLanguageService(typeof(IFormattingService), language: NoCompilationConstants.LanguageName), Shared, PartNotDiscoverable]
    internal class TestFormattingService : IFormattingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestFormattingService()
        {
        }

        public Task<Document> FormatAsync(Document document, IEnumerable<TextSpan>? spans, LineFormattingOptions lineFormattingOptions, SyntaxFormattingOptions? syntaxFormattingOptions, CancellationToken cancellationToken)
            => Task.FromResult(document.WithText(SourceText.From($"Formatted with options: {lineFormattingOptions.ToString().Replace("\r", "\\r").Replace("\n", "\\n")}")));
    }

    [Export(typeof(IDocumentOptionsProviderFactory)), Shared, PartNotDiscoverable]
    internal class TestDocumentOptionsProviderFactory : IDocumentOptionsProviderFactory
    {
        public readonly Dictionary<OptionKey, object> Options = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestDocumentOptionsProviderFactory()
        {
        }

        public IDocumentOptionsProvider? TryCreate(Workspace workspace)
            => new Provider(new DocumentOptions(Options));

        private class Provider : IDocumentOptionsProvider
        {
            private readonly DocumentOptions _options;

            public Provider(DocumentOptions options)
                => _options = options;

            public Task<IDocumentOptions?> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
                => Task.FromResult<IDocumentOptions?>(_options);
        }

        public class DocumentOptions : IDocumentOptions
        {
            private readonly Dictionary<OptionKey, object> _options;

            public DocumentOptions(Dictionary<OptionKey, object> options)
                => _options = options;

            public bool TryGetDocumentOption(OptionKey option, out object? value)
                => _options.TryGetValue(option, out value);
        }
    }

    [Fact]
    public async Task FormatAsync_ForeignLanguageWithFormattingSupport()
    {
        var hostServices = s_composition.AddParts(new[] { typeof(NoCompilationLanguageServiceFactory), typeof(TestFormattingService) }).GetHostServices();
        using var workspace = new AdhocWorkspace(hostServices);

        var project = workspace.AddProject("Dummy", NoCompilationConstants.LanguageName);
        var document = workspace.AddDocument(project.Id, "File.dummy", SourceText.From("dummy"));

#pragma warning disable RS0030 // Do not used banned APIs
        var formattedDocument = await Formatter.FormatAsync(document, spans: null, options: null, CancellationToken.None);
#pragma warning restore RS0030 // Do not used banned APIs

        var formattedText = await formattedDocument.GetTextAsync();
        AssertEx.Equal(@"Formatted with options: LineFormattingOptions { UseTabs = False, TabSize = 4, IndentationSize = 4, NewLine = \r\n }", formattedText.ToString());
    }

    [Theory]
    [CombinatorialData]
    public async Task FormatAsync_ForeignLanguageWithFormattingSupport_Options(bool passExplicitOptions)
    {
        var hostServices = s_composition.AddParts(new[] { typeof(NoCompilationLanguageServiceFactory), typeof(TestFormattingService), typeof(TestDocumentOptionsProviderFactory) }).GetHostServices();

        using var workspace = new AdhocWorkspace(hostServices);

        // register custom document options provider (Razor scenario)
        var documentOptionsFactory = (TestDocumentOptionsProviderFactory)((IMefHostExportProvider)hostServices).GetExportedValue<IDocumentOptionsProviderFactory>();
        documentOptionsFactory.Options.Add(new OptionKey(FormattingOptions.IndentationSize, NoCompilationConstants.LanguageName), 10);
        var provider = documentOptionsFactory.TryCreate(workspace)!;
        workspace.Services.GetRequiredService<IOptionService>().RegisterDocumentOptionsProvider(provider);

        var project = workspace.AddProject("Dummy", NoCompilationConstants.LanguageName);
        var document = workspace.AddDocument(project.Id, "File.dummy", SourceText.From("dummy"));

        var solutionOptions = workspace.CurrentSolution.Options.
            WithChangedOption(new OptionKey(FormattingOptions.UseTabs, NoCompilationConstants.LanguageName), false).
            WithChangedOption(new OptionKey(FormattingOptions.TabSize, NoCompilationConstants.LanguageName), 1).
            WithChangedOption(new OptionKey(FormattingOptions.IndentationSize, NoCompilationConstants.LanguageName), 7).
            WithChangedOption(new OptionKey(FormattingOptions.NewLine, NoCompilationConstants.LanguageName), "\n");

        document = document.Project.Solution.WithOptions(solutionOptions).GetRequiredDocument(document.Id);

        var documentOptions = await document.GetOptionsAsync();
        Assert.Equal(10, documentOptions.GetOption(FormattingOptions.IndentationSize));

        var options = passExplicitOptions ? new OptionValueSet(ImmutableDictionary<OptionKey, object?>.Empty.
            Add(new OptionKey(FormattingOptions.UseTabs, NoCompilationConstants.LanguageName), true).
            Add(new OptionKey(FormattingOptions.TabSize, NoCompilationConstants.LanguageName), 5).
            Add(new OptionKey(FormattingOptions.IndentationSize, NoCompilationConstants.LanguageName), 6).
            Add(new OptionKey(FormattingOptions.NewLine, NoCompilationConstants.LanguageName), "\r")) : null;

#pragma warning disable RS0030 // Do not used banned APIs
        var formattedDocument = await Formatter.FormatAsync(document, spans: null, options, CancellationToken.None);
#pragma warning restore RS0030 // Do not used banned APIs

        var formattedText = await formattedDocument.GetTextAsync();

        if (passExplicitOptions)
        {
            // explicit options override solution and document options:
            AssertEx.Equal(@"Formatted with options: LineFormattingOptions { UseTabs = True, TabSize = 5, IndentationSize = 6, NewLine = \r }", formattedText.ToString());
        }
        else
        {
            // document options override solution options:
            AssertEx.Equal(@"Formatted with options: LineFormattingOptions { UseTabs = False, TabSize = 1, IndentationSize = 10, NewLine = \n }", formattedText.ToString());
        }
    }

    [Fact]
    public async Task PublicOptions()
    {
        using var workspace = new AdhocWorkspace();
        var csProject = workspace.AddProject("CS", LanguageNames.CSharp);
        var vbProject = workspace.AddProject("VB", LanguageNames.VisualBasic);
        var csDocument = workspace.AddDocument(csProject.Id, "File.cs", SourceText.From("class C { }"));
        var vbDocument = workspace.AddDocument(vbProject.Id, "File.vb", SourceText.From("Class C : End Class"));

        var updatedOptions = GetOptionSetWithChangedPublicOptions(workspace.CurrentSolution.Options);

        // Validate that options are read from specified OptionSet:

        ValidateCSharpOptions((CSharpSyntaxFormattingOptions)(await Formatter.GetOptionsAsync(csDocument, updatedOptions, CancellationToken.None)).Syntax!);
        ValidateVisualBasicOptions((VisualBasicSyntaxFormattingOptions)(await Formatter.GetOptionsAsync(vbDocument, updatedOptions, CancellationToken.None)).Syntax!);

        // Validate that options are read from solution snapshot as a fallback (we have no editorconfig file, so all options should fall back):

        var solutionWithUpdatedOptions = workspace.CurrentSolution.WithOptions(updatedOptions);
        var csDocumentWithUpdatedOptions = solutionWithUpdatedOptions.GetRequiredDocument(csDocument.Id);
        var vbDocumentWithUpdatedOptions = solutionWithUpdatedOptions.GetRequiredDocument(vbDocument.Id);

        ValidateCSharpOptions((CSharpSyntaxFormattingOptions)(await Formatter.GetOptionsAsync(csDocumentWithUpdatedOptions, optionSet: null, CancellationToken.None)).Syntax!);
        ValidateVisualBasicOptions((VisualBasicSyntaxFormattingOptions)(await Formatter.GetOptionsAsync(vbDocumentWithUpdatedOptions, optionSet: null, CancellationToken.None)).Syntax!);

        static OptionSet GetOptionSetWithChangedPublicOptions(OptionSet options)
        {
            // all public options and their non-default values:

            var publicOptions = new (IOption, object)[]
            {
                (FormattingOptions.UseTabs, true),
                (FormattingOptions.TabSize, 5),
                (FormattingOptions.IndentationSize, 7),
                (FormattingOptions.NewLine, "\r"),
                (CSharpFormattingOptions.IndentBlock, false),
                (CSharpFormattingOptions.IndentBraces, true),
                (CSharpFormattingOptions.IndentSwitchCaseSection, false),
                (CSharpFormattingOptions.IndentSwitchCaseSectionWhenBlock, false),
                (CSharpFormattingOptions.IndentSwitchSection, false),
                (CSharpFormattingOptions.LabelPositioning, LabelPositionOptions.LeftMost),
                (CSharpFormattingOptions.NewLineForCatch, false),
                (CSharpFormattingOptions.NewLineForClausesInQuery, false),
                (CSharpFormattingOptions.NewLineForElse, false),
                (CSharpFormattingOptions.NewLineForFinally, false),
                (CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, false),
                (CSharpFormattingOptions.NewLineForMembersInObjectInit, false),
                (CSharpFormattingOptions.NewLinesForBracesInAccessors, false),
                (CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, false),
                (CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, false),
                (CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false),
                (CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, false),
                (CSharpFormattingOptions.NewLinesForBracesInMethods, false),
                (CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false),
                (CSharpFormattingOptions.NewLinesForBracesInProperties, false),
                (CSharpFormattingOptions.NewLinesForBracesInTypes, false),
                (CSharpFormattingOptions.SpaceAfterCast, true),
                (CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, false),
                (CSharpFormattingOptions.SpaceAfterComma, false),
                (CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, false),
                (CSharpFormattingOptions.SpaceAfterDot, true),
                (CSharpFormattingOptions.SpaceAfterMethodCallName, true),
                (CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, false),
                (CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, false),
                (CSharpFormattingOptions.SpaceBeforeComma, true),
                (CSharpFormattingOptions.SpaceBeforeDot, true),
                (CSharpFormattingOptions.SpaceBeforeOpenSquareBracket, true),
                (CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement, true),
                (CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, true),
                (CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses, true),
                (CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, true),
                (CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, true),
                (CSharpFormattingOptions.SpaceWithinCastParentheses, true),
                (CSharpFormattingOptions.SpaceWithinExpressionParentheses, true),
                (CSharpFormattingOptions.SpaceWithinMethodCallParentheses, true),
                (CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis, true),
                (CSharpFormattingOptions.SpaceWithinOtherParentheses, true),
                (CSharpFormattingOptions.SpaceWithinSquareBrackets, true),
                (CSharpFormattingOptions.SpacingAfterMethodDeclarationName, true),
                (CSharpFormattingOptions.SpacingAroundBinaryOperator, BinaryOperatorSpacingOptions.Remove),
                (CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false),
                (CSharpFormattingOptions.WrappingPreserveSingleLine, false),
            };

            var updatedOptions = options;
            foreach (var (option, newValue) in publicOptions)
            {
                var languages = (option is IPerLanguageOption) ? new[] { LanguageNames.CSharp, LanguageNames.VisualBasic } : new string?[] { null };

                foreach (var language in languages)
                {
                    updatedOptions = updatedOptions.WithChangedOption(new OptionKey(option, language), newValue);
                }
            }

            return updatedOptions;
        }

        static void ValidateCommonOptions(SyntaxFormattingOptions formattingOptions)
        {
            Assert.True(formattingOptions.UseTabs);
            Assert.Equal(5, formattingOptions.TabSize);
            Assert.Equal(7, formattingOptions.IndentationSize);
            Assert.Equal("\r", formattingOptions.NewLine);
        }

        static void ValidateCSharpOptions(CSharpSyntaxFormattingOptions formattingOptions)
        {
            ValidateCommonOptions(formattingOptions);

            Assert.False(formattingOptions.Spacing.HasFlag(SpacePlacement.AfterControlFlowStatementKeyword));
            Assert.False(formattingOptions.Spacing.HasFlag(SpacePlacement.AfterSemicolonsInForStatement));
            Assert.False(formattingOptions.Spacing.HasFlag(SpacePlacement.AfterColonInBaseTypeDeclaration));
            Assert.False(formattingOptions.Spacing.HasFlag(SpacePlacement.BeforeColonInBaseTypeDeclaration));
            Assert.False(formattingOptions.Spacing.HasFlag(SpacePlacement.AfterComma));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.IgnoreAroundVariableDeclaration));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.AfterMethodDeclarationName));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.BetweenEmptyMethodDeclarationParentheses));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.AfterMethodCallName));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.BetweenEmptyMethodCallParentheses));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.WithinMethodCallParentheses));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.WithinExpressionParentheses));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.WithinCastParentheses));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.BeforeSemicolonsInForStatement));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.WithinOtherParentheses));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.AfterCast));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.BeforeOpenSquareBracket));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.BetweenEmptySquareBrackets));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.WithinSquareBrackets));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.BeforeComma));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.AfterDot));
            Assert.True(formattingOptions.Spacing.HasFlag(SpacePlacement.BeforeDot));

            Assert.Equal(BinaryOperatorSpacingOptions.Remove, formattingOptions.SpacingAroundBinaryOperator);

            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeMembersInObjectInitializers));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeMembersInAnonymousTypes));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeElse));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeCatch));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeFinally));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousTypes));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInProperties));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAccessors));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousMethods));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInControlBlocks));
            Assert.False(formattingOptions.NewLines.HasFlag(NewLinePlacement.BetweenQueryExpressionClauses));

            Assert.Equal(LabelPositionOptions.LeftMost, formattingOptions.LabelPositioning);

            Assert.True(formattingOptions.Indentation.HasFlag(IndentationPlacement.Braces));
            Assert.False(formattingOptions.Indentation.HasFlag(IndentationPlacement.BlockContents));
            Assert.False(formattingOptions.Indentation.HasFlag(IndentationPlacement.SwitchCaseContents));
            Assert.False(formattingOptions.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock));
            Assert.False(formattingOptions.Indentation.HasFlag(IndentationPlacement.SwitchSection));

            Assert.False(formattingOptions.WrappingPreserveSingleLine);
            Assert.False(formattingOptions.WrappingKeepStatementsOnSingleLine);
        }

        static void ValidateVisualBasicOptions(VisualBasicSyntaxFormattingOptions simplifierOptions)
        {
            ValidateCommonOptions(simplifierOptions);
        }
    }
}
