// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseNamedArguments
{
    internal abstract class AbstractUseNamedArgumentsDiagnosticAnalyzer<TSyntaxKind>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        public const string IncludeTrailingArgumentsKey = nameof(IncludeTrailingArgumentsKey);
        public const string ArgumentNameKey = nameof(ArgumentNameKey);

        protected interface IAnalyzer
        {
            void AnalyzeArgument(AbstractUseNamedArgumentsDiagnosticAnalyzer<TSyntaxKind> analyzer, SyntaxNodeAnalysisContext context);
        }

        protected abstract class Analyzer<TBaseArgumentSyntax, TSimpleArgumentSyntax, TArgumentListSyntax> : IAnalyzer
            where TBaseArgumentSyntax : SyntaxNode
            where TSimpleArgumentSyntax : TBaseArgumentSyntax
            where TArgumentListSyntax : SyntaxNode
        {
            public void AnalyzeArgument(AbstractUseNamedArgumentsDiagnosticAnalyzer<TSyntaxKind> analyzer, SyntaxNodeAnalysisContext context)
            {
                var cancellationToken = context.CancellationToken;
                var argument = context.Node as TSimpleArgumentSyntax;
                if (argument == null)
                {
                    return;
                }

                if (!IsPositionalArgument(argument))
                {
                    return;
                }

                var receiver = GetReceiver(argument);
                if (receiver == null)
                {
                    return;
                }

                if (receiver.ContainsDiagnostics)
                {
                    return;
                }

                var semanticModel = context.SemanticModel;

                var symbol = semanticModel.GetSymbolInfo(receiver, cancellationToken).Symbol;
                if (symbol == null)
                {
                    return;
                }

                var parameters = symbol.GetParameters();
                if (parameters.IsDefaultOrEmpty)
                {
                    return;
                }

                var argumentList = argument.Parent as TArgumentListSyntax;
                if (argumentList == null)
                {
                    return;
                }

                var arguments = GetArguments(argumentList);
                var argumentCount = arguments.Count;
                var argumentIndex = arguments.IndexOf(argument);
                if (argumentIndex >= parameters.Length)
                {
                    return;
                }

                if (!IsLegalToAddNamedArguments(parameters, argumentCount))
                {
                    return;
                }

                for (var i = argumentIndex; i < argumentCount; i++)
                {
                    if (!(arguments[i] is TSimpleArgumentSyntax))
                    {
                        return;
                    }
                }

                var syntaxTree = semanticModel.SyntaxTree;
                var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
                if (optionSet == null)
                {
                    return;
                }

                var option = optionSet.GetOption(CodeStyleOptions.RequireNamedArguments, semanticModel.Language);

                var severity = GetSeverity();

                var argumentName = parameters[argumentIndex].Name;
                var diagnosticLocation = GetDiagnosticLocation();
                var additionalLocations = ImmutableArray.Create(argument.GetLocation());

                var properties = ImmutableDictionary<string, string>.Empty.Add(ArgumentNameKey, argumentName);

                if (this.SupportsNonTrailingNamedArguments(syntaxTree.Options) &&
                    argumentIndex < argumentCount - 1)
                {
                    properties = properties.Add(IncludeTrailingArgumentsKey, "");
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(analyzer.CreateDescriptorWithSeverity(severity), diagnosticLocation, additionalLocations, properties));

                return;

                // Local functions

                DiagnosticSeverity GetSeverity()
                {
                    switch (option.Value)
                    {
                        default:
                        case NamedArgumentsRequired.Never: return DiagnosticSeverity.Hidden;
                        case NamedArgumentsRequired.Always: return option.Notification.Value;
                        case NamedArgumentsRequired.ForLiterals:
                            var syntaxFacts = GetSyntaxFactsService();
                            var isLiteral = syntaxFacts.IsLiteralExpression(syntaxFacts.GetExpressionOfArgument(argument));
                            return isLiteral ? option.Notification.Value : DiagnosticSeverity.Hidden;
                    }
                }

                Location GetDiagnosticLocation()
                {
                    if (severity != DiagnosticSeverity.Hidden)
                    {
                        // We're showing some sort of UI affordance (i.e a squiggle or tickler),
                        // just place it on the first token of hte argument so that the span is
                        // not too large
                        return argument.GetFirstToken().GetLocation();
                    }

                    // We're showing a hidden diagnostic.  This is trickier.  We want to make 
                    // the fix available anywhere on the argument as long as:
                    // 
                    // a) The location is on the same line as the argument.
                    // b) The location is before any other argument.  i.e. we don't want to have
                    //      Goo(Bar(1)) and offer to add named args for both the inner and outer
                    //      arguments when on the '1'.
                    var descendentArgumentList = argument.DescendantNodes().OfType<TArgumentListSyntax>().FirstOrDefault();
                    var span = descendentArgumentList == null
                        ? argument.Span
                        : TextSpan.FromBounds(argument.SpanStart, descendentArgumentList.SpanStart);

                    var sourceText = syntaxTree.GetText(cancellationToken);
                    var startLine = sourceText.Lines.GetLineFromPosition(span.Start);
                    var endLine = sourceText.Lines.GetLineFromPosition(span.End);

                    if (startLine.LineNumber == endLine.LineNumber)
                    {
                        return syntaxTree.GetLocation(span);
                    }

                    // Span cross multiple lines.  Cut it off at the end of the first line.
                    return syntaxTree.GetLocation(TextSpan.FromBounds(span.Start, startLine.End));
                }
            }

            private Task<Document> AddNamedArgumentsAsync(
                SyntaxNode root,
                Document document,
                TSimpleArgumentSyntax firstArgument,
                ImmutableArray<IParameterSymbol> parameters,
                int index,
                bool includingTrailingArguments)
            {
                var argumentList = (TArgumentListSyntax)firstArgument.Parent;
                var newArgumentList = GetOrSynthesizeNamedArguments(parameters, argumentList, index, includingTrailingArguments);
                var newRoot = root.ReplaceNode(argumentList, newArgumentList);
                return Task.FromResult(document.WithSyntaxRoot(newRoot));
            }

            private TArgumentListSyntax GetOrSynthesizeNamedArguments(
                ImmutableArray<IParameterSymbol> parameters, TArgumentListSyntax argumentList,
                int index, bool includingTrailingArguments)
            {
                var arguments = GetArguments(argumentList);
                var namedArguments = arguments
                    .Select((argument, i) => ShouldAddName(argument, i)
                        ? WithName((TSimpleArgumentSyntax)argument, parameters[i].Name).WithTriviaFrom(argument)
                        : argument);

                return WithArguments(argumentList, namedArguments, arguments.GetSeparators());

                // local functions

                bool ShouldAddName(TBaseArgumentSyntax argument, int currentIndex)
                {
                    if (currentIndex > index && !includingTrailingArguments)
                    {
                        return false;
                    }

                    return currentIndex >= index && argument is TSimpleArgumentSyntax s && IsPositionalArgument(s);
                }
            }

            protected abstract TArgumentListSyntax WithArguments(
                TArgumentListSyntax argumentList, IEnumerable<TBaseArgumentSyntax> namedArguments, IEnumerable<SyntaxToken> separators);

            protected abstract ISyntaxFactsService GetSyntaxFactsService();
            protected abstract bool IsLegalToAddNamedArguments(ImmutableArray<IParameterSymbol> parameters, int argumentCount);
            protected abstract TSimpleArgumentSyntax WithName(TSimpleArgumentSyntax argument, string name);
            protected abstract bool IsPositionalArgument(TSimpleArgumentSyntax argument);
            protected abstract SeparatedSyntaxList<TBaseArgumentSyntax> GetArguments(TArgumentListSyntax argumentList);
            protected abstract SyntaxNode GetReceiver(SyntaxNode argument);
            protected abstract bool SupportsNonTrailingNamedArguments(ParseOptions options);
        }

        private readonly IAnalyzer _argumentAnalyzer;
        private readonly IAnalyzer _attributeArgumentAnalyzer;

        protected AbstractUseNamedArgumentsDiagnosticAnalyzer(
            IAnalyzer argumentAnalyzer,
            IAnalyzer attributeArgumentAnalyzer)
            : base(IDEDiagnosticIds.UseNamedArgumentId,
                   new LocalizableResourceString(nameof(FeaturesResources.Prefer_explicitly_provided_argument_name), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _argumentAnalyzer = argumentAnalyzer;
            _attributeArgumentAnalyzer = attributeArgumentAnalyzer;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeArgument, GetArgumentSyntaxKinds());

        protected abstract ImmutableArray<TSyntaxKind> GetArgumentSyntaxKinds();

        private void AnalyzeArgument(SyntaxNodeAnalysisContext context)
        {
            _argumentAnalyzer.AnalyzeArgument(this, context);
            _attributeArgumentAnalyzer?.AnalyzeArgument(this, context);
        }
    }

    internal class AbstractUseNamedArgumentCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseNamedArgumentId);

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => diagnostic.Severity != DiagnosticSeverity.Hidden;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var supportsNonTrailingArguments = diagnostic.Properties.ContainsKey(AbstractUseNamedArgumentsDiagnosticAnalyzer<int>.IncludeTrailingArgumentsKey);
            var argumentName = diagnostic.Properties[AbstractUseNamedArgumentsDiagnosticAnalyzer<int>.ArgumentNameKey];

            if (supportsNonTrailingArguments)
            {
                context.RegisterCodeFix(new MyCodeAction(
                    string.Format(FeaturesResources.Add_argument_name_0, argumentName),
                    c => FixAsync(context.Document, diagnostic, includeTrailingArguments: false, cancellationToken: c)), context.Diagnostics);

                context.RegisterCodeFix(new MyCodeAction(
                    string.Format(FeaturesResources.Add_argument_name_0_including_trailing_arguments, argumentName),
                    c => FixAsync(context.Document, diagnostic, includeTrailingArguments: true, cancellationToken: c)), context.Diagnostics);
            }
            else
            {
                context.RegisterCodeFix(new MyCodeAction(
                    string.Format(FeaturesResources.Add_argument_name_0, argumentName),
                    c => FixAsync(context.Document, diagnostic, includeTrailingArguments: true, cancellationToken: c)), context.Diagnostics);
            }

            return SpecializedTasks.EmptyTask;
            //    context.RegisterRefactoring(
            //        new MyCodeAction(
            //            string.Format(FeaturesResources.Add_argument_name_0_including_trailing_arguments, argumentName),
            //            c => AddNamedArgumentsAsync(root, document, argument, parameters, argumentIndex, includingTrailingArguments: true)));
            //}
            //else
            //{
            //    context.RegisterRefactoring(
            //        new MyCodeAction(
            //            string.Format(FeaturesResources.Add_argument_name_0, argumentName),
            //            c => AddNamedArgumentsAsync(root, document, argument, parameters, argumentIndex, includingTrailingArguments: true)));
            //}
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            return FixAllAsync(document, )
        }

        private Task<Document> FixAsync(
            Document document, Diagnostic diagnostic, 
            bool includeTrailingArguments, CancellationToken cancellationToken)
        {
            return FixAllWithEditorAsync(document,
                editor => FixAllAsync(document, ImmutableArray.Create(diagnostic), editor, includeTrailingArguments, cancellationToken),
                cancellationToken)
        }

        private Task FixAllAsync(Document document, object diagnostics, SyntaxEditor editor, bool includeTrailingArguments, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
