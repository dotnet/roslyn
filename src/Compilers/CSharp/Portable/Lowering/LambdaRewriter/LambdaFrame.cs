// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
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
        private readonly MethodSymbol _topLevelMethod;
        private readonly MethodSymbol _constructor;
        private readonly MethodSymbol _staticConstructor;
        private readonly FieldSymbol _singletonCache;
        internal readonly CSharpSyntaxNode ScopeSyntaxOpt;
        internal readonly int ClosureOrdinal;

        internal LambdaFrame(VariableSlotAllocator slotAllocatorOpt, MethodSymbol topLevelMethod, MethodDebugId methodId, CSharpSyntaxNode scopeSyntaxOpt, int closureOrdinal)
            : base(MakeName(slotAllocatorOpt, scopeSyntaxOpt, methodId, closureOrdinal), topLevelMethod)
        {
            _topLevelMethod = topLevelMethod;
            _constructor = new LambdaFrameConstructor(this);
            this.ClosureOrdinal = closureOrdinal;

            // static lambdas technically have the class scope so the scope syntax is null 
            if (scopeSyntaxOpt == null)
            {
                _staticConstructor = new SynthesizedStaticConstructor(this);
                var cacheVariableName = GeneratedNames.MakeCachedFrameInstanceFieldName();
                _singletonCache = new SynthesizedLambdaCacheFieldSymbol(this, this, cacheVariableName, topLevelMethod, isReadOnly: true, isStatic: true);
            }

            AssertIsClosureScopeSyntax(scopeSyntaxOpt);
            this.ScopeSyntaxOpt = scopeSyntaxOpt;
        }

        private static string MakeName(VariableSlotAllocator slotAllocatorOpt, SyntaxNode scopeSyntaxOpt, MethodDebugId methodId, int closureOrdinal)
        {
            if (scopeSyntaxOpt == null)
            {
                // Display class is shared among static non-generic lambdas accross generations, method ordinal is -1 in that case.
                // A new display class of a static generic lambda is created for each method and each generation.
                return GeneratedNames.MakeStaticLambdaDisplayClassName(methodId.Ordinal, methodId.Generation);
            }

            int previousClosureOrdinal;
            if (slotAllocatorOpt != null && slotAllocatorOpt.TryGetPreviousClosure(scopeSyntaxOpt, out previousClosureOrdinal))
            {
                methodId = slotAllocatorOpt.PreviousMethodId;
                closureOrdinal = previousClosureOrdinal;
            }

            // If we haven't found existing closure in the previous generation, use the current generation method ordinal.
            // That is, don't try to reuse previous generation method ordinal as that might create name conflict. 
            // E.g. 
            //     Gen0                    Gen1
            //                             F() { new closure } // ordinal 0
            //     G() { } // ordinal 0    G() { new closure } // ordinal 1
            //
            // In the example above G is updated and F is added. 
            // G's ordinal in Gen0 is 0. If we used that ordinal for updated G's new closure it would conflict with F's ordinal.

            Debug.Assert(methodId.Ordinal >= 0);
            return GeneratedNames.MakeLambdaDisplayClassName(methodId.Ordinal, methodId.Generation, closureOrdinal);
        }

        [Conditional("DEBUG")]
        private static void AssertIsClosureScopeSyntax(CSharpSyntaxNode syntaxOpt)
        {
            // See C# specification, chapter 3.7 Scopes.

            // static lambdas technically have the class scope so the scope syntax is null 
            if (syntaxOpt == null)
            {
                return;
            }

            if (SyntaxUtilities.IsClosureScope(syntaxOpt))
            {
                return;
            }

            throw ExceptionUtilities.UnexpectedValue(syntaxOpt.Kind());
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Class; }
        }

        internal override MethodSymbol Constructor
        {
            get { return _constructor; }
        }

        internal MethodSymbol StaticConstructor
        {
            get { return _staticConstructor; }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            var members = base.GetMembers();
            if ((object)_staticConstructor != null)
            {
                members = ImmutableArray.Create<Symbol>(_staticConstructor, _singletonCache).AddRange(members);
            }

            return members;
        }

        internal FieldSymbol SingletonCache
        {
            get { return _singletonCache; }
        }

        // display classes for static lambdas do not have any data and can be serialized.
        internal override bool IsSerializable
        {
            get { return (object)_singletonCache != null; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _topLevelMethod.ContainingSymbol; }
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
            get { return _topLevelMethod; }
        }
    }
}
