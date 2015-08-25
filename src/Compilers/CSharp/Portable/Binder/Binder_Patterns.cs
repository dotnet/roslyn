// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class Binder
    {
        private BoundExpression BindIsPatternExpression(IsPatternExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var expression = BindExpression(node.Expression, diagnostics);
            var pattern = BindPattern(node.Pattern, diagnostics);
            return new BoundIsPattern(node, expression, pattern, GetSpecialType(SpecialType.System_Boolean, diagnostics, node));
        }

        private BoundPattern BindPattern(PatternSyntax node, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DeclarationPattern:
                    return BindDeclarationPattern((DeclarationPatternSyntax)node, diagnostics);

                default:
                    throw new NotImplementedException();
            }
        }

        private BoundPattern BindDeclarationPattern(DeclarationPatternSyntax node, DiagnosticBag diagnostics)
        {
            bool hasErrors = node.HasErrors;
            var typeSyntax = node.Type;
            var identifier = node.Identifier;

            bool isVar;
            AliasSymbol aliasOpt;
            TypeSymbol declType = BindType(typeSyntax, diagnostics, out isVar, out aliasOpt);
            if (declType.IsStatic)
            {
                Error(diagnostics, ErrorCode.ERR_VarDeclIsStaticClass, typeSyntax, declType);
            }
            SourceLocalSymbol localSymbol = this.LookupLocal(identifier);

            // In error scenarios with misplaced code, it is possible we can't bind the local declaration.
            // This occurs through the semantic model.  In that case concoct a plausible result.
            if ((object)localSymbol == null)
            {
                localSymbol = SourceLocalSymbol.MakeLocal(
                    ContainingMemberOrLambda,
                    this,
                    typeSyntax,
                    identifier,
                    LocalDeclarationKind.PatternVariable);
            }

            // Check for variable declaration errors.
            hasErrors |= this.ValidateDeclarationNameConflictsInScope(localSymbol, diagnostics);

            if (this.ContainingMemberOrLambda.Kind == SymbolKind.Method
                && ((MethodSymbol)this.ContainingMemberOrLambda).IsAsync
                && declType.IsRestrictedType())
            {
                Error(diagnostics, ErrorCode.ERR_BadSpecialByRefLocal, typeSyntax, declType);
                hasErrors = true;
            }

            DeclareLocalVariable(localSymbol, identifier, declType);
            var boundDeclType = new BoundTypeExpression(typeSyntax, aliasOpt, inferredType: false, type: declType);
            return new BoundDeclarationPattern(node, localSymbol, boundDeclType);
        }
    }
}
