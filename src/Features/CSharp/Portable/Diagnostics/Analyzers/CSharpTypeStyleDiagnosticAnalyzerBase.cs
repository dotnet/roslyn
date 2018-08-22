// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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
            if (!typeStyle.IsStylePreferred || !typeStyle.CanConvert())
            {
                return;
            }

            // TO-DO: Reduce degree of tight-coupling and hard-coded options
            ImmutableDictionary<string, string> properties = null;
            var preferences = typeStyle.GetTypeStylePreferences;
            if (preferences == CodeStyle.TypeStyle.TypeStylePreference.ImplicitTypeForIntrinsicTypes)
            {
                properties = SetOptionNameAndValue(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, optionSet);
            }
            else if (preferences == CodeStyle.TypeStyle.TypeStylePreference.ImplicitTypeWhereApparent)
            {
                properties = SetOptionNameAndValue(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, optionSet);
            }
            else if (preferences == CodeStyle.TypeStyle.TypeStylePreference.ImplicitTypeWherePossible)
            {
                properties = SetOptionNameAndValue(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, optionSet);
            }

            // The severity preference is not Hidden, as indicated by IsStylePreferred.
            var descriptor = Descriptor;
            context.ReportDiagnostic(CreateDiagnostic(descriptor, declarationStatement, declaredType.StripRefIfNeeded().Span, typeStyle.Severity, properties));
        }

        private ImmutableDictionary<string, string> SetOptionNameAndValue(Option<CodeStyleOption<bool>> option, OptionSet optionSet)
        {
            var properties = ImmutableDictionary.CreateBuilder<string, string>();
            var name = option.StorageLocations.OfType<EditorConfigStorageLocation<CodeStyleOption<bool>>>().FirstOrDefault();
            if (name != null) {
                properties["OptionName"] = name.KeyName;
                var optionCurrent = optionSet.GetOption(option);
                properties["OptionCurrent"] = optionCurrent.Value.ToString().ToLowerInvariant();
            }
            return properties.ToImmutable();
        }

        private Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, SyntaxNode declaration, TextSpan diagnosticSpan, ReportDiagnostic severity, ImmutableDictionary<string, string> properties)
            => DiagnosticHelper.Create(descriptor, declaration.SyntaxTree.GetLocation(diagnosticSpan), severity, additionalLocations: null, properties: properties);
    }
}
