// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle
{
    internal abstract partial class CSharpTypeStyleDiagnosticAnalyzerBase :
        AbstractCodeStyleDiagnosticAnalyzer
    {
        protected abstract CSharpTypeStyleHelper Helper { get; }

        protected CSharpTypeStyleDiagnosticAnalyzerBase(
            string diagnosticId, LocalizableString title, LocalizableString message)
            : base(diagnosticId, title, message)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
        {
            var forIntrinsicTypesOption = workspace.Options.GetOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes).Notification;
            var whereApparentOption = workspace.Options.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent).Notification;
            var wherePossibleOption = workspace.Options.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible).Notification;

            return !(forIntrinsicTypesOption == NotificationOption.Warning || forIntrinsicTypesOption == NotificationOption.Error ||
                     whereApparentOption == NotificationOption.Warning || whereApparentOption == NotificationOption.Error ||
                     wherePossibleOption == NotificationOption.Warning || wherePossibleOption == NotificationOption.Error);
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
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var declaredType = Helper.FindAnalyzableType(declarationStatement, semanticModel, cancellationToken);
            if (declaredType == null)
            {
                return;
            }

            var typeStyle = Helper.AnalyzeTypeName(
                declaredType, semanticModel, optionSet, cancellationToken);
            if (!typeStyle.IsStylePreferred ||
                typeStyle.Severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) >= ReportDiagnostic.Hidden ||
                !typeStyle.CanConvert())
            {
                return;
            }

            // The severity preference is not Hidden, as indicated by IsStylePreferred.
            var descriptor = Descriptor;
            context.ReportDiagnostic(CreateDiagnostic(descriptor, declarationStatement, declaredType.StripRefIfNeeded().Span, typeStyle.Severity));
        }

        private Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, SyntaxNode declaration, TextSpan diagnosticSpan, ReportDiagnostic severity) 
            => DiagnosticHelper.Create(descriptor, declaration.SyntaxTree.GetLocation(diagnosticSpan), severity, additionalLocations: null, properties: null);
    }
}
