using System;

namespace Roslyn.Compilers.CSharp.Descriptions
{
    internal partial class SymbolDescriptionVisitor : SymbolVisitor<ArrayBuilder<SymbolDescriptionPart>, object>
    {
        private readonly SymbolDescriptionFormat format;
        private readonly Location location;
        private readonly SyntaxBinding binding;
        private readonly IFormatProvider formatProvider;

        public SymbolDescriptionVisitor(SymbolDescriptionFormat format, Location location, SyntaxBinding binding, IFormatProvider formatProvider)
        {
            this.format = format;
            this.location = location;
            this.binding = binding;
            this.formatProvider = formatProvider;
        }

        protected internal override object VisitAssembly(AssemblySymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            var text = (format.TypeQualificationStyle == QualificationStyle.NameOnly) ?
                symbol.AssemblyName.Name :
                symbol.AssemblyName.FullName;

            builder.Add(new SymbolDescriptionPart
            {
                Kind = SymbolDescriptionPartKind.AssemblyName,
                Text = text,
            });

            return null;
        }

        protected internal override object VisitModule(ModuleSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            builder.Add(new SymbolDescriptionPart
            {
                Kind = SymbolDescriptionPartKind.ModuleName,
                Text = symbol.Name,
            });

            return null;
        }

        protected internal override object VisitNamespace(NamespaceSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            if (format.PrettyPrintingFlags.HasFlag(PrettyPrintingFlags.UseAliases))
            {
                throw new NotImplementedException(); //TODO
            }

            if (format.TypeQualificationStyle == QualificationStyle.NameAndContainingTypesAndNamespaces)
            {
                if (symbol.ContainingNamespace != null)
                {
                    VisitNamespace(symbol.ContainingNamespace, builder);
                    AddPunctuation(SyntaxKind.DotToken, builder);
                }
            }
            else if (format.TypeQualificationStyle == QualificationStyle.ShortestUnambiguous)
            {
                throw new NotImplementedException(); //TODO
            }

            //TODO: do we use the same global namespace string in all circumstances or
            //do we want a different one when it's a containing namespace
            string text = symbol.IsGlobalNamespace ?
                MessageID.IDS_GlobalNamespace.Localize().ToString(null, formatProvider) :
                symbol.Name;

            builder.Add(new SymbolDescriptionPart
            {
                Kind = SymbolDescriptionPartKind.NamespaceName,
                Text = text,
            });

            return null;
        }

        private void AddSpace(ArrayBuilder<SymbolDescriptionPart> builder)
        {
            builder.Add(new SymbolDescriptionPart
            {
                Kind = SymbolDescriptionPartKind.Space,
                Text = " ",
            });
        }

        private static void AddPunctuation(SyntaxKind punctuationKind, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            builder.Add(new SymbolDescriptionPart
            {
                Kind = SymbolDescriptionPartKind.Punctuation,
                Text = SyntaxFacts.GetText(punctuationKind),
            });
        }

        private static void AddKeyword(SyntaxKind keywordKind, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            builder.Add(new SymbolDescriptionPart
            {
                Kind = SymbolDescriptionPartKind.Keyword,
                Text = SyntaxFacts.GetText(keywordKind),
            });
        }

        private void AddAccessibilityIfRequired(Symbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            if (format.MemberFlags.HasFlag(MemberFlags.IncludeAccessibility))
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Private:
                        AddKeyword(SyntaxKind.PrivateKeyword, builder);
                        break;
                    case Accessibility.Internal:
                        AddKeyword(SyntaxKind.InternalKeyword, builder);
                        break;
                    case Accessibility.Protected:
                        AddKeyword(SyntaxKind.ProtectedKeyword, builder);
                        break;
                    case Accessibility.ProtectedInternal:
                        AddKeyword(SyntaxKind.ProtectedKeyword, builder);
                        AddSpace(builder);
                        AddKeyword(SyntaxKind.InternalKeyword, builder);
                        break;
                    case Accessibility.Public:
                        AddKeyword(SyntaxKind.PublicKeyword, builder);
                        break;
                    default:
                        throw new NotImplementedException(); //TODO
                }
                AddSpace(builder);
            }
        }
    }
}