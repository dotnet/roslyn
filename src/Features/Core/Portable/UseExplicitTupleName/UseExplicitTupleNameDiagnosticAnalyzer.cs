// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseExplicitTupleName
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class UseExplicitTupleNameDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public const string ElementName = nameof(ElementName);

        public UseExplicitTupleNameDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseExplicitTupleNameDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_explicitly_provided_tuple_name), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Prefer_explicitly_provided_tuple_element_name), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override bool OpenFileOnly(Workspace workspace) => false;
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeOperation, OperationKind.FieldReference);

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferExplicitTupleNames, context.Compilation.Language);
            var severity = option.Notification.Severity;
            if (severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) >= ReportDiagnostic.Hidden)
            {
                return;
            }

            var fieldReferenceOperation = (IFieldReferenceOperation)context.Operation;

            var field = fieldReferenceOperation.Field;
            if (field.ContainingType.IsTupleType)
            {
                if (field.CorrespondingTupleField.Equals(field))
                {
                    var namedField = GetNamedField(field.ContainingType, field, cancellationToken);
                    if (namedField != null)
                    {
                        var memberAccessSyntax = fieldReferenceOperation.Syntax;
                        var nameNode = memberAccessSyntax.ChildNodesAndTokens().Reverse().FirstOrDefault();
                        if (nameNode != null)
                        {
                            var properties = ImmutableDictionary<string, string>.Empty.Add(
                                nameof(ElementName), namedField.Name);
                            context.ReportDiagnostic(DiagnosticHelper.Create(
                                Descriptor,
                                nameNode.GetLocation(),
                                severity,
                                additionalLocations: null,
                                properties));
                        }
                    }
                }
            }
        }

        private IFieldSymbol GetNamedField(
            INamedTypeSymbol containingType, IFieldSymbol unnamedField, CancellationToken cancellationToken)
        {
            foreach (var member in containingType.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (member.Kind == SymbolKind.Field)
                {
                    var fieldSymbol = (IFieldSymbol)member;
                    if (unnamedField.Equals(fieldSymbol.CorrespondingTupleField) &&
                        !fieldSymbol.Name.Equals(unnamedField.Name))
                    {
                        return fieldSymbol;
                    }
                }
            }

            return null;
        }
    }
}
