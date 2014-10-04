// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A class that represents the set of variables in a scope that have been
    /// captured by lambdas within that scope.
    /// </summary>
    internal sealed class LambdaFrame : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol topLevelMethod;
        private readonly MethodSymbol constructor;
        internal readonly CSharpSyntaxNode ScopeSyntax;

        internal LambdaFrame(TypeCompilationState compilationState, MethodSymbol topLevelMethod, CSharpSyntaxNode scopeSyntax)
            : base(GeneratedNames.MakeLambdaDisplayClassName(compilationState.GenerateTempNumber()), topLevelMethod)
        {
            AssertIsLambdaScopeSyntax(scopeSyntax);

            this.topLevelMethod = topLevelMethod;
            this.constructor = new LambdaFrameConstructor(this);
            this.ScopeSyntax = scopeSyntax;
        }

        [Conditional("DEBUG")]
        private static void AssertIsLambdaScopeSyntax(CSharpSyntaxNode syntax)
        {
            // block:
            if (syntax.IsKind(SyntaxKind.Block))
            {
                return;
            }

            // switch block:
            if (syntax.IsKind(SyntaxKind.SwitchStatement))
            {
                return;
            }

            // expression-bodied member:
            if (syntax.Parent.IsKind(SyntaxKind.ArrowExpressionClause))
            {
                return;
            }

            // catch clause (including filter):
            if (syntax.IsKind(SyntaxKind.CatchClause))
            {
                return;
            }

            // class/struct containing a field/property with a declaration expression
            if (syntax.IsKind(SyntaxKind.ClassDeclaration) || syntax.IsKind(SyntaxKind.StructDeclaration))
            {
                return;
            }

#if FEATURE_CSHARP6_CUT
            // primary constructor:
            if (syntax.IsKind(SyntaxKind.ParameterList))
            {
                Debug.Assert(syntax.Parent.IsKind(SyntaxKind.ClassDeclaration) || syntax.Parent.IsKind(SyntaxKind.StructDeclaration));
                return;
            }

            // base class with arguments containing a declaration expression:
            if (syntax.IsKind(SyntaxKind.BaseClassWithArguments))
            {
                return;
            }

            // constructor declaration with constructor initializer containing a declaration expression:
            if (syntax.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                return;
            }

            // statement body containing a declaration expression:
            if (syntax is StatementSyntax)
            {
                Debug.Assert(IsStatementWithEmbeddedStatementBody(syntax.Parent.Kind));
                return;
            }
#endif

            if (IsStatementWithEmbeddedStatementBody(syntax.Kind))
            {
                return;
            }

            // lambda bodies:
            if (SyntaxFacts.IsLambdaBody(syntax))
            {
                return;
            }

            // TODO: EE expression
            if (syntax is ExpressionSyntax && syntax.Parent.Parent == null)
            {
                return;
            }
            
            throw ExceptionUtilities.UnexpectedValue(syntax.CSharpKind());
        }

        private static bool IsStatementWithEmbeddedStatementBody(SyntaxKind syntax)
        {
            switch (syntax)
            {
                case SyntaxKind.IfStatement:
                case SyntaxKind.DoStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.UsingStatement:
                case SyntaxKind.FixedStatement:
                case SyntaxKind.LockStatement:
                    return true;
            }

            return false;
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Class; }
        }

        internal override MethodSymbol Constructor
        {
            get { return this.constructor; }
        }

        public override Symbol ContainingSymbol
        {
            get { return topLevelMethod.ContainingSymbol; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get
            {
                // the lambda method contains user code from the lambda:
                return true;
            }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return topLevelMethod; }
        }
    }
}
