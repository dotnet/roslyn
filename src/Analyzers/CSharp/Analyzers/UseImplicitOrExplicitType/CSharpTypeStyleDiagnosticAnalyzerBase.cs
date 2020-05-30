// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

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
            string diagnosticId, LocalizableString title, LocalizableString message)
            : base(diagnosticId,
                   ImmutableHashSet.Create<ILanguageSpecificOption>(CSharpCodeStyleOptions.VarForBuiltInTypes, CSharpCodeStyleOptions.VarWhenTypeIsApparent, CSharpCodeStyleOptions.VarElsewhere),
                   LanguageNames.CSharp,
                   title, message)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(OptionSet options)
        {
            var forIntrinsicTypesOption = options.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes).Notification;
            var whereApparentOption = options.GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent).Notification;
            var wherePossibleOption = options.GetOption(CSharpCodeStyleOptions.VarElsewhere).Notification;

            return !(forIntrinsicTypesOption == NotificationOption2.Warning || forIntrinsicTypesOption == NotificationOption2.Error ||
                     whereApparentOption == NotificationOption2.Warning || whereApparentOption == NotificationOption2.Error ||
                     wherePossibleOption == NotificationOption2.Warning || wherePossibleOption == NotificationOption2.Error);
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(
                HandleVariableDeclaration, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression);

        private void HandleVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declarationStatement = context.Node;
            var options = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = options.GetAnalyzerOptionSet(syntaxTree, cancellationToken);

            var semanticModel = context.SemanticModel;
            var declaredType = Helper.FindAnalyzableType(declarationStatement, semanticModel, cancellationToken);
            if (declaredType == null)
            {
                return;
            }

            var typeStyle = Helper.AnalyzeTypeName(
                declaredType, semanticModel, optionSet, cancellationToken);
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
