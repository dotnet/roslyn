// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;

#if DEBUG
using Roslyn.Utilities;

#endif
namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// When compiling in metadata-only mode, <see cref="MethodCompiler"/> is not run. This is problematic because 
    /// <see cref="MethodCompiler"/> adds synthesized explicit implementations to the list of synthesized definitions.
    /// In lieu of running <see cref="MethodCompiler"/>, this class performs a quick 
    /// traversal of the symbol table and performs processing of synthesized symbols if necessary
    /// </summary>
    internal sealed class SynthesizedMetadataCompiler : CSharpSymbolVisitor
    {
        private readonly PEModuleBuilder _moduleBeingBuilt;
        private readonly CancellationToken _cancellationToken;

        private SynthesizedMetadataCompiler(PEModuleBuilder moduleBeingBuilt, CancellationToken cancellationToken)
        {
            Debug.Assert(moduleBeingBuilt != null);
            _moduleBeingBuilt = moduleBeingBuilt;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Traverse the symbol table and call Module.AddSynthesizedDefinition for each
        /// synthesized explicit implementation stub that has been generated (e.g. when the real
        /// implementation doesn't have the appropriate custom modifiers).
        /// </summary>
        public static void ProcessSynthesizedMembers(
            CSharpCompilation compilation,
            PEModuleBuilder moduleBeingBuilt,
            CancellationToken cancellationToken)
        {
            Debug.Assert(moduleBeingBuilt != null);

            var compiler = new SynthesizedMetadataCompiler(moduleBeingBuilt, cancellationToken);
            compiler.Visit(compilation.SourceModule.GlobalNamespace);
        }

        public override void VisitNamespace(NamespaceSymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            foreach (var s in symbol.GetMembers())
            {
                s.Accept(this);
            }
        }

        public override void VisitNamedType(NamedTypeSymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var sourceTypeSymbol = symbol as SourceMemberContainerTypeSymbol;
            if ((object)sourceTypeSymbol != null)
            {
                if (_moduleBeingBuilt != null)
                {
                    // In some circumstances (e.g. implicit implementation of an interface method by a non-virtual method in a 
                    // base type from another assembly) it is necessary for the compiler to generate explicit implementations for
                    // some interface methods.  They don't go in the symbol table, but if we are emitting metadata, then we should
                    // generate MethodDef entries for them.
                    foreach (var synthesizedExplicitImpl in sourceTypeSymbol.GetSynthesizedExplicitImplementations(_cancellationToken))
                    {
                        _moduleBeingBuilt.AddSynthesizedDefinition(symbol, synthesizedExplicitImpl);
                    }
                }
            }

            foreach (Symbol member in symbol.GetMembers())
            {
                switch (member.Kind)
                {
                    case SymbolKind.Property:
                    case SymbolKind.NamedType:
                        member.Accept(this);
                        break;
                }
            }
        }

        public override void VisitProperty(PropertySymbol symbol)
        {
            var sourceProperty = symbol as SourcePropertySymbol;
            if (sourceProperty is object { IsSealed: true })
            {
                var synthesizedAccessor = sourceProperty.SynthesizedSealedAccessorOpt;
                if ((object)synthesizedAccessor != null)
                {
                    _moduleBeingBuilt.AddSynthesizedDefinition(sourceProperty.ContainingType, synthesizedAccessor);
                }
            }
        }

#if DEBUG
        public override void VisitMethod(MethodSymbol symbol)
        {
            throw ExceptionUtilities.Unreachable;
        }
#endif
    }
}
