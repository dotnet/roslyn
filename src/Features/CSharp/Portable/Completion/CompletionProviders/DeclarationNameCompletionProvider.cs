using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;
using Words = System.Collections.Generic.IEnumerable<string>;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class DeclarationNameCompletionProvider : CommonCompletionProvider
    {
        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, insertedCharacterPosition, options);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            var position = completionContext.Position;
            var document = completionContext.Document;
            var cancellationToken = completionContext.CancellationToken;
            var semanticModel = await document.GetSemanticModelForSpanAsync(new Text.TextSpan(position, 0), cancellationToken).ConfigureAwait(false);

            var context = CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);

            var targetToken = context.TargetToken;
            var nameInfo = await NameDeclarationInfo.GetDeclarationInfo(document, position, cancellationToken).ConfigureAwait(false);

            if (!IsValidType(nameInfo.Type))
            {
                return;
            }

            var type = UnwrapType(nameInfo.Type);
            var baseNames = NameGenerator.GetBaseNames(type);
            int i = 0;
            foreach (var (name, kind) in GetRecommendedNames(baseNames, nameInfo, context))
            {
                // We've produced items in the desired order, add a sort text to each item to prevent alphabetization
                completionContext.AddItem(CreateCompletionItem(name, GetGlyph(kind, nameInfo.DeclaredAccessibility), i++.ToString("D8")));
            }

            AddBuilder(completionContext);
        }

        private bool IsValidType(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.IsErrorType() && (type.Name == "var" || type.Name == string.Empty))
            {
                return false;
            }

            return type.SpecialType != SpecialType.System_Void;
        }

        private Glyph GetGlyph(SymbolKind kind, Accessibility declaredAccessibility)
        {
            Glyph publicIcon;
            switch (kind)
            {
                case SymbolKind.Field:
                    publicIcon = Glyph.FieldPublic;
                    break;
                case SymbolKind.Local:
                    publicIcon = Glyph.Local;
                    break;
                case SymbolKind.Method:
                    publicIcon = Glyph.MethodPublic;
                    break;
                case SymbolKind.Parameter:
                    publicIcon = Glyph.Parameter;
                    break;
                case SymbolKind.Property:
                    publicIcon = Glyph.PropertyPublic;
                    break;
                case SymbolKind.RangeVariable:
                    publicIcon = Glyph.RangeVariable;
                    break;
                case SymbolKind.TypeParameter:
                    publicIcon = Glyph.TypeParameter;
                    break;
                default:
                    throw new ArgumentException();
            }

            switch (declaredAccessibility)
            {
                case Accessibility.Private:
                    publicIcon += Glyph.ClassPrivate - Glyph.ClassPublic;
                    break;

                case Accessibility.Protected:
                case Accessibility.ProtectedAndInternal:
                case Accessibility.ProtectedOrInternal:
                    publicIcon += Glyph.ClassProtected - Glyph.ClassPublic;
                    break;

                case Accessibility.Internal:
                    publicIcon += Glyph.ClassInternal - Glyph.ClassPublic;
                    break;
            }

            return publicIcon;
        }

        private void AddBuilder(CompletionContext completionContext)
        {
            completionContext.SuggestionModeItem = CommonCompletionItem.Create(CSharpFeaturesResources.Name);
        }

        private ITypeSymbol UnwrapType(ITypeSymbol type)
        {
            var nts = type as INamedTypeSymbol;
            if (nts != null && nts.ConstructedFrom != null)
            {
                var constructedFrom = nts.ConstructedFrom;
                switch (constructedFrom.SpecialType)
                {
                    case SpecialType.System_Collections_Generic_IEnumerable_T:
                    case SpecialType.System_Collections_Generic_IList_T:
                    case SpecialType.System_Collections_Generic_ICollection_T:
                    case SpecialType.System_Collections_Generic_IReadOnlyList_T:
                    case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
                    case SpecialType.System_Nullable_T:
                        return UnwrapType(nts.TypeArguments[0]);
                }

                if (constructedFrom.Name == "Task"
                   && constructedFrom.ContainingNamespace?.Name == "Tasks"
                   && constructedFrom.ContainingNamespace.ContainingNamespace?.Name == "Threading"
                   && constructedFrom.ContainingNamespace.ContainingNamespace.ContainingNamespace?.Name == "System"
                   && (constructedFrom.ContainingNamespace.ContainingNamespace.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace ?? false))
                {
                    return UnwrapType(nts.TypeArguments[0]);
                }
            }

            return type;
        }

        private IEnumerable<(string, SymbolKind)> GetRecommendedNames(IEnumerable<Words> baseNames, NameDeclarationInfo declarationInfo, CSharpSyntaxContext context)
        {
            var namingStyleOptions = context.Workspace.Options.GetOption(SimplificationOptions.NamingPreferences, LanguageNames.CSharp);
            var rules = namingStyleOptions.CreateRules().NamingRules.Concat(s_BuiltInRules);
            var result = new List<(string, SymbolKind)>();
            foreach (var symbolKind in declarationInfo.PossibleSymbolKinds)
            {
                var kind = new SymbolKindOrTypeKind(symbolKind);
                var modifiers = declarationInfo.Modifiers;
                foreach (var rule in rules)
                {
                    if (rule.SymbolSpecification.AppliesTo(kind, declarationInfo.Modifiers, declarationInfo.DeclaredAccessibility))
                    {
                        foreach (var name in baseNames)
                        {
                            result.Add((rule.NamingStyle.CreateName(name), symbolKind));
                        }
                    }
                }
            }

            return result;
        }

        CompletionItem CreateCompletionItem(string name, Glyph glyph, string sortText)
        {
            return CommonCompletionItem.Create(name, glyph, sortText: sortText,
                rules: CompletionItemRules.Default);
        }
    }
}
