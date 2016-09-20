//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.Diagnostics;

//namespace Microsoft.CodeAnalysis.UseObjectInitializer
//{
//    internal abstract class AbstractUseObjectInitializerDiagnosticAnalyzer<
//            TObjectCreationExpression, 
//            TEqualsValueClause,
//            TVariableDeclarator,
//            TAssignmentExpression,
//            TSyntaxKind>
//        : DiagnosticAnalyzer
//        where TObjectCreationExpression : SyntaxNode
//        where TEqualsValueClause : SyntaxNode
//        where TVariableDeclarator : SyntaxNode
//        where TAssignmentExpression : SyntaxNode
//        where TSyntaxKind : struct
//    {
//        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
//        {
//            get
//            {
//                throw new NotImplementedException();
//            }
//        }

//        public override void Initialize(AnalysisContext context)
//        {
//            context.RegisterSyntaxNodeAction<TSyntaxKind>(
//                AnalyzeNode, 
//                ImmutableArray.Create(GetObjectCreationSyntaxKind()));
//        }

//        protected abstract TSyntaxKind GetObjectCreationSyntaxKind();

//        private void AnalyzeNode(SyntaxNodeAnalysisContext obj)
//        {
//            var objectCreationNode = (TObjectCreationExpression)obj.Node;
//            if (objectCreationNode.Parent is TEqualsValueClause &&
//                objectCreationNode.Parent.Parent is TVariableDeclarator)
//            {
//                AnalyzeInVariableDeclarator(objectCreationNode);
//                return;
//            }

//            if( objectCreationNode.Parent is TAssignmentExpression &&)
//        }
//    }
//}
