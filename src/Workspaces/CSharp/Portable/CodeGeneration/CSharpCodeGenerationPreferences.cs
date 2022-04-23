// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal sealed class CSharpCodeGenerationPreferences : CodeGenerationPreferences
    {
        public readonly ExpressionBodyPreference PreferExpressionBodiedMethods;
        public readonly ExpressionBodyPreference PreferExpressionBodiedAccessors;
        public readonly ExpressionBodyPreference PreferExpressionBodiedProperties;
        public readonly ExpressionBodyPreference PreferExpressionBodiedIndexers;
        public readonly ExpressionBodyPreference PreferExpressionBodiedConstructors;
        public readonly ExpressionBodyPreference PreferExpressionBodiedOperators;
        public readonly ExpressionBodyPreference PreferExpressionBodiedLocalFunctions;
        public readonly NamespaceDeclarationPreference NamespaceDeclarations;
        public readonly AddImportPlacement PreferredUsingDirectivePlacement;
        public readonly LanguageVersion LanguageVersion;

        public CSharpCodeGenerationPreferences(
            bool placeSystemNamespaceFirst,
            ExpressionBodyPreference preferExpressionBodiedMethods,
            ExpressionBodyPreference preferExpressionBodiedAccessors,
            ExpressionBodyPreference preferExpressionBodiedProperties,
            ExpressionBodyPreference preferExpressionBodiedIndexers,
            ExpressionBodyPreference preferExpressionBodiedConstructors,
            ExpressionBodyPreference preferExpressionBodiedOperators,
            ExpressionBodyPreference preferExpressionBodiedLocalFunctions,
            NamespaceDeclarationPreference namespaceDeclarations,
            AddImportPlacement preferredUsingDirectivePlacement,
            LanguageVersion languageVersion)
            : base(placeSystemNamespaceFirst)
        {
            PreferExpressionBodiedMethods = preferExpressionBodiedMethods;
            PreferExpressionBodiedAccessors = preferExpressionBodiedAccessors;
            PreferExpressionBodiedProperties = preferExpressionBodiedProperties;
            PreferExpressionBodiedIndexers = preferExpressionBodiedIndexers;
            PreferExpressionBodiedConstructors = preferExpressionBodiedConstructors;
            PreferExpressionBodiedOperators = preferExpressionBodiedOperators;
            PreferExpressionBodiedLocalFunctions = preferExpressionBodiedLocalFunctions;
            NamespaceDeclarations = namespaceDeclarations;
            PreferredUsingDirectivePlacement = preferredUsingDirectivePlacement;
            LanguageVersion = languageVersion;
        }

        public override bool PlaceImportsInsideNamespaces
            => PreferredUsingDirectivePlacement == AddImportPlacement.InsideNamespace;

        public override CodeGenerationOptions GetOptions(CodeGenerationContext context)
            => new CSharpCodeGenerationOptions(context, this);

        public static CSharpCodeGenerationPreferences Create(CSharpParseOptions parseOptions, OptionSet documentOptions)
            => new(
                placeSystemNamespaceFirst: documentOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp),
                preferExpressionBodiedMethods: documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods).Value,
                preferExpressionBodiedAccessors: documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value,
                preferExpressionBodiedProperties: documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties).Value,
                preferExpressionBodiedIndexers: documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers).Value,
                preferExpressionBodiedConstructors: documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors).Value,
                preferExpressionBodiedOperators: documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators).Value,
                preferExpressionBodiedLocalFunctions: documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions).Value,
                namespaceDeclarations: documentOptions.GetOption(CSharpCodeStyleOptions.NamespaceDeclarations).Value,
                preferredUsingDirectivePlacement: documentOptions.GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement).Value,
                languageVersion: parseOptions.LanguageVersion);

        public static new async Task<CSharpCodeGenerationPreferences> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var parseOptions = (CSharpParseOptions?)document.Project.ParseOptions;
            Contract.ThrowIfNull(parseOptions);

            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return Create(parseOptions, documentOptions);
        }
    }
}
