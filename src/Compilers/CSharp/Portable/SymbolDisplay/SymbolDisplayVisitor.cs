// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SymbolDisplay;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SymbolDisplayVisitor : AbstractSymbolDisplayVisitor
    {
        private static readonly ObjectPool<SymbolDisplayVisitor> s_visitorPool = new ObjectPool<SymbolDisplayVisitor>(pool => new SymbolDisplayVisitor(pool), 128);

        private readonly ObjectPool<SymbolDisplayVisitor> _pool;

        private bool _escapeKeywordIdentifiers;
        private IDictionary<INamespaceOrTypeSymbol, IAliasSymbol>? _lazyAliasMap;

        private SymbolDisplayVisitor(ObjectPool<SymbolDisplayVisitor> pool)
        {
            _pool = pool;
        }

        public static SymbolDisplayVisitor GetInstance(
            ArrayBuilder<SymbolDisplayPart> builder,
            SymbolDisplayFormat format,
            SemanticModel? semanticModelOpt,
            int positionOpt)
        {
            var instance = s_visitorPool.Allocate();
            instance.Initialize(builder, format, isFirstSymbolVisited: true, semanticModelOpt, positionOpt, inNamespaceOrType: false);
            return instance;
        }

        private static SymbolDisplayVisitor GetInstance(
            ArrayBuilder<SymbolDisplayPart> builder,
            SymbolDisplayFormat format,
            SemanticModel? semanticModelOpt,
            int positionOpt,
            bool escapeKeywordIdentifiers,
            IDictionary<INamespaceOrTypeSymbol, IAliasSymbol>? aliasMap,
            bool isFirstSymbolVisited,
            bool inNamespaceOrType = false)
        {
            var instance = s_visitorPool.Allocate();
            instance.Initialize(builder, format, isFirstSymbolVisited, semanticModelOpt, positionOpt, inNamespaceOrType);
            instance._escapeKeywordIdentifiers = escapeKeywordIdentifiers;
            instance._lazyAliasMap = aliasMap;
            return instance;
        }

        protected new void Initialize(ArrayBuilder<SymbolDisplayPart> builder, SymbolDisplayFormat format, bool isFirstSymbolVisited, SemanticModel? semanticModelOpt, int positionOpt, bool inNamespaceOrType)
        {
            base.Initialize(builder, format, isFirstSymbolVisited, semanticModelOpt, positionOpt, inNamespaceOrType);

            _escapeKeywordIdentifiers = format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);
        }

        public override void Free()
        {
            base.Free();

            _escapeKeywordIdentifiers = false;
            _lazyAliasMap = null;

            _pool.Free(this);
        }

        protected override AbstractSymbolDisplayVisitor MakeNotFirstVisitor(bool inNamespaceOrType = false)
        {
            return GetInstance(
                this.Builder,
                this.Format,
                this.SemanticModelOpt,
                this.PositionOpt,
                _escapeKeywordIdentifiers,
                _lazyAliasMap,
                isFirstSymbolVisited: false,
                inNamespaceOrType: inNamespaceOrType);
        }

        protected override void FreeNotFirstVisitor(AbstractSymbolDisplayVisitor visitor)
        {
            Debug.Assert(visitor != this);
            visitor.Free();
        }

        internal SymbolDisplayPart CreatePart(SymbolDisplayPartKind kind, ISymbol? symbol, string text)
        {
            text = (text == null) ? "?" :
                   (_escapeKeywordIdentifiers && IsEscapable(kind)) ? EscapeIdentifier(text) : text;

            return new SymbolDisplayPart(kind, symbol, text);
        }

        private static bool IsEscapable(SymbolDisplayPartKind kind)
        {
            switch (kind)
            {
                case SymbolDisplayPartKind.AliasName:
                case SymbolDisplayPartKind.ClassName:
                case SymbolDisplayPartKind.RecordClassName:
                case SymbolDisplayPartKind.StructName:
                case SymbolDisplayPartKind.RecordStructName:
                case SymbolDisplayPartKind.InterfaceName:
                case SymbolDisplayPartKind.EnumName:
                case SymbolDisplayPartKind.DelegateName:
                case SymbolDisplayPartKind.TypeParameterName:
                case SymbolDisplayPartKind.MethodName:
                case SymbolDisplayPartKind.PropertyName:
                case SymbolDisplayPartKind.FieldName:
                case SymbolDisplayPartKind.LocalName:
                case SymbolDisplayPartKind.NamespaceName:
                case SymbolDisplayPartKind.ParameterName:
                    return true;
                default:
                    return false;
            }
        }

        private static string EscapeIdentifier(string identifier)
        {
            var kind = SyntaxFacts.GetKeywordKind(identifier);
            return kind == SyntaxKind.None && !StringComparer.Ordinal.Equals(identifier, "record")
                ? identifier
                : $"@{identifier}";
        }

        public override void VisitAssembly(IAssemblySymbol symbol)
        {
            var text = Format.TypeQualificationStyle == SymbolDisplayTypeQualificationStyle.NameOnly
                ? symbol.Identity.Name
                : symbol.Identity.GetDisplayName();

            Builder.Add(CreatePart(SymbolDisplayPartKind.AssemblyName, symbol, text));
        }

        public override void VisitModule(IModuleSymbol symbol)
        {
            Builder.Add(CreatePart(SymbolDisplayPartKind.ModuleName, symbol, symbol.Name));
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            if (this.IsMinimizing)
            {
                if (TryAddAlias(symbol, Builder))
                {
                    return;
                }

                MinimallyQualify(symbol);
                return;
            }

            if (IsFirstSymbolVisited && Format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeNamespaceKeyword))
            {
                AddKeyword(SyntaxKind.NamespaceKeyword);
                AddSpace();
            }

            if (Format.TypeQualificationStyle == SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)
            {
                var containingNamespace = symbol.ContainingNamespace;
                if (ShouldVisitNamespace(containingNamespace))
                {
                    containingNamespace.Accept(this.NotFirstVisitor);
                    AddPunctuation(containingNamespace.IsGlobalNamespace ? SyntaxKind.ColonColonToken : SyntaxKind.DotToken);
                }
            }

            if (symbol.IsGlobalNamespace)
            {
                AddGlobalNamespace(symbol);
            }
            else
            {
                Builder.Add(CreatePart(SymbolDisplayPartKind.NamespaceName, symbol, symbol.Name));
            }
        }

        private void AddGlobalNamespace(INamespaceSymbol globalNamespace)
        {
            // Formerly, localized via MessageID.IDS_GlobalNamespace.
            const string standaloneGlobalNamespaceString = "<global namespace>";

            switch (Format.GlobalNamespaceStyle)
            {
                case SymbolDisplayGlobalNamespaceStyle.Omitted:
                    break;
                case SymbolDisplayGlobalNamespaceStyle.Included:
                    if (this.IsFirstSymbolVisited)
                    {
                        Builder.Add(CreatePart(
                            SymbolDisplayPartKind.Text,
                            globalNamespace,
                            standaloneGlobalNamespaceString));
                    }
                    else
                    {
                        Builder.Add(CreatePart(SymbolDisplayPartKind.Keyword, globalNamespace,
                            SyntaxFacts.GetText(SyntaxKind.GlobalKeyword)));
                    }
                    break;
                case SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining:
                    Debug.Assert(this.IsFirstSymbolVisited, "Don't call with IsFirstSymbolVisited = false if OmittedAsContaining");
                    Builder.Add(CreatePart(
                        SymbolDisplayPartKind.Text,
                        globalNamespace,
                        standaloneGlobalNamespaceString));
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(Format.GlobalNamespaceStyle);
            }
        }

        public override void VisitLocal(ILocalSymbol symbol)
        {
            if (Format.LocalOptions.IncludesOption(SymbolDisplayLocalOptions.IncludeModifiers))
            {
                if (symbol.IsRef)
                {
                    if (symbol.ScopedKind == ScopedKind.ScopedRef)
                    {
                        AddKeyword(SyntaxKind.ScopedKeyword);
                        AddSpace();
                    }

                    AddKeyword(SyntaxKind.RefKeyword);
                    AddSpace();

                    if (symbol.RefKind == RefKind.RefReadOnly)
                    {
                        AddKeyword(SyntaxKind.ReadOnlyKeyword);
                        AddSpace();
                    }
                }
                else if (symbol.ScopedKind == ScopedKind.ScopedValue)
                {
                    AddKeyword(SyntaxKind.ScopedKeyword);
                    AddSpace();
                }
            }

            if (Format.LocalOptions.IncludesOption(SymbolDisplayLocalOptions.IncludeType))
            {
                symbol.Type.Accept(this.NotFirstVisitor);
                AddSpace();
            }

            if (symbol.IsConst)
            {
                Builder.Add(CreatePart(SymbolDisplayPartKind.ConstantName, symbol, symbol.Name));
            }
            else
            {
                Builder.Add(CreatePart(SymbolDisplayPartKind.LocalName, symbol, symbol.Name));
            }

            if (Format.LocalOptions.IncludesOption(SymbolDisplayLocalOptions.IncludeConstantValue) &&
                symbol.IsConst &&
                symbol.HasConstantValue &&
                CanAddConstant(symbol.Type, symbol.ConstantValue))
            {
                AddSpace();
                AddPunctuation(SyntaxKind.EqualsToken);
                AddSpace();

                AddConstantValue(symbol.Type, symbol.ConstantValue);
            }
        }

        public override void VisitDiscard(IDiscardSymbol symbol)
        {
            if (Format.LocalOptions.IncludesOption(SymbolDisplayLocalOptions.IncludeType))
            {
                symbol.Type.Accept(this.NotFirstVisitor);
                AddSpace();
            }

            Builder.Add(CreatePart(SymbolDisplayPartKind.Punctuation, symbol, "_"));
        }

        public override void VisitRangeVariable(IRangeVariableSymbol symbol)
        {
            if (Format.LocalOptions.IncludesOption(SymbolDisplayLocalOptions.IncludeType))
            {
                ITypeSymbol? type = GetRangeVariableType(symbol);

                if (type != null && type.TypeKind != TypeKind.Error)
                {
                    type.Accept(this);
                }
                else
                {
                    Builder.Add(CreatePart(SymbolDisplayPartKind.ErrorTypeName, type, "?"));
                }

                AddSpace();
            }

            Builder.Add(CreatePart(SymbolDisplayPartKind.RangeVariableName, symbol, symbol.Name));
        }

        public override void VisitLabel(ILabelSymbol symbol)
        {
            Builder.Add(CreatePart(SymbolDisplayPartKind.LabelName, symbol, symbol.Name));
        }

        public override void VisitAlias(IAliasSymbol symbol)
        {
            Builder.Add(CreatePart(SymbolDisplayPartKind.AliasName, symbol, symbol.Name));

            if (Format.LocalOptions.IncludesOption(SymbolDisplayLocalOptions.IncludeType))
            {
                // ???
                AddPunctuation(SyntaxKind.EqualsToken);
                symbol.Target.Accept(this);
            }
        }

        protected override void AddSpace()
        {
            Builder.Add(CreatePart(SymbolDisplayPartKind.Space, null, " "));
        }

        private void AddPunctuation(SyntaxKind punctuationKind)
        {
            Builder.Add(CreatePart(SymbolDisplayPartKind.Punctuation, null, SyntaxFacts.GetText(punctuationKind)));
        }

        private void AddKeyword(SyntaxKind keywordKind)
        {
            Builder.Add(CreatePart(SymbolDisplayPartKind.Keyword, null, SyntaxFacts.GetText(keywordKind)));
        }

        private void AddAccessibilityIfNeeded(ISymbol symbol)
        {
            INamedTypeSymbol containingType = symbol.ContainingType;

            // this method is only called for members and they should have a containingType or a containing symbol should be a TypeSymbol.
            Debug.Assert((object)containingType != null || (symbol.ContainingSymbol is ITypeSymbol));

            if (Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeAccessibility) &&
                (containingType == null ||
                 (containingType.TypeKind != TypeKind.Interface && !IsEnumMember(symbol) & !IsLocalFunction(symbol))))
            {
                AddAccessibility(symbol);
            }
        }

        private static bool IsLocalFunction(ISymbol symbol)
        {
            if (symbol.Kind != SymbolKind.Method)
            {
                return false;
            }

            return ((IMethodSymbol)symbol).MethodKind == MethodKind.LocalFunction;
        }

        private void AddAccessibility(ISymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Private:
                    AddKeyword(SyntaxKind.PrivateKeyword);
                    break;
                case Accessibility.Internal:
                    AddKeyword(SyntaxKind.InternalKeyword);
                    break;
                case Accessibility.ProtectedAndInternal:
                    AddKeyword(SyntaxKind.PrivateKeyword);
                    AddSpace();
                    AddKeyword(SyntaxKind.ProtectedKeyword);
                    break;
                case Accessibility.Protected:
                    AddKeyword(SyntaxKind.ProtectedKeyword);
                    break;
                case Accessibility.ProtectedOrInternal:
                    AddKeyword(SyntaxKind.ProtectedKeyword);
                    AddSpace();
                    AddKeyword(SyntaxKind.InternalKeyword);
                    break;
                case Accessibility.Public:
                    AddKeyword(SyntaxKind.PublicKeyword);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.DeclaredAccessibility);
            }

            AddSpace();
        }

        private bool ShouldVisitNamespace(ISymbol containingSymbol)
        {
            var namespaceSymbol = containingSymbol as INamespaceSymbol;
            if (namespaceSymbol == null)
            {
                return false;
            }

            if (Format.TypeQualificationStyle != SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)
            {
                return false;
            }

            return
                !namespaceSymbol.IsGlobalNamespace ||
                Format.GlobalNamespaceStyle == SymbolDisplayGlobalNamespaceStyle.Included;
        }

        private bool IncludeNamedType([NotNullWhen(true)] INamedTypeSymbol? namedType)
        {
            if (namedType is null)
            {
                return false;
            }

            if (namedType.IsScriptClass && !Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeScriptType))
            {
                return false;
            }

            if (namedType == SemanticModelOpt?.Compilation.ScriptGlobalsType)
            {
                return false;
            }

            return true;
        }

        private static bool IsEnumMember(ISymbol symbol)
        {
            return symbol != null
                && symbol.Kind == SymbolKind.Field
                && symbol.ContainingType != null
                && symbol.ContainingType.TypeKind == TypeKind.Enum
                && symbol.Name != WellKnownMemberNames.EnumBackingFieldName;
        }
    }
}
