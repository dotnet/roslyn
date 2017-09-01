// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseDeconstruction
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal class CSharpUseDeconstructionDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseDeconstructionDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseDeconstructionDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Deconstruct_variable_declaration), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Variable_declaration_can_be_deconstructed), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode,
                SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(context.Node.SyntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferDeconstruction);
            if (!option.Value)
            {
                return;
            }

            switch (context.Node)
            {
                case VariableDeclarationSyntax variableDeclaration:
                    AnalyzeVariableDeclaration(context, variableDeclaration, option.Notification.Value);
                    return;
                //case SyntaxKind.ForEachStatement:
                //    AnalyzeForEachStatement(context, option.Notification.Value);
                //    return;
            }
        }

        private void AnalyzeVariableDeclaration(
            SyntaxNodeAnalysisContext context, VariableDeclarationSyntax variableDeclaration, DiagnosticSeverity severity)
        {
            if (!this.TryAnalyzeVariableDeclaration(
                    context.SemanticModel, variableDeclaration,
                    out var memberAccessExpressions, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                this.GetDescriptorWithSeverity(severity),
                variableDeclaration.Variables[0].Identifier.GetLocation()));
        }

        public bool TryAnalyzeVariableDeclaration(
            SemanticModel semanticModel, VariableDeclarationSyntax variableDeclaration,
            out ImmutableArray<MemberAccessExpressionSyntax> memberAccessExpressions,
            CancellationToken cancellationToken)
        {
            memberAccessExpressions = default;

            if (!variableDeclaration.IsParentKind(SyntaxKind.LocalDeclarationStatement))
            {
                return false;
            }

            if (!IsViableTupleTypeSyntax(variableDeclaration.Type))
            {
                return false;
            }

            if (variableDeclaration.Variables.Count != 1)
            {
                return false;
            }

            var declarator = variableDeclaration.Variables[0];
            if (declarator.Initializer == null)
            {
                return false;
            }

            var type = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken).Type;
            if (type == null || !type.IsTupleType)
            {
                return false;
            }

            var tupleType = (INamedTypeSymbol)type;

            var local = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declarator, cancellationToken);
            var searchScope = variableDeclaration.Parent.Parent;
            var variableName = declarator.Identifier.ValueText;

            var references = ArrayBuilder<MemberAccessExpressionSyntax>.GetInstance();
            try
            {
                if (!OnlyUsedToAccessTupleFields(
                        semanticModel, searchScope, local,
                        references, cancellationToken))
                {
                    return false;
                }

                if (AnyTupleFieldNamesCollideWithExistingNames(
                        semanticModel, tupleType, searchScope, cancellationToken))
                {
                    return false;
                }

                memberAccessExpressions = references.ToImmutable();
                return true;
            }
            finally
            {
                references.Free();
            }
        }

        private bool AnyTupleFieldNamesCollideWithExistingNames(
            SemanticModel semanticModel, INamedTypeSymbol tupleType,
            SyntaxNode container, CancellationToken cancellationToken)
        {
            var existingSymbols = GetExistingSymbols(semanticModel, container, cancellationToken);

            var reservedNames = semanticModel.LookupSymbols(container.SpanStart)
                                             .Select(s => s.Name)
                                             .Concat(existingSymbols.Select(s => s.Name))
                                             .ToSet();

            foreach (var element in tupleType.TupleElements)
            {
                if (reservedNames.Contains(element.Name))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsViableTupleTypeSyntax(TypeSyntax type)
        {
            if (type.IsVar)
            {
                // 'var t' can be converted to 'var (x, y, z)'
                return true;
            }

            if (type.IsKind(SyntaxKind.TupleType))
            {
                // '(int x, int y) t' can be convered to '(int x, int y)'.  So all the elements
                // need names.

                var tupleType = (TupleTypeSyntax)type;
                foreach (var element in tupleType.Elements)
                {
                    if (element.Identifier.IsKind(SyntaxKind.None))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        private bool OnlyUsedToAccessTupleFields(
            SemanticModel semanticModel, SyntaxNode searchScope, ILocalSymbol local,
            ArrayBuilder<MemberAccessExpressionSyntax> memberAccessLocations, CancellationToken cancellationToken)
        {
            var localName = local.Name;

            foreach (var identifierName in searchScope.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (identifierName.Identifier.ValueText == localName)
                {
                    var symbol = semanticModel.GetSymbolInfo(identifierName, cancellationToken).GetAnySymbol();
                    if (local.Equals(symbol))
                    {
                        if (!(identifierName.Parent is MemberAccessExpressionSyntax memberAccess))
                        {
                            return false;
                        }

                        var member = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).GetAnySymbol();
                        if (!(member is IFieldSymbol field))
                        {
                            return false;
                        }

                        if (field.CorrespondingTupleField == field)
                        {
                            // They're referring to .Item1-.ItemN.  We can't update this to refer to the local
                            return false;
                        }

                        memberAccessLocations.Add(memberAccess);
                    }
                }
            }

            return true;
        }

        private static IEnumerable<ISymbol> GetExistingSymbols(
            SemanticModel semanticModel, SyntaxNode container, CancellationToken cancellationToken)
        {
            // Ignore an annonymous type property.  It's ok if they have a name that 
            // matches the name of the local we're introducing.
            return semanticModel.GetAllDeclaredSymbols(container, cancellationToken)
                                .Where(s => !s.IsAnonymousTypeProperty());
        }
    }
}
