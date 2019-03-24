// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class TypeSubstitutedLocalSymbol : LocalSymbol
    {
        private readonly LocalSymbol _originalVariable;
        private readonly TypeWithAnnotations _type;
        private readonly Symbol _containingSymbol;

        public TypeSubstitutedLocalSymbol(LocalSymbol originalVariable, TypeWithAnnotations type, Symbol containingSymbol)
        {
            Debug.Assert(originalVariable != null);
            Debug.Assert(type.HasType);
            Debug.Assert(containingSymbol != null);

            _originalVariable = originalVariable;
            _type = type;
            _containingSymbol = containingSymbol;
        }

        internal override bool IsImportedFromMetadata
        {
            get { return _originalVariable.IsImportedFromMetadata; }
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return _originalVariable.DeclarationKind; }
        }

        internal override SynthesizedLocalKind SynthesizedKind
        {
            get { return _originalVariable.SynthesizedKind; }
        }

        internal override SyntaxNode ScopeDesignatorOpt
        {
            get { return _originalVariable.ScopeDesignatorOpt; }
        }

        public override string Name
        {
            get { return _originalVariable.Name; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return _originalVariable.DeclaringSyntaxReferences; }
        }

        internal override SyntaxNode GetDeclaratorSyntax()
        {
            return _originalVariable.GetDeclaratorSyntax();
        }

        public override ImmutableArray<Location> Locations
        {
            get { return _originalVariable.Locations; }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _type; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { return _originalVariable.IdentifierToken; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return _originalVariable.IsCompilerGenerated; }
        }

        internal override bool IsPinned
        {
            get { return _originalVariable.IsPinned; }
        }

        public override RefKind RefKind
        {
            get { return _originalVariable.RefKind; }
        }

        /// <summary>
        /// Compiler should always be synthesizing locals with correct escape semantics.
        /// Checking escape scopes is not valid here.
        /// </summary>
        internal override uint ValEscapeScope => throw ExceptionUtilities.Unreachable;

        /// <summary>
        /// Compiler should always be synthesizing locals with correct escape semantics.
        /// Checking escape scopes is not valid here.
        /// </summary>
        internal override uint RefEscapeScope => throw ExceptionUtilities.Unreachable;

        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, DiagnosticBag diagnostics)
        {
            return _originalVariable.GetConstantValue(node, inProgress, diagnostics);
        }

        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return _originalVariable.GetConstantValueDiagnostics(boundInitValue);
        }

        internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax)
        {
            var origSynthesized = (SynthesizedLocal)_originalVariable;
            return new TypeSubstitutedLocalSymbol(
                    origSynthesized.WithSynthesizedLocalKindAndSyntax(kind, syntax),
                    _type,
                    _containingSymbol
                );
        }
    }
}
