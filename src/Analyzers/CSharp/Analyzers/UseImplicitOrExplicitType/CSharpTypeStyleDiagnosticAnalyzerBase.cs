// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle
{
    internal abstract partial class CSharpTypeStyleDiagnosticAnalyzerBase :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        protected abstract CSharpTypeStyleHelper Helper { get; }

        protected CSharpTypeStyleDiagnosticAnalyzerBase(
            string diagnosticId, EnforceOnBuild enforceOnBuild, LocalizableString title, LocalizableString message)
            : base(diagnosticId,
                   enforceOnBuild,
                   ImmutableHashSet.Create<ILanguageSpecificOption>(CSharpCodeStyleOptions.VarForBuiltInTypes, CSharpCodeStyleOptions.VarWhenTypeIsApparent, CSharpCodeStyleOptions.VarElsewhere),
                   LanguageNames.CSharp,
                   title, message)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(SimplifierOptions? options)
        {
            // analyzer is only active in C# projects
            Contract.ThrowIfNull(options);

            var csOptions = (CSharpSimplifierOptions)options;
            return !(csOptions.VarForBuiltInTypes.Notification.Severity is ReportDiagnostic.Warn or ReportDiagnostic.Error ||
                     csOptions.VarWhenTypeIsApparent.Notification.Severity is ReportDiagnostic.Warn or ReportDiagnostic.Error ||
                     csOptions.VarElsewhere.Notification.Severity is ReportDiagnostic.Warn or ReportDiagnostic.Error);
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(
                HandleVariableDeclaration, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression);

        private void HandleVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declarationStatement = context.Node;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            var semanticModel = context.SemanticModel;
            var declaredType = Helper.FindAnalyzableType(declarationStatement, semanticModel, cancellationToken);
            if (declaredType == null)
            {
                return;
            }

            var simplifierOptions = context.Options.GetCSharpSimplifierOptions(syntaxTree);

            var typeStyle = Helper.AnalyzeTypeName(
                declaredType, semanticModel, simplifierOptions, cancellationToken);
            if (!typeStyle.IsStylePreferred || !typeStyle.CanConvert())
            {
                return;
            }

            // The severity preference is not Hidden, as indicated by IsStylePreferred.
            var descriptor = Descriptor;
            context.ReportDiagnostic(CreateDiagnostic(descriptor, declarationStatement, declaredType.StripRefIfNeeded().Span, typeStyle.Severity));
        }

        private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, SyntaxNode declaration, TextSpan diagnosticSpan, ReportDiagnostic severity)
            => DiagnosticHelper.Create(descriptor, declaration.SyntaxTree.GetLocation(diagnosticSpan), severity, additionalLocations: null, properties: null);
    }
}
