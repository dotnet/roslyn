// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class ScriptLocalScopeBinder : LocalScopeBinder
    {
        private readonly Labels _labels;

        internal ScriptLocalScopeBinder(Labels labels, Binder next) : base(next)
        {
            _labels = labels;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get { return _labels.ScriptInitializer; }
        }

        protected override ImmutableArray<LabelSymbol> BuildLabels()
        {
            return _labels.GetLabels();
        }

        // Labels potentially shared across multiple ScriptLocalScopeBinder instances.
        new internal sealed class Labels
        {
            private readonly SynthesizedInteractiveInitializerMethod _scriptInitializer;
            private readonly CompilationUnitSyntax _syntax;
            private ImmutableArray<LabelSymbol> _lazyLabels;

            internal Labels(SynthesizedInteractiveInitializerMethod scriptInitializer, CompilationUnitSyntax syntax)
            {
                _scriptInitializer = scriptInitializer;
                _syntax = syntax;
            }

            internal SynthesizedInteractiveInitializerMethod ScriptInitializer
            {
                get { return _scriptInitializer; }
            }

            internal ImmutableArray<LabelSymbol> GetLabels()
            {
                if (_lazyLabels == null)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyLabels, GetLabels(_scriptInitializer, _syntax));
                }
                return _lazyLabels;
            }

            private static ImmutableArray<LabelSymbol> GetLabels(SynthesizedInteractiveInitializerMethod scriptInitializer, CompilationUnitSyntax syntax)
            {
                var builder = ArrayBuilder<LabelSymbol>.GetInstance();
                foreach (var member in syntax.Members)
                {
                    if (member.Kind() != SyntaxKind.GlobalStatement)
                    {
                        continue;
                    }
                    LocalScopeBinder.BuildLabels(scriptInitializer, ((GlobalStatementSyntax)member).Statement, ref builder);
                }
                return builder.ToImmutableAndFree();
            }
        }
    }
}
