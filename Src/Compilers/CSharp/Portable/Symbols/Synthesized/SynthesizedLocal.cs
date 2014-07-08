// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized local variable.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class SynthesizedLocal : LocalSymbol
    {
        private readonly MethodSymbol containingMethod;
        private readonly TypeSymbol type;
        private readonly CSharpSyntaxNode syntax;
        private readonly bool isPinned;
        private readonly RefKind refKind;
        private readonly SynthesizedLocalKind kind;

        internal SynthesizedLocal(
            MethodSymbol containingMethod,
            TypeSymbol type,
            SynthesizedLocalKind kind,
            CSharpSyntaxNode syntax = null,
            bool isPinned = false,
            RefKind refKind = RefKind.None)
        {
            this.containingMethod = containingMethod;
            Debug.Assert(type.SpecialType != SpecialType.System_Void);

            this.type = type;
            this.syntax = syntax;
            this.isPinned = isPinned;
            this.refKind = refKind;
            this.kind = kind;
        }

        internal override RefKind RefKind
        {
            get { return this.refKind; }
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return LocalDeclarationKind.None; }
        }

        internal override SynthesizedLocalKind SynthesizedLocalKind
        {
            get { return this.kind; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { return default(SyntaxToken); }
        }

        public override Symbol ContainingSymbol
        {
            get { return this.containingMethod; }
        }

        public override string Name
        {
            get { return null; }
        }

        public override TypeSymbol Type
        {
            get { return type; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return (this.syntax == null) ? ImmutableArray<Location>.Empty : ImmutableArray.Create(this.syntax.GetLocation()); }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return (this.syntax == null) ? ImmutableArray<SyntaxReference>.Empty : ImmutableArray.Create(this.syntax.GetReference()); }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override bool IsPinned
        {
            get { return this.isPinned; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return true; }
        }

        internal override ConstantValue GetConstantValue(LocalSymbol inProgress)
        {
            return null;
        }

        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return default(ImmutableArray<Diagnostic>);
        }

        private new string GetDebuggerDisplay()
        {
            return string.Format("{0} {1}",
                this.kind == SynthesizedLocalKind.None ? "<temp>" : this.kind.ToString(), 
                this.type.ToDisplayString(SymbolDisplayFormat.TestFormat));
        }
    }
}