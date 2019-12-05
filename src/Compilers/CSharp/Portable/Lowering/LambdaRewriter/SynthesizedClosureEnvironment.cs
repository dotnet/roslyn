// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The synthesized type added to a compilation to hold captured variables for closures.
    /// </summary>
    internal sealed class SynthesizedClosureEnvironment : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
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

        private ArrayBuilder<Symbol> _membersBuilder = ArrayBuilder<Symbol>.GetInstance();
        private ImmutableArray<Symbol> _members;

        public override TypeKind TypeKind { get; }
        internal override MethodSymbol Constructor { get; }

        internal SynthesizedClosureEnvironment(
            MethodSymbol topLevelMethod,
            MethodSymbol containingMethod,
            bool isStruct,
            SyntaxNode scopeSyntaxOpt,
            DebugId methodId,
            DebugId closureId)
            : base(MakeName(scopeSyntaxOpt, methodId, closureId), containingMethod)
        {
            TypeKind = isStruct ? TypeKind.Struct : TypeKind.Class;
            _topLevelMethod = topLevelMethod;
            OriginalContainingMethodOpt = containingMethod;
            Constructor = isStruct ? null : new SynthesizedClosureEnvironmentConstructor(this);
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

        internal void AddHoistedField(LambdaCapturedVariable captured) => _membersBuilder.Add(captured);

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
            if (_members.IsDefault)
            {
                var builder = _membersBuilder;
                if ((object)StaticConstructor != null)
                {
                    builder.Add(StaticConstructor);
                    builder.Add(SingletonCache);
                }
                builder.AddRange(base.GetMembers());
                _members = builder.ToImmutableAndFree();
                _membersBuilder = null;
            }

            return _members;
        }

        /// <summary>
        /// All fields should have already been added as synthesized members on the
        /// <see cref="CommonPEModuleBuilder" />, so we don't want to duplicate them here.
        /// </summary>
        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
            => (object)SingletonCache != null
            ? SpecializedCollections.SingletonEnumerable(SingletonCache)
            : SpecializedCollections.EmptyEnumerable<FieldSymbol>();

        // display classes for static lambdas do not have any data and can be serialized.
        public override bool IsSerializable => (object)SingletonCache != null;

        public override Symbol ContainingSymbol => _topLevelMethod.ContainingSymbol;

        // The lambda method contains user code from the lambda
        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => true;

        IMethodSymbolInternal ISynthesizedMethodBodyImplementationSymbol.Method => _topLevelMethod;
    }
}
