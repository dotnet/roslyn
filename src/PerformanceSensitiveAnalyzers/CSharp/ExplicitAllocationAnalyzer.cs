// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ExplicitAllocationAnalyzer : AbstractAllocationAnalyzer<SyntaxKind>
    {
        public const string NewArrayRuleId = "HAA0501";
        public const string NewObjectRuleId = "HAA0502";
        public const string AnonymousNewObjectRuleId = "HAA0503";
        public const string ImplicitArrayCreationRuleId = "HAA0504";
        public const string InitializerCreationRuleId = "HAA0505";
        public const string LetCauseRuleId = "HAA0506";

        private static readonly LocalizableString s_localizableNewArrayRuleTitleAndMessage = new LocalizableResourceString(nameof(AnalyzersResources.NewArrayRuleTitleAndMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableNewObjectRuleTitleAndMessage = new LocalizableResourceString(nameof(AnalyzersResources.NewObjectRuleTitleAndMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableAnonymousNewObjectRuleTitleAndMessage = new LocalizableResourceString(nameof(AnalyzersResources.AnonymousNewObjectRuleTitleAndMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableImplicitArrayCreationRuleTitleAndMessage = new LocalizableResourceString(nameof(AnalyzersResources.ImplicitArrayCreationRuleTitleAndMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableInitializerCreationRuleTitleAndMessage = new LocalizableResourceString(nameof(AnalyzersResources.InitializerCreationRuleTitleAndMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableLetCauseRuleTitleAndMessage = new LocalizableResourceString(nameof(AnalyzersResources.LetCauseRuleTitleAndMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        internal static DiagnosticDescriptor NewArrayRule = new DiagnosticDescriptor(
            NewArrayRuleId,
            s_localizableNewArrayRuleTitleAndMessage,
            s_localizableNewArrayRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor NewObjectRule = new DiagnosticDescriptor(
            NewObjectRuleId,
            s_localizableNewObjectRuleTitleAndMessage,
            s_localizableNewObjectRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor AnonymousNewObjectRule = new DiagnosticDescriptor(
            AnonymousNewObjectRuleId,
            s_localizableAnonymousNewObjectRuleTitleAndMessage,
            s_localizableAnonymousNewObjectRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: "http://msdn.microsoft.com/en-us/library/bb397696.aspx");

        internal static DiagnosticDescriptor ImplicitArrayCreationRule = new DiagnosticDescriptor(
            ImplicitArrayCreationRuleId,
            s_localizableImplicitArrayCreationRuleTitleAndMessage,
            s_localizableImplicitArrayCreationRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor InitializerCreationRule = new DiagnosticDescriptor(
            InitializerCreationRuleId,
            s_localizableInitializerCreationRuleTitleAndMessage,
            s_localizableInitializerCreationRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor LetCauseRule = new DiagnosticDescriptor(
            LetCauseRuleId,
            s_localizableLetCauseRuleTitleAndMessage,
            s_localizableLetCauseRuleTitleAndMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(LetCauseRule, InitializerCreationRule, ImplicitArrayCreationRule, AnonymousNewObjectRule, NewObjectRule, NewArrayRule);

        protected override ImmutableArray<SyntaxKind> Expressions => ImmutableArray.Create(
            SyntaxKind.ObjectCreationExpression,            // Used
            SyntaxKind.AnonymousObjectCreationExpression,   // Used
            SyntaxKind.ArrayInitializerExpression,          // Used (this is inside an ImplicitArrayCreationExpression)
            SyntaxKind.CollectionInitializerExpression,     // Is this used anywhere?
            SyntaxKind.ComplexElementInitializerExpression, // Is this used anywhere? For what this is see http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.CSharp/Compilation/CSharpSemanticModel.cs,80
            SyntaxKind.ObjectInitializerExpression,         // Used linked to InitializerExpressionSyntax
            SyntaxKind.ArrayCreationExpression,             // Used
            SyntaxKind.ImplicitArrayCreationExpression,     // Used (this then contains an ArrayInitializerExpression)
            SyntaxKind.LetClause                            // Used
            );

        private static readonly object[] EmptyMessageArgs = Array.Empty<object>();

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, in PerformanceSensitiveInfo info)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            var cancellationToken = context.CancellationToken;

            // An InitializerExpressionSyntax has an ObjectCreationExpressionSyntax as it's parent, i.e
            // var testing = new TestClass { Name = "Bob" };
            //               |             |--------------| <- InitializerExpressionSyntax or SyntaxKind.ObjectInitializerExpression
            //               |----------------------------| <- ObjectCreationExpressionSyntax or SyntaxKind.ObjectCreationExpression
            var initializerExpression = node as InitializerExpressionSyntax;
            if (initializerExpression?.Parent is ObjectCreationExpressionSyntax)
            {
                var objectCreation = node.Parent as ObjectCreationExpressionSyntax;
                var typeInfo = semanticModel.GetTypeInfo(objectCreation, cancellationToken);
                if (typeInfo.ConvertedType?.TypeKind != TypeKind.Error &&
                    typeInfo.ConvertedType?.IsReferenceType == true &&
                    objectCreation.Parent?.IsKind(SyntaxKind.EqualsValueClause) == true &&
                    objectCreation.Parent?.Parent?.IsKind(SyntaxKind.VariableDeclarator) == true)
                {
                    reportDiagnostic(Diagnostic.Create(InitializerCreationRule, ((VariableDeclaratorSyntax)objectCreation.Parent.Parent).Identifier.GetLocation(), EmptyMessageArgs));
                    return;
                }
            }

            if (node is ImplicitArrayCreationExpressionSyntax implicitArrayExpression)
            {
                reportDiagnostic(Diagnostic.Create(ImplicitArrayCreationRule, implicitArrayExpression.NewKeyword.GetLocation(), EmptyMessageArgs));
                return;
            }

            if (node is AnonymousObjectCreationExpressionSyntax newAnon)
            {
                reportDiagnostic(Diagnostic.Create(AnonymousNewObjectRule, newAnon.NewKeyword.GetLocation(), EmptyMessageArgs));
                return;
            }

            if (node is ArrayCreationExpressionSyntax newArr)
            {
                reportDiagnostic(Diagnostic.Create(NewArrayRule, newArr.NewKeyword.GetLocation(), EmptyMessageArgs));
                return;
            }

            if (node is ObjectCreationExpressionSyntax newObj)
            {
                var typeInfo = semanticModel.GetTypeInfo(newObj, cancellationToken);
                if (typeInfo.ConvertedType != null && typeInfo.ConvertedType.TypeKind != TypeKind.Error && typeInfo.ConvertedType.IsReferenceType)
                {
                    reportDiagnostic(Diagnostic.Create(NewObjectRule, newObj.NewKeyword.GetLocation(), EmptyMessageArgs));
                }
                return;
            }

            if (node is LetClauseSyntax letKind)
            {
                reportDiagnostic(Diagnostic.Create(LetCauseRule, letKind.LetKeyword.GetLocation(), EmptyMessageArgs));
                return;
            }
        }
    }
}