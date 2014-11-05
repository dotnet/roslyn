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
        private readonly MethodSymbol staticConstructor;
        private readonly FieldSymbol singletonCache;
        internal readonly CSharpSyntaxNode ScopeSyntaxOpt;

        internal LambdaFrame(TypeCompilationState compilationState, MethodSymbol topLevelMethod, CSharpSyntaxNode scopeSyntax, bool isStatic)
            : base(GeneratedNames.MakeLambdaDisplayClassName(compilationState.GenerateTempNumber()), topLevelMethod)
        {
            this.topLevelMethod = topLevelMethod;
            this.constructor = new LambdaFrameConstructor(this);

            // static lambdas technically have the class scope so the scope syntax is null 
            if (isStatic)
            {
                this.staticConstructor = new SynthesizedStaticConstructor(this);
                var cacheVariableName = GeneratedNames.MakeCachedFrameInstanceName();
                singletonCache = new SynthesizedFieldSymbol(this, this, cacheVariableName, isPublic: true, isStatic: true, isReadOnly: true);
                this.ScopeSyntaxOpt = null;
            }
            else
            {
                this.ScopeSyntaxOpt = scopeSyntax;
            }

            AssertIsLambdaScopeSyntax(this.ScopeSyntaxOpt);
        }

        [Conditional("DEBUG")]
        private static void AssertIsLambdaScopeSyntax(CSharpSyntaxNode syntax)
        {
            // See C# specification, chapter 3.7 Scopes.

            // static lambdas technically have the class scope so the scope syntax is null 
            if (syntax == null)
            {
                return;
            }

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
            if (syntax.IsKind(SyntaxKind.ArrowExpressionClause))
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

            // lambda in a let clause, 
            // e.g. from item in array let a = new Func<int>(() => item)
            if (syntax.IsKind(SyntaxKind.LetClause))
            {
                return;
            }

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

        internal MethodSymbol StaticConstructor
        {
            get { return this.staticConstructor; }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            var members = base.GetMembers();
            if ((object)this.staticConstructor != null)
            {
                members = ImmutableArray.Create<Symbol>(this.staticConstructor, this.singletonCache).AddRange(members);
            }

            return members;
        }

        internal FieldSymbol SingletonCache
        {
            get { return this.singletonCache; }
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
