// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Desktop.Analyzers.Common;

namespace Desktop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpDoNotCatchCorruptedStateExceptionsAnalyzer : DoNotCatchCorruptedStateExceptionsAnalyzer
    {
        private static readonly ImmutableArray<SyntaxKind> s_nodeKindsOfInterest =
            ImmutableArray.Create(SyntaxKind.MethodDeclaration,
                                  SyntaxKind.ConstructorDeclaration,
                                  SyntaxKind.DestructorDeclaration,
                                  SyntaxKind.ConversionOperatorDeclaration,
                                  SyntaxKind.OperatorDeclaration,
                                  SyntaxKind.AddAccessorDeclaration,
                                  SyntaxKind.RemoveAccessorDeclaration,
                                  SyntaxKind.GetAccessorDeclaration,
                                  SyntaxKind.SetAccessorDeclaration);

        protected override Analyzer GetAnalyzer(CompilationStartAnalysisContext context, CompilationSecurityTypes types)
        {
            Analyzer analyzer = new CSharpAnalyzer(types);
            context.RegisterSyntaxNodeAction(analyzer.AnalyzeNode, s_nodeKindsOfInterest);
            return analyzer;
        }

        private sealed class CSharpAnalyzer : Analyzer
        {
            public CSharpAnalyzer(CompilationSecurityTypes compilationTypes)
                : base(compilationTypes)
            {}

            private static bool MayContainCatchNodesOfInterest(SyntaxNode node)
            {
                SyntaxKind kind = node.Kind();
                switch (kind)
                {
                    case SyntaxKind.AnonymousMethodExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                        // for now there doesn't seem to have any way to annotate lambdas with attributes
                        return false;

                    default:
                        return true;
                }
            }

            protected override void CheckNode(SyntaxNode methodNode, SemanticModel model, Action<Diagnostic> reportDiagnostic)
            {
                Debug.Assert(s_nodeKindsOfInterest.Contains(methodNode.Kind()));

                foreach (SyntaxNode node in methodNode.DescendantNodes(MayContainCatchNodesOfInterest))
                {
                    SyntaxKind kind = node.Kind();
                    if (kind != SyntaxKind.CatchClause)
                    {
                        continue;
                    }

                    ISymbol exceptionTypeSym = null;
                    var catchNode = (CatchClauseSyntax)node;
                    CatchDeclarationSyntax catchDeclaration = catchNode.Declaration;
                    if (catchDeclaration != null)
                    {
                        exceptionTypeSym = SyntaxNodeHelper.GetSymbol(catchDeclaration.Type, model);
                        if (!IsCatchTypeTooGeneral(exceptionTypeSym))
                        {
                            continue;
                        }
                    }

                    if (!HasCorrespondingRethrowInSubTree(catchNode))
                    {
                        reportDiagnostic(
                            Diagnostic.Create(
                                Rule,
                                catchNode.GetLocation(),
                                SyntaxNodeHelper.GetSymbol(methodNode, model).ToDisplayString(),
                                (exceptionTypeSym ?? this.TypesOfInterest.SystemObject).ToDisplayString()));
                    }
                }
            }

            private static bool HasCorrespondingRethrowInSubTree(SyntaxNode catchNode)
            {
                Debug.Assert(catchNode.Kind() == SyntaxKind.CatchClause);

                Func<SyntaxNode, bool> shouldDescend = (node) =>
                    (node.Kind() != SyntaxKind.CatchClause) || (node == catchNode);
                foreach (SyntaxNode n in catchNode.DescendantNodes(shouldDescend))
                {
                    if (n.Kind() == SyntaxKind.ThrowStatement)
                    {
                        var t = (ThrowStatementSyntax)n;
                        if (t.Expression == null)
                        {
                            // We make the same assumption FxCop makes here -- one re-throw implies the dev
                            // understands what he is doing with corrupted process state exceptions
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
