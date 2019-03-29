// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SentinenlLocalSymbol : LocalSymbol
    {
        internal static readonly SentinenlLocalSymbol Instance = new SentinenlLocalSymbol();
        private SentinenlLocalSymbol() { }

        public override TypeWithAnnotations TypeWithAnnotations => throw ExceptionUtilities.Unreachable;
        public override RefKind RefKind => throw ExceptionUtilities.Unreachable;
        public override Symbol ContainingSymbol => throw ExceptionUtilities.Unreachable;
        public override ImmutableArray<Location> Locations => throw ExceptionUtilities.Unreachable;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw ExceptionUtilities.Unreachable;
        internal override LocalDeclarationKind DeclarationKind => throw ExceptionUtilities.Unreachable;
        internal override SynthesizedLocalKind SynthesizedKind => throw ExceptionUtilities.Unreachable;
        internal override SyntaxNode ScopeDesignatorOpt => throw ExceptionUtilities.Unreachable;
        internal override bool IsImportedFromMetadata => throw ExceptionUtilities.Unreachable;
        internal override SyntaxToken IdentifierToken => throw ExceptionUtilities.Unreachable;
        internal override bool IsPinned => throw ExceptionUtilities.Unreachable;
        internal override bool IsCompilerGenerated => throw ExceptionUtilities.Unreachable;
        internal override uint RefEscapeScope => throw ExceptionUtilities.Unreachable;
        internal override uint ValEscapeScope => throw ExceptionUtilities.Unreachable;
        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, DiagnosticBag diagnostics = null) => throw ExceptionUtilities.Unreachable;
        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue) => throw ExceptionUtilities.Unreachable;
        internal override SyntaxNode GetDeclaratorSyntax() => throw ExceptionUtilities.Unreachable;
        internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax) => throw ExceptionUtilities.Unreachable;
    }
}
