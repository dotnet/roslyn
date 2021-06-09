// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(ExplicitInterfaceMemberCompletionProvider), LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = nameof(UnnamedSymbolCompletionProvider))]
    internal partial class ExplicitInterfaceMemberCompletionProvider : LSPCompletionProvider
    {
        private static readonly SymbolDisplayFormat s_signatureDisplayFormat =
            new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExplicitInterfaceMemberCompletionProvider()
        {
        }

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => text[characterPosition] == '.';

        public override ImmutableHashSet<char> TriggerCharacters { get; } = ImmutableHashSet.Create('.');

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                var document = context.Document;
                var position = context.Position;
                var cancellationToken = context.CancellationToken;

                var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

                if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken) ||
                    syntaxFacts.IsPreProcessorDirectiveContext(syntaxTree, position, cancellationToken))
                {
                    return;
                }

                var targetToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                            .GetPreviousTokenIfTouchingWord(position);

                if (!syntaxTree.IsRightOfDotOrArrowOrColonColon(position, targetToken, cancellationToken))
                    return;

                var node = targetToken.Parent;
                if (!node.IsKind(SyntaxKind.ExplicitInterfaceSpecifier, out ExplicitInterfaceSpecifierSyntax? specifierNode))
                    return;

                // Bind the interface name which is to the left of the dot
                var name = specifierNode.Name;

                var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
                var symbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol as ITypeSymbol;
                if (symbol?.TypeKind != TypeKind.Interface)
                    return;

                // We're going to create a entry for each one, including the signature
                var namePosition = name.SpanStart;
                foreach (var member in symbol.GetMembers())
                {
                    if (!member.IsAbstract && !member.IsVirtual)
                        continue;

                    if (member.IsAccessor() ||
                        member.Kind == SymbolKind.NamedType ||
                        !semanticModel.IsAccessible(node.SpanStart, member))
                    {
                        continue;
                    }

                    //var memberString = member.ToMinimalDisplayString(semanticModel, namePosition, s_signatureDisplayFormat);
                    var memberString = member switch
                    {
                        IEventSymbol eventSymbol => ToDisplayString(eventSymbol),
                        IPropertySymbol propertySymbol => ToDisplayString(propertySymbol),
                        IMethodSymbol methodSymbol => ToDisplayString(methodSymbol),
                        _ => string.Empty // This should be unexpected.
                    };

                    // Split the member string into two parts (generally the name, and the signature portion). We want
                    // the split so that other features (like spell-checking), only look at the name portion.
                    var (displayText, displayTextSuffix) = SplitMemberName(memberString);

                    context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                        displayText,
                        displayTextSuffix,
                        insertionText: memberString,
                        symbols: ImmutableArray.Create<ISymbol>(member),
                        contextPosition: position,
                        rules: CompletionItemRules.Default));
                }
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                // nop
            }
        }

        #region DisplayBuilder
        private static string ToDisplayString(IEventSymbol symbol)
            => symbol.Name;

        private static string ToDisplayString(IPropertySymbol symbol)
        {
            var builder = new StringBuilder();
            if (symbol.IsIndexer)
            {
                builder.Append("this");
            }
            else
            {
                builder.Append(symbol.Name);
            }

            if (symbol.Parameters.Length > 0)
            {
                builder.Append('[');

                var first = true;
                foreach (var parameter in symbol.Parameters)
                {
                    if (!first)
                    {
                        builder.Append(", ");
                    }

                    first = false;
                    builder.Append(ToDisplayString(parameter));
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        private static string ToDisplayString(IMethodSymbol symbol)
        {
            var builder = new StringBuilder();
            switch (symbol.MethodKind)
            {
                case MethodKind.Ordinary:
                    builder.Append(symbol.Name);
                    break;
                case MethodKind.UserDefinedOperator:
                case MethodKind.BuiltinOperator:
                    {
                        builder.Append("operator ");
                        builder.Append(SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(symbol.MetadataName)));
                    }
                    break;
                case MethodKind.Conversion:
                    {

                        builder.Append("operator ");
                        AddReturnType(symbol, builder);
                        break;
                    }
            }

            AddTypeArguments(symbol, builder);
            // Parameters is tricky part for operators.
            AddParameters(symbol);
            return builder.ToString();
        }

        private static void AddParameters(IMethodSymbol symbol)
        {
            throw new NotImplementedException();
        }

        private static void AddTypeArguments(IMethodSymbol symbol, StringBuilder builder)
        {
            if (symbol.TypeArguments.Length > 0)
            {
                builder.Append('<');

                var first = true;
                foreach (var typeArgument in symbol.TypeArguments)
                {
                    if (!first)
                    {
                        builder.Append(", ");
                    }

                    first = false;
                    // TODO: Make sure there are no edge cases:
                    builder.Append(typeArgument.Name);

                }

                builder.Append('>');
            }
        }

        private static void AddReturnType(IMethodSymbol symbol, StringBuilder builder)
        {
            // TODO: Should respect user settings regarding BCL types vs C# aliases (int vs Int32, etc.)
            // I doubt this should be something to rewrite completely from scratch.
            //builder.Append(symbol.ReturnType.??);
        }

        private static string ToDisplayString(IParameterSymbol symbol)
            => symbol.Name;

        #endregion

        private static (string text, string suffix) SplitMemberName(string memberString)
        {
            for (var i = 0; i < memberString.Length; i++)
            {
                if (!SyntaxFacts.IsIdentifierPartCharacter(memberString[i]))
                    return (memberString[0..i], memberString[i..]);
            }

            return (memberString, "");
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        public override Task<TextChange?> GetTextChangeAsync(
            Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            // If the user is typing a punctuation portion of the signature, then just emit the name.  i.e. if the
            // member is `Contains<T>(string key)`, then typing `<` should just emit `Contains` and not
            // `Contains<T>(string key)<`
            return Task.FromResult<TextChange?>(new TextChange(
                selectedItem.Span,
                ch == '(' || ch == '[' || ch == '<'
                    ? selectedItem.DisplayText
                    : SymbolCompletionItem.GetInsertionText(selectedItem)));
        }
    }
}
