// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Completion;
using System;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.ErrorReporting;
using System.Linq;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

internal sealed partial class ExplicitInterfaceMemberCompletionProvider
{
    private sealed class ItemGetter : AbstractItemGetter<ExplicitInterfaceMemberCompletionProvider>
    {
        private ItemGetter(
            ExplicitInterfaceMemberCompletionProvider overrideCompletionProvider,
            Document document,
            int position,
            SourceText text,
            SyntaxTree syntaxTree,
            int startLineNumber,
            CancellationToken cancellationToken)
            : base(
                  overrideCompletionProvider,
                  document,
                  position,
                  text,
                  syntaxTree,
                  startLineNumber,
                  cancellationToken)
        {
        }

        public static async Task<ItemGetter> CreateAsync(
            ExplicitInterfaceMemberCompletionProvider overrideCompletionProvider,
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var startLineNumber = text.Lines.IndexOf(position);
            return new ItemGetter(overrideCompletionProvider, document, position, text, syntaxTree, startLineNumber, cancellationToken);
        }

        public override async Task<ImmutableArray<CompletionItem>> GetItemsAsync()
        {
            // modifiers* type? Interface(<typeparams+>)?.|
            try
            {
                var syntaxFacts = Document.GetRequiredLanguageService<ISyntaxFactsService>();
                var semanticFacts = Document.GetRequiredLanguageService<ISemanticFactsService>();

                if (syntaxFacts.IsInNonUserCode(SyntaxTree, Position, CancellationToken) ||
                    syntaxFacts.IsPreProcessorDirectiveContext(SyntaxTree, Position, CancellationToken))
                {
                    return [];
                }

                var targetToken = SyntaxTree
                    .FindTokenOnLeftOfPosition(Position, CancellationToken)
                    .GetPreviousTokenIfTouchingWord(Position);

                if (!SyntaxTree.IsRightOfDotOrArrowOrColonColon(Position, targetToken, CancellationToken))
                    return [];

                var node = targetToken.Parent;
                // Bind the interface name which is to the left of the dot
                NameSyntax? name = null;
                switch (node)
                {
                    case ExplicitInterfaceSpecifierSyntax specifierNode:
                        name = specifierNode.Name;
                        break;

                    case QualifiedNameSyntax qualifiedName
                    when node.Parent.IsKind(SyntaxKind.IncompleteMember):
                        name = qualifiedName.Left;
                        break;

                    default:
                        return [];
                }

                var semanticModel = await Document.ReuseExistingSpeculativeModelAsync(Position, CancellationToken).ConfigureAwait(false);
                var symbol = semanticModel.GetSymbolInfo(name, CancellationToken).Symbol as ITypeSymbol;
                if (symbol?.TypeKind != TypeKind.Interface)
                    return [];

                var typeDeclaration = node.GetAncestor<BaseTypeDeclarationSyntax>();
                if (typeDeclaration is null)
                    return [];

                var containingType = semanticModel.GetDeclaredSymbol(typeDeclaration, CancellationToken);
                if (containingType is null)
                    return [];

                // We must be explicitly implementing the interface
                if (!containingType.Interfaces.Contains(symbol))
                    return [];

                // We're going to create a entry for each one, including the signature
                var namePosition = name.SpanStart;
                var text = await Document.GetValueTextAsync(CancellationToken).ConfigureAwait(false);
                text.GetLineAndOffset(namePosition, out var line, out var lineOffset);
                var items = symbol.GetMembers()
                    .Where(FilterInterfaceMember)
                    .SelectAsArray(CreateCompletionItem);

                return items;

                bool FilterInterfaceMember(ISymbol member)
                {
                    if (!member.IsAbstract && !member.IsVirtual)
                        return false;

                    if (member.IsAccessor() ||
                        member.Kind == SymbolKind.NamedType ||
                        !semanticModel.IsAccessible(node.SpanStart, member))
                    {
                        return false;
                    }

                    return true;
                }

                CompletionItem CreateCompletionItem(ISymbol member)
                {
                    var memberString = ToDisplayString(member, semanticModel);

                    // Split the member string into two parts (generally the name, and the signature portion). We want
                    // the split so that other features (like spell-checking), only look at the name portion.
                    var (displayText, displayTextSuffix) = SplitMemberName(memberString);

                    return MemberInsertionCompletionItem.Create(
                        displayText, displayTextSuffix, DeclarationModifiers.None, line,
                        member, targetToken, Position,
                        rules: GetRules());
                }
            }
            catch (Exception e)
            when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
            {
                // nop
                return [];
            }
        }

