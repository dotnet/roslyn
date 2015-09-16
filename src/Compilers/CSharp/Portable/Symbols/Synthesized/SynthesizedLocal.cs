// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly MethodSymbol _containingMethodOpt;
        private readonly TypeSymbol _type;
        private readonly SynthesizedLocalKind _kind;
        private readonly SyntaxNode _syntaxOpt;
        private readonly bool _isPinned;
        private readonly RefKind _refKind;

#if DEBUG
        private readonly int _createdAtLineNumber;
        private readonly string _createdAtFilePath;

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

            _containingMethodOpt = containingMethodOpt;
            _type = type;
            _kind = kind;
            _syntaxOpt = syntaxOpt;
            _isPinned = isPinned;
            _refKind = refKind;

            _createdAtLineNumber = createdAtLineNumber;
            _createdAtFilePath = createdAtFilePath;
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
            _containingMethodOpt = containingMethodOpt;
            _type = type;
            _kind = kind;
            _syntaxOpt = syntaxOpt;
            _isPinned = isPinned;
            _refKind = refKind;
        }
#endif
        public SyntaxNode SyntaxOpt
        {
            get { return _syntaxOpt; }
        }

        internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax)
        {
            return new SynthesizedLocal(
                _containingMethodOpt,
                _type,
                kind,
                syntax,
                _isPinned,
                _refKind);
        }

        internal override RefKind RefKind
        {
            get { return _refKind; }
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
            get { return _kind; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { return default(SyntaxToken); }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingMethodOpt; }
        }

        public override string Name
        {
            get { return null; }
        }

        public override TypeSymbol Type
        {
            get { return _type; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return (_syntaxOpt == null) ? ImmutableArray<Location>.Empty : ImmutableArray.Create(_syntaxOpt.GetLocation()); }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return (_syntaxOpt == null) ? ImmutableArray<SyntaxReference>.Empty : ImmutableArray.Create(_syntaxOpt.GetReference()); }
        }

        internal override SyntaxNode GetDeclaratorSyntax()
        {
            Debug.Assert(_syntaxOpt != null);
            return _syntaxOpt;
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override bool IsPinned
        {
            get { return _isPinned; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return true; }
        }

        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, DiagnosticBag diagnostics)
        {
            return null;
        }

        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return ImmutableArray<Diagnostic>.Empty;
        }

        private new string GetDebuggerDisplay()
        {
            var builder = new StringBuilder();
            builder.Append((_kind == SynthesizedLocalKind.UserDefined) ? "<temp>" : _kind.ToString());
            builder.Append(' ');
            builder.Append(_type.ToDisplayString(SymbolDisplayFormat.TestFormat));

#if DEBUG
            builder.Append(" @");
            builder.Append(_createdAtFilePath);
            builder.Append('(');
            builder.Append(_createdAtLineNumber);
            builder.Append(')');
#endif

            return builder.ToString();
        }
    }
}
