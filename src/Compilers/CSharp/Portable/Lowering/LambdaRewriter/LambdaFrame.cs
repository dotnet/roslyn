// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A class that represents the set of variables in a scope that have been
    /// captured by nested functions within that scope.
    /// </summary>
    internal sealed class ClosureEnvironment : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol _topLevelMethod;
        internal readonly SyntaxNode ScopeSyntaxOpt;
        internal readonly int ClosureOrdinal;
        /// <summary>
        /// The closest method/lambda that this frame is originally from. Null if nongeneric static closure.
        /// Useful because this frame's type parameters are constructed from this method and all methods containing this method.
        /// </summary>
        internal readonly MethodSymbol OriginalContainingMethodOpt;
        internal readonly FieldSymbol SingletonCache;
        internal readonly MethodSymbol StaticConstructor;
        public readonly IEnumerable<Symbol> CapturedVariables;

        public override TypeKind TypeKind { get; }
        internal override MethodSymbol Constructor { get; }

        internal ClosureEnvironment(
            IEnumerable<Symbol> capturedVariables,
            MethodSymbol topLevelMethod,
            MethodSymbol containingMethod,
            bool isStruct,
            SyntaxNode scopeSyntaxOpt,
            DebugId methodId,
            DebugId closureId)
            : base(MakeName(scopeSyntaxOpt, methodId, closureId), containingMethod)
        {
            CapturedVariables = capturedVariables;
            TypeKind = isStruct ? TypeKind.Struct : TypeKind.Class;
            _topLevelMethod = topLevelMethod;
            OriginalContainingMethodOpt = containingMethod;
            Constructor = isStruct ? null : new LambdaFrameConstructor(this);
            this.ClosureOrdinal = closureId.Ordinal;

            // static lambdas technically have the class scope so the scope syntax is null 
            if (scopeSyntaxOpt == null)
            {
                StaticConstructor = new SynthesizedStaticConstructor(this);
                var cacheVariableName = GeneratedNames.MakeCachedFrameInstanceFieldName();
                SingletonCache = new SynthesizedLambdaCacheFieldSymbol(this, this, cacheVariableName, topLevelMethod, isReadOnly: true, isStatic: true);
            }

            AssertIsClosureScopeSyntax(scopeSyntaxOpt);
            this.ScopeSyntaxOpt = scopeSyntaxOpt;
        }

        private static string MakeName(SyntaxNode scopeSyntaxOpt, DebugId methodId, DebugId closureId)
        {
            if (scopeSyntaxOpt == null)
            {
                // Display class is shared among static non-generic lambdas across generations, method ordinal is -1 in that case.
                // A new display class of a static generic lambda is created for each method and each generation.
                return GeneratedNames.MakeStaticLambdaDisplayClassName(methodId.Ordinal, methodId.Generation);
            }

            Debug.Assert(methodId.Ordinal >= 0);
            return GeneratedNames.MakeLambdaDisplayClassName(methodId.Ordinal, methodId.Generation, closureId.Ordinal, closureId.Generation);
        }

        [Conditional("DEBUG")]
        private static void AssertIsClosureScopeSyntax(SyntaxNode syntaxOpt)
        {
            // See C# specification, chapter 3.7 Scopes.

            // static lambdas technically have the class scope so the scope syntax is null 
            if (syntaxOpt == null)
            {
                return;
            }

            if (LambdaUtilities.IsClosureScope(syntaxOpt))
            {
                return;
            }

            throw ExceptionUtilities.UnexpectedValue(syntaxOpt.Kind());
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            var members = base.GetMembers();
            if ((object)StaticConstructor != null)
            {
                members = ImmutableArray.Create<Symbol>(StaticConstructor, SingletonCache).AddRange(members);
            }

            return members;
        }

        // display classes for static lambdas do not have any data and can be serialized.
        internal override bool IsSerializable => (object)SingletonCache != null;

        public override Symbol ContainingSymbol => _topLevelMethod.ContainingSymbol;

        // The lambda method contains user code from the lambda
        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => true;

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method => _topLevelMethod;
    }
}
