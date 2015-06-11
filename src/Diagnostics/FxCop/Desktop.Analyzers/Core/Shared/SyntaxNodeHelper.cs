// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Desktop.Analyzers.Common
{
    public abstract class SyntaxNodeHelper
    {
        [System.Flags]
        protected enum CallKind
        {
            None = 0,
            Invocation = 1,
            ObjectCreation = 2, // a constructor call
            AnyCall = Invocation | ObjectCreation,
        };

        public abstract bool ContainsMethodCall(SyntaxNode node, Func<string, bool> predicate);
        public abstract IMethodSymbol GetCallerMethodSymbol(SyntaxNode node, SemanticModel semanticModel);
        public abstract ITypeSymbol GetEnclosingTypeSymbol(SyntaxNode node, SemanticModel semanticModel);
        public abstract ITypeSymbol GetClassDeclarationTypeSymbol(SyntaxNode node, SemanticModel semanticModel);
        public abstract SyntaxNode GetAssignmentLeftNode(SyntaxNode node);
        public abstract SyntaxNode GetAssignmentRightNode(SyntaxNode node);
        public abstract SyntaxNode GetMemberAccessExpressionNode(SyntaxNode node);
        public abstract SyntaxNode GetMemberAccessNameNode(SyntaxNode node); 
        public abstract SyntaxNode GetCallTargetNode(SyntaxNode node);
        public abstract SyntaxNode GetInvocationExpressionNode(SyntaxNode node);
        public abstract SyntaxNode GetDefaultValueForAnOptionalParameter(SyntaxNode declNode, int paramIndex);
        protected abstract IEnumerable<SyntaxNode> GetCallArgumentExpressionNodes(SyntaxNode node, CallKind callKind);
        public abstract IEnumerable<SyntaxNode> GetObjectInitializerExpressionNodes(SyntaxNode node);
        public abstract IEnumerable<SyntaxNode> GetDescendantAssignmentExpressionNodes(SyntaxNode node);
        public abstract IEnumerable<SyntaxNode> GetDescendantMemberAccessExpressionNodes(SyntaxNode node);

        public IEnumerable<SyntaxNode> GetCallArgumentExpressionNodes(SyntaxNode node)
        {
            return GetCallArgumentExpressionNodes(node, CallKind.AnyCall);
        }

        public IEnumerable<SyntaxNode> GetInvocationArgumentExpressionNodes(SyntaxNode node)
        {
            return GetCallArgumentExpressionNodes(node, CallKind.Invocation);
        }

        public IEnumerable<SyntaxNode> GetObjectCreationArgumentExpressionNodes(SyntaxNode node)
        {
            return GetCallArgumentExpressionNodes(node, CallKind.ObjectCreation);
        }

        // This will return true iff the SyntaxNode is either InvocationExpression or ObjectCreationExpression (in C# or VB)
        public abstract bool IsMethodInvocationNode(SyntaxNode node);

        // returns true if node is an ObjectCreationExpression and is under a FieldDeclaration node
        public abstract bool IsObjectCreationExpressionUnderFieldDeclaration(SyntaxNode node);

        // returns the ancestor VariableDeclarator node for an ObjectCreationExpression if 
        // IsObjectCreationExpressionUnderFieldDeclaration(node) returns true, return null otherwise.
        public abstract SyntaxNode GetVariableDeclaratorOfAFieldDeclarationNode(SyntaxNode objectCreationExpression);

        public ISymbol GetEnclosingConstructSymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node == null)
            {
                return null;
            }

            ISymbol symbol = GetCallerMethodSymbol(node, semanticModel);

            if (symbol == null)
            {
                symbol = GetEnclosingTypeSymbol(node, semanticModel);
            }

            return symbol;
        }
 
        public static IMethodSymbol GetCalleeMethodSymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            ISymbol symbol = GetReferencedSymbol(node, semanticModel);
            if (symbol != null && symbol.Kind == SymbolKind.Method)
            {
                return (IMethodSymbol)symbol;
            }

            return null;
        }

        public static IEnumerable<IMethodSymbol> GetCandidateCalleeMethodSymbols(SyntaxNode node, SemanticModel semanticModel)
        {
            foreach (ISymbol symbol in GetCandidateReferencedSymbols(node, semanticModel))
            {
                if (symbol != null && symbol.Kind == SymbolKind.Method)
                {
                    yield return (IMethodSymbol)symbol;
                }
            }
        }

        public static IEnumerable<IMethodSymbol> GetCalleeMethodSymbols(SyntaxNode node, SemanticModel semanticModel)
        {
            IMethodSymbol symbol = GetCalleeMethodSymbol(node, semanticModel); 
            if (symbol != null)
            {
                return new List<IMethodSymbol>() { symbol };
            }

            return GetCandidateCalleeMethodSymbols(node, semanticModel);
        }

        public static IPropertySymbol GetCalleePropertySymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            ISymbol symbol = GetReferencedSymbol(node, semanticModel);
            if (symbol != null && symbol.Kind == SymbolKind.Property)
            {
                return (IPropertySymbol)symbol;
            }

            return null;
        }

        public static IFieldSymbol GetCalleeFieldSymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            ISymbol symbol = GetReferencedSymbol(node, semanticModel);
            if (symbol != null && symbol.Kind == SymbolKind.Field)
            {
                return (IFieldSymbol)symbol;
            }

            return null;
        }

        public static ISymbol GetSymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            return GetDeclaredSymbol(node, semanticModel) ?? GetReferencedSymbol(node, semanticModel);
        }

        public static ISymbol GetDeclaredSymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node == null)
            {
                return null;
            }

            return semanticModel.GetDeclaredSymbol(node);
        }

        public static ISymbol GetReferencedSymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node == null)
            {
                return null;
            }

            return semanticModel.GetSymbolInfo(node).Symbol;
        }

        public static IEnumerable<ISymbol> GetCandidateReferencedSymbols(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node == null)
            {
                return null;
            }

            return semanticModel.GetSymbolInfo(node).CandidateSymbols;
        }

        public static bool NodeHasConstantValueNull(SyntaxNode node, SemanticModel model)
        {
            if (node == null || model == null)
            {
                return false;
            }
            var value = model.GetConstantValue(node);
            return value.HasValue && value.Value == null;
        }

        public static bool NodeHasConstantValueIntZero(SyntaxNode node, SemanticModel model)
        {
            if (node == null || model == null)
            {
                return false;
            }
            var value = model.GetConstantValue(node);
            return value.HasValue && 
                   value.Value is int && 
                   (int)value.Value == 0;
        }

        public static bool NodeHasConstantValueBoolFalse(SyntaxNode node, SemanticModel model)
        {
            if (node == null || model == null)
            {
                return false;
            }
            var value = model.GetConstantValue(node);
            return value.HasValue &&
                   value.Value is bool &&
                   (bool)value.Value == false;
        }
    }
}
