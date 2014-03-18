// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class UsingStatementBinder : LocalScopeBinder
    {
        private readonly UsingStatementSyntax syntax;
        private readonly LockOrUsingStatementExpressionHandler expressionHandler;

        public UsingStatementBinder(MethodSymbol owner, Binder enclosing, UsingStatementSyntax syntax)
            : base(owner, enclosing)
        {
            this.syntax = syntax;
            this.expressionHandler = syntax.Expression == null ? null : new LockOrUsingStatementExpressionHandler(syntax.Expression, this);
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            if (syntax.Declaration != null)
            {
                var locals = new ArrayBuilder<LocalSymbol>(syntax.Declaration.Variables.Count);
                foreach (VariableDeclaratorSyntax declarator in syntax.Declaration.Variables)
                {
                    locals.Add(SourceLocalSymbol.MakeLocal(this.Owner, this, syntax.Declaration.Type, declarator.Identifier, declarator.Initializer, LocalDeclarationKind.Using));
                }

                return locals.ToImmutable();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override ImmutableHashSet<Symbol> LockedOrDisposedVariables
        {
            get
            {
                return expressionHandler == null
                    ? Next.LockedOrDisposedVariables
                    : expressionHandler.LockedOrDisposedVariables;
            }
        }

        internal override BoundStatement BindUsingStatementParts(DiagnosticBag diagnostics)
        {
            ExpressionSyntax expressionSyntax = syntax.Expression;
            VariableDeclarationSyntax declarationSyntax = syntax.Declaration;

            Debug.Assert((expressionSyntax == null) ^ (declarationSyntax == null)); // Can't have both or neither.

            bool hasErrors = false;
            ImmutableArray<LocalSymbol> localsOpt = default(ImmutableArray<LocalSymbol>);
            BoundMultipleLocalDeclarations declarationsOpt = null;
            BoundExpression expressionOpt = null;
            Conversion iDisposableConversion = Conversion.NoConversion;
            TypeSymbol iDisposable = this.Compilation.GetSpecialType(SpecialType.System_IDisposable); // no need for diagnostics, so use the Compilation version
            Debug.Assert((object)iDisposable != null);

            if (expressionSyntax != null)
            {
                Debug.Assert(this.expressionHandler != null);
                expressionOpt = this.expressionHandler.GetExpression(diagnostics);

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                iDisposableConversion = this.Conversions.ClassifyImplicitConversionFromExpression(expressionOpt, iDisposable, ref useSiteDiagnostics);
                diagnostics.Add(expressionSyntax, useSiteDiagnostics);

                if (!iDisposableConversion.IsImplicit)
                {
                    TypeSymbol expressionType = expressionOpt.Type;
                    if ((object)expressionType == null || !expressionType.IsErrorType())
                    {
                        Error(diagnostics, ErrorCode.ERR_NoConvToIDisp, expressionSyntax, expressionOpt.Display);
                    }
                    hasErrors = true;
                }
            }
            else
            {
                ImmutableArray<BoundLocalDeclaration> declarations;
                BindForOrUsingOrFixedDeclarations(declarationSyntax, LocalDeclarationKind.Using, diagnostics, out localsOpt, out declarations);

                Debug.Assert(!localsOpt.IsEmpty);
                Debug.Assert(!declarations.IsEmpty);

                declarationsOpt = new BoundMultipleLocalDeclarations(declarationSyntax, declarations);

                TypeSymbol declType = declarations[0].DeclaredType.Type;

                if (declType.IsDynamic())
                {
                    iDisposableConversion = Conversion.ImplicitDynamic;
                }
                else
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    iDisposableConversion = Conversions.ClassifyImplicitConversion(declType, iDisposable, ref useSiteDiagnostics);
                    diagnostics.Add(declarationSyntax, useSiteDiagnostics);

                    if (!iDisposableConversion.IsImplicit)
                    {
                        if (!declType.IsErrorType())
                        {
                            Error(diagnostics, ErrorCode.ERR_NoConvToIDisp, declarationSyntax, declType);
                        }

                        hasErrors = true;
                    }
                }
            }

            BoundStatement boundBody = BindPossibleEmbeddedStatement(syntax.Statement, diagnostics);

            return new BoundUsingStatement(
                syntax,
                localsOpt,
                declarationsOpt,
                expressionOpt,
                iDisposableConversion,
                boundBody,
                hasErrors);
        }
    }
}