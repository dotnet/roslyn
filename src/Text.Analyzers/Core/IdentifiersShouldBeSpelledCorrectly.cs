// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Text.Analyzers
{
    /// <summary>
    /// CA1704: Identifiers should be spelled correctly
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class IdentifiersShouldBeSpelledCorrectlyAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1704";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyTitle), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));

        private static readonly LocalizableString s_localizableMessageFileParse = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyFileParse), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageAssembly = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageAssembly), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageNamespace = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageNamespace), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageType = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageType), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageVariable = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageVariable), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageMember = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageMember), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageMemberParameter = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageMemberParameter), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageDelegateParameter = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageDelegateParameter), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageTypeTypeParameter = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageTypeTypeParameter), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageMethodTypeParameter = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageMethodTypeParameter), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageAssemblyMoreMeaningfulName = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageAssemblyMoreMeaningfulName), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageNamespaceMoreMeaningfulName = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageNamespaceMoreMeaningfulName), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageTypeMoreMeaningfulName = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageTypeMoreMeaningfulName), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageMemberMoreMeaningfulName = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageMemberMoreMeaningfulName), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageMemberParameterMoreMeaningfulName = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageMemberParameterMoreMeaningfulName), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageDelegateParameterMoreMeaningfulName = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageDelegateParameterMoreMeaningfulName), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageTypeTypeParameterMoreMeaningfulName = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageTypeTypeParameterMoreMeaningfulName), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageMethodTypeParameterMoreMeaningfulName = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyMessageMethodTypeParameterMoreMeaningfulName), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(TextAnalyzersResources.IdentifiersShouldBeSpelledCorrectlyDescription), TextAnalyzersResources.ResourceManager, typeof(TextAnalyzersResources));

        private static readonly SourceTextValueProvider<CodeAnalysisDictionary> s_xmlDictionaryProvider = new SourceTextValueProvider<CodeAnalysisDictionary>(ParseXmlDictionary);
        private static readonly SourceTextValueProvider<CodeAnalysisDictionary> s_dicDictionaryProvider = new SourceTextValueProvider<CodeAnalysisDictionary>(ParseDicDictionary);
        private static readonly CodeAnalysisDictionary s_mainDictionary = GetMainDictionary();

        internal static DiagnosticDescriptor FileParseRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageFileParse,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor AssemblyRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageAssembly,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor NamespaceRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageNamespace,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor TypeRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageType,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor VariableRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageVariable,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor MemberRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageMember,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor MemberParameterRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageMemberParameter,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor DelegateParameterRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageDelegateParameter,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor TypeTypeParameterRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageTypeTypeParameter,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor MethodTypeParameterRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageMethodTypeParameter,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor AssemblyMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageAssemblyMoreMeaningfulName,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor NamespaceMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageNamespaceMoreMeaningfulName,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor TypeMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageTypeMoreMeaningfulName,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor MemberMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageMemberMoreMeaningfulName,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor MemberParameterMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageMemberParameterMoreMeaningfulName,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor DelegateParameterMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageDelegateParameterMoreMeaningfulName,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor TypeTypeParameterMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageTypeTypeParameterMoreMeaningfulName,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        internal static DiagnosticDescriptor MethodTypeParameterMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            s_localizableMessageMethodTypeParameterMoreMeaningfulName,
            DiagnosticCategory.Naming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            customTags: FxCopWellKnownDiagnosticTags.PortedFxCopRule);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            FileParseRule,
            AssemblyRule,
            NamespaceRule,
            TypeRule,
            VariableRule,
            MemberRule,
            MemberParameterRule,
            DelegateParameterRule,
            TypeTypeParameterRule,
            MethodTypeParameterRule,
            AssemblyMoreMeaningfulNameRule,
            NamespaceMoreMeaningfulNameRule,
            TypeMoreMeaningfulNameRule,
            MemberMoreMeaningfulNameRule,
            MemberParameterMoreMeaningfulNameRule,
            DelegateParameterMoreMeaningfulNameRule,
            TypeTypeParameterMoreMeaningfulNameRule,
            MethodTypeParameterMoreMeaningfulNameRule);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecution();
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            analysisContext.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext compilationStartContext)
        {
            var dictionaries = ReadDictionaries();
            var projectDictionary = CodeAnalysisDictionary.CreateFromDictionaries(dictionaries.Concat(s_mainDictionary));

            compilationStartContext.RegisterOperationAction(AnalyzeVariable, OperationKind.VariableDeclarator);
            compilationStartContext.RegisterCompilationEndAction(AnalyzeAssembly);
            compilationStartContext.RegisterSymbolAction(
                AnalyzeSymbol,
                SymbolKind.Namespace,
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Event,
                SymbolKind.Field,
                SymbolKind.Parameter);

            IEnumerable<CodeAnalysisDictionary> ReadDictionaries()
            {
                var fileProvider = AdditionalFileProvider.FromOptions(compilationStartContext.Options);
                return fileProvider.GetMatchingFiles(@"(?:dictionary|custom).*?\.(?:xml|dic)$")
                    .Select(CreateDictionaryFromAdditionalText)
                    .Where(x => x != null)
                    .ToList();

                CodeAnalysisDictionary CreateDictionaryFromAdditionalText(AdditionalText additionalFile)
                {
                    var text = additionalFile.GetText(compilationStartContext.CancellationToken);
                    var isXml = additionalFile.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
                    var provider = isXml ? s_xmlDictionaryProvider : s_dicDictionaryProvider;

                    if (!compilationStartContext.TryGetValue(text, provider, out var dictionary))
                    {
                        try
                        {
                            // Annoyingly (and expectedly), TryGetValue swallows the parsing exception,
                            // so we have to parse again to get it.
                            var unused = isXml ? ParseXmlDictionary(text) : ParseDicDictionary(text);
                            ReportFileParseDiagnostic(additionalFile.Path, "Unknown error");
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                        {
                            ReportFileParseDiagnostic(additionalFile.Path, ex.Message);
                        }
                    }

                    return dictionary;
                }

                void ReportFileParseDiagnostic(string filePath, string message)
                {
                    var diagnostic = Diagnostic.Create(FileParseRule, Location.None, filePath, message);
                    compilationStartContext.RegisterCompilationEndAction(x => x.ReportDiagnostic(diagnostic));
                }
            }

            void AnalyzeVariable(OperationAnalysisContext operationContext)
            {
                var variableOperation = (IVariableDeclaratorOperation)operationContext.Operation;
                var variable = variableOperation.Symbol;

                ReportDiagnosticsForSymbol(variable, variable.Name, operationContext.ReportDiagnostic, checkForUnmeaningful: false);
            }

            void AnalyzeAssembly(CompilationAnalysisContext context)
            {
                var assembly = context.Compilation.Assembly;

                ReportDiagnosticsForSymbol(assembly, assembly.Name, context.ReportDiagnostic);
            }

            void AnalyzeSymbol(SymbolAnalysisContext symbolContext)
            {
                var typeParameterDiagnostics = Enumerable.Empty<Diagnostic>();

                ISymbol symbol = symbolContext.Symbol;
                if (symbol.IsOverride)
                {
                    return;
                }

                var symbolName = symbol.Name;
                switch (symbol)
                {
                    case IFieldSymbol:
                        symbolName = RemovePrefixIfPresent('_', symbolName);
                        break;

                    case IMethodSymbol method:
                        switch (method.MethodKind)
                        {
                            case MethodKind.PropertyGet:
                            case MethodKind.PropertySet:
                                return;

                            case MethodKind.Constructor:
                            case MethodKind.StaticConstructor:
                                symbolName = symbol.ContainingType.Name;
                                break;
                        }

                        foreach (var typeParameter in method.TypeParameters)
                        {
                            ReportDiagnosticsForSymbol(typeParameter, RemovePrefixIfPresent('T', typeParameter.Name), symbolContext.ReportDiagnostic);
                        }

                        break;

                    case INamedTypeSymbol type:
                        if (type.TypeKind == TypeKind.Interface)
                        {
                            symbolName = RemovePrefixIfPresent('I', symbolName);
                        }

                        foreach (var typeParameter in type.TypeParameters)
                        {
                            ReportDiagnosticsForSymbol(typeParameter, RemovePrefixIfPresent('T', typeParameter.Name), symbolContext.ReportDiagnostic);
                        }

                        break;
                }

                ReportDiagnosticsForSymbol(symbol, symbolName, symbolContext.ReportDiagnostic);
            }

            void ReportDiagnosticsForSymbol(ISymbol symbol, string symbolName, Action<Diagnostic> reportDiagnostic, bool checkForUnmeaningful = true)
            {
                foreach (var misspelledWord in GetMisspelledWords(symbolName))
                {
                    reportDiagnostic(GetMisspelledWordDiagnostic(symbol, misspelledWord));
                }

                if (checkForUnmeaningful && symbolName.Length == 1)
                {
                    reportDiagnostic(GetUnmeaningfulIdentifierDiagnostic(symbol, symbolName));
                }
            }

            IEnumerable<string> GetMisspelledWords(string symbolName)
            {
                var parser = new WordParser(symbolName, WordParserOptions.SplitCompoundWords);
                if (parser.PeekWord() != null)
                {
                    var word = parser.NextWord();
                    if (word == null)
                    {
                        yield break;
                    }

                    do
                    {
                        if (IsWordAcronym(word) || IsWordNumeric(word) || IsWordSpelledCorrectly(word))
                        {
                            continue;
                        }

                        yield return word;
                    }
                    while ((word = parser.NextWord()) != null);
                }
            }

            static bool IsWordAcronym(string word) => word.All(char.IsUpper);

            static bool IsWordNumeric(string word) => char.IsDigit(word[0]);

            bool IsWordSpelledCorrectly(string word)
                => !projectDictionary.UnrecognizedWords.Contains(word) && projectDictionary.RecognizedWords.Contains(word);
        }

        private static CodeAnalysisDictionary GetMainDictionary()
        {
            // The "main" dictionary, Dictionary.dic, was created in WSL Ubuntu with the following commands:
            //
            // Install dependencies:
            // > sudo apt install hunspell-tools hunspell-en-us
            // 
            // Create dictionary:
            // > unmunch /usr/share/hunspell/en_US.dic /usr/share/hunspell/en_US.aff > Dictionary.dic
            //
            // Tweak:
            // Added the words: 'namespace'
            var text = SourceText.From(TextAnalyzersResources.Dictionary);
            return ParseDicDictionary(text);
        }

        private static CodeAnalysisDictionary ParseXmlDictionary(SourceText text)
            => text.Parse(CodeAnalysisDictionary.CreateFromXml);

        private static CodeAnalysisDictionary ParseDicDictionary(SourceText text)
            => text.Parse(CodeAnalysisDictionary.CreateFromDic);

        private static string RemovePrefixIfPresent(char prefix, string name)
            => name.Length > 0 && name[0] == prefix ? name[1..] : name;

        private static Diagnostic GetMisspelledWordDiagnostic(ISymbol symbol, string misspelledWord)
        {
            return symbol.Kind switch
            {
                SymbolKind.Assembly => symbol.CreateDiagnostic(AssemblyRule, misspelledWord, symbol.Name),
                SymbolKind.Namespace => symbol.CreateDiagnostic(NamespaceRule, misspelledWord, symbol.ToDisplayString()),
                SymbolKind.NamedType => symbol.CreateDiagnostic(TypeRule, misspelledWord, symbol.ToDisplayString()),
                SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field
                    => symbol.CreateDiagnostic(MemberRule, misspelledWord, symbol.ToDisplayString()),
                SymbolKind.Parameter => symbol.ContainingType.TypeKind == TypeKind.Delegate
                    ? symbol.CreateDiagnostic(DelegateParameterRule, symbol.ContainingType.ToDisplayString(), misspelledWord, symbol.Name)
                    : symbol.CreateDiagnostic(MemberParameterRule, symbol.ContainingSymbol.ToDisplayString(), misspelledWord, symbol.Name),
                SymbolKind.TypeParameter => symbol.ContainingSymbol.Kind == SymbolKind.Method
                    ? symbol.CreateDiagnostic(MethodTypeParameterRule, symbol.ContainingSymbol.ToDisplayString(), misspelledWord, symbol.Name)
                    : symbol.CreateDiagnostic(TypeTypeParameterRule, symbol.ContainingSymbol.ToDisplayString(), misspelledWord, symbol.Name),
                SymbolKind.Local => symbol.CreateDiagnostic(VariableRule, misspelledWord, symbol.ToDisplayString()),
                _ => throw new NotImplementedException($"Unknown SymbolKind: {symbol.Kind}"),
            };
        }

        private static Diagnostic GetUnmeaningfulIdentifierDiagnostic(ISymbol symbol, string symbolName)
        {
            return symbol.Kind switch
            {
                SymbolKind.Assembly => symbol.CreateDiagnostic(AssemblyMoreMeaningfulNameRule, symbolName),
                SymbolKind.Namespace => symbol.CreateDiagnostic(NamespaceMoreMeaningfulNameRule, symbolName),
                SymbolKind.NamedType => symbol.CreateDiagnostic(TypeMoreMeaningfulNameRule, symbolName),
                SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field
                    => symbol.CreateDiagnostic(MemberMoreMeaningfulNameRule, symbolName),
                SymbolKind.Parameter => symbol.ContainingType.TypeKind == TypeKind.Delegate
                    ? symbol.CreateDiagnostic(DelegateParameterMoreMeaningfulNameRule, symbol.ContainingType.ToDisplayString(), symbolName)
                    : symbol.CreateDiagnostic(MemberParameterMoreMeaningfulNameRule, symbol.ContainingSymbol.ToDisplayString(), symbolName),
                SymbolKind.TypeParameter => symbol.ContainingSymbol.Kind == SymbolKind.Method
                    ? symbol.CreateDiagnostic(MethodTypeParameterMoreMeaningfulNameRule, symbol.ContainingSymbol.ToDisplayString(), symbol.Name)
                    : symbol.CreateDiagnostic(TypeTypeParameterMoreMeaningfulNameRule, symbol.ContainingSymbol.ToDisplayString(), symbol.Name),
                _ => throw new NotImplementedException($"Unknown SymbolKind: {symbol.Kind}"),
            };
        }
    }
}