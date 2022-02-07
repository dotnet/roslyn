// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal sealed class CSharpCodeGenerationPreferences : CodeGenerationPreferences
    {
        public readonly LanguageVersion LanguageVersion;

        public CSharpCodeGenerationPreferences(CSharpParseOptions parseOptions, OptionSet options)
            : this(parseOptions.LanguageVersion, options)
        {
        }

        public CSharpCodeGenerationPreferences(LanguageVersion languageVersion, OptionSet options)
            : base(options)
        {
            LanguageVersion = languageVersion;
        }

        public ExpressionBodyPreference PreferExpressionBodiedMethods
            => Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods).Value;

        public ExpressionBodyPreference PreferExpressionBodiedAccessors
            => Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value;

        public ExpressionBodyPreference PreferExpressionBodiedProperties
            => Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties).Value;

        public ExpressionBodyPreference PreferExpressionBodiedIndexers
            => Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers).Value;

        public ExpressionBodyPreference PreferExpressionBodiedConstructors
            => Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors).Value;

        public ExpressionBodyPreference PreferExpressionBodiedOperators
            => Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators).Value;

        public ExpressionBodyPreference PreferExpressionBodiedLocalFunctions
            => Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions).Value;

        public NamespaceDeclarationPreference NamespaceDeclarations
            => Options.GetOption(CSharpCodeStyleOptions.NamespaceDeclarations).Value;

        public AddImportPlacement PreferredUsingDirectivePlacement
            => Options.GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement).Value;

        public override bool PlaceImportsInsideNamespaces
            => PreferredUsingDirectivePlacement == AddImportPlacement.InsideNamespace;

        public override string Language
            => LanguageNames.CSharp;

        public override CodeGenerationOptions GetOptions(CodeGenerationContext context)
            => new CSharpCodeGenerationOptions(context, this);

        public static new async Task<CSharpCodeGenerationPreferences> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var parseOptions = (CSharpParseOptions?)document.Project.ParseOptions;
            Contract.ThrowIfNull(parseOptions);

            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return new CSharpCodeGenerationPreferences(parseOptions, documentOptions);
        }
    }
}
