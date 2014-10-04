// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized local variable.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class SynthesizedLocal : LocalSymbol
    {
        private readonly MethodSymbol containingMethodOpt;
        private readonly TypeSymbol type;
        private readonly SynthesizedLocalKind kind;
        private readonly SyntaxNode syntaxOpt;
        private readonly bool isPinned;
        private readonly RefKind refKind;

#if DEBUG
        private readonly int createdAtLineNumber;
        private readonly string createdAtFilePath;
        
        internal SynthesizedLocal(
            MethodSymbol containingMethodOpt,
            TypeSymbol type,
            SynthesizedLocalKind kind,
            SyntaxNode syntaxOpt = null,
            bool isPinned = false,
            RefKind refKind = RefKind.None, 
            [CallerLineNumber]int createdAtLineNumber = 0, 
            [CallerFilePath]string createdAtFilePath = null)
        {
            Debug.Assert(type.SpecialType != SpecialType.System_Void);
            Debug.Assert(!kind.IsLongLived() || syntaxOpt != null);

            this.containingMethodOpt = containingMethodOpt;
            this.type = type;
            this.kind = kind;
            this.syntaxOpt = syntaxOpt;
            this.isPinned = isPinned;
            this.refKind = refKind;

            this.createdAtLineNumber = createdAtLineNumber;
            this.createdAtFilePath = createdAtFilePath;
        }
#else
        internal SynthesizedLocal(
            MethodSymbol containingMethodOpt,
            TypeSymbol type,
            SynthesizedLocalKind kind,
            SyntaxNode syntaxOpt = null,
            bool isPinned = false,
            RefKind refKind = RefKind.None)
        {
            this.containingMethodOpt = containingMethodOpt;
            this.type = type;
            this.kind = kind;
            this.syntaxOpt = syntaxOpt;
            this.isPinned = isPinned;
            this.refKind = refKind;
        }
#endif
        public SyntaxNode SyntaxOpt
        {
            get { return syntaxOpt; } 
        }

        internal SynthesizedLocal WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax)
        {
            return new SynthesizedLocal(
                this.containingMethodOpt,
                this.type,
                kind,
                syntax,
                this.isPinned, 
                this.refKind);
        }

        internal override RefKind RefKind
        {
            get { return this.refKind; }
        }

        internal override bool IsImportedFromMetadata
        {
            get { return false; }
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return LocalDeclarationKind.None; }
        }

        internal override SynthesizedLocalKind SynthesizedKind
        {
            get { return this.kind; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { return default(SyntaxToken); }
        }

        public override Symbol ContainingSymbol
        {
            get { return this.containingMethodOpt; }
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
            get { return (this.syntaxOpt == null) ? ImmutableArray<Location>.Empty : ImmutableArray.Create(this.syntaxOpt.GetLocation()); }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return (this.syntaxOpt == null) ? ImmutableArray<SyntaxReference>.Empty : ImmutableArray.Create(this.syntaxOpt.GetReference()); }
        }

        internal override SyntaxNode GetDeclaratorSyntax()
        {
            Debug.Assert(syntaxOpt != null);
            return this.syntaxOpt;
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
            var builder = new StringBuilder();
            builder.Append((this.kind == SynthesizedLocalKind.UserDefined) ? "<temp>" : this.kind.ToString());
            builder.Append(' ');
            builder.Append(this.type.ToDisplayString(SymbolDisplayFormat.TestFormat));

#if DEBUG
            builder.Append(" @");
            builder.Append(createdAtFilePath);
            builder.Append('(');
            builder.Append(createdAtLineNumber);
            builder.Append(')');
#endif

            return builder.ToString();
        }
    }
}