        private string ToDisplayString(ISymbol symbol, SemanticModel semanticModel)
            => symbol switch
            {
                IEventSymbol eventSymbol => ToDisplayString(eventSymbol),
                IPropertySymbol propertySymbol => ToDisplayString(propertySymbol, semanticModel),
                IMethodSymbol methodSymbol => ToDisplayString(methodSymbol, semanticModel),
                _ => "" // This shouldn't happen.
            };

        private static string ToDisplayString(IEventSymbol symbol)
            => symbol.Name;

        private static SyntaxToken FindStartingToken(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            return token.GetPreviousTokenIfTouchingWord(position);
        }

        private string ToDisplayString(IPropertySymbol symbol, SemanticModel semanticModel)
        {
            using var _ = PooledStringBuilder.GetInstance(out var builder);
            var startToken = FindStartingToken(SyntaxTree, Position, CancellationToken);

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
                AddParameters(symbol.Parameters, builder, semanticModel);
                builder.Append(']');
            }

            return builder.ToString();
        }

        private string ToDisplayString(IMethodSymbol symbol, SemanticModel semanticModel)
        {
            using var _ = PooledStringBuilder.GetInstance(out var builder);
            switch (symbol.MethodKind)
            {
                case MethodKind.Ordinary:
                    builder.Append(symbol.Name);
                    break;
                case MethodKind.UserDefinedOperator:
                case MethodKind.BuiltinOperator:
                    AppendOperatorKeywords(symbol, builder);
                    builder.Append(SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(symbol.MetadataName)));
                    break;
                case MethodKind.Conversion:
                    AppendOperatorKeywords(symbol, builder);
                    AddType(symbol.ReturnType, builder, semanticModel);
                    break;
            }

            AddTypeArguments(symbol, builder);
            builder.Append('(');
            AddParameters(symbol.Parameters, builder, semanticModel);
            builder.Append(')');
            return builder.ToString();
        }

        private static void AppendOperatorKeywords(IMethodSymbol symbol, StringBuilder builder)
        {
            builder.Append("operator ");
            if (SyntaxFacts.IsCheckedOperator(symbol.MetadataName))
            {
                builder.Append("checked ");
            }
        }

        private void AddParameters(ImmutableArray<IParameterSymbol> parameters, StringBuilder builder, SemanticModel semanticModel)
        {
            builder.AppendJoinedValues(", ", parameters, (parameter, builder) =>
            {
                builder.Append(parameter.RefKind switch
                {
                    RefKind.Out => "out ",
                    RefKind.Ref => "ref ",
                    RefKind.In => "in ",
                    _ => ""
                });

                if (parameter.IsParams)
                {
                    builder.Append("params ");
                }

                AddType(parameter.Type, builder, semanticModel);
                builder.Append($" {parameter.Name.EscapeIdentifier()}");
            });
        }

        private static void AddTypeArguments(IMethodSymbol symbol, StringBuilder builder)
        {
            if (symbol.TypeArguments.Length <= 0)
            {
                return;
            }

            builder.Append('<');
            builder.AppendJoinedValues(", ", symbol.TypeArguments, static (symbol, builder) => builder.Append(symbol.Name.EscapeIdentifier()));
            builder.Append('>');
        }

        private void AddType(ITypeSymbol symbol, StringBuilder builder, SemanticModel semanticModel)
        {
            builder.Append(symbol.ToMinimalDisplayString(semanticModel, Position));
        }
    }
}
