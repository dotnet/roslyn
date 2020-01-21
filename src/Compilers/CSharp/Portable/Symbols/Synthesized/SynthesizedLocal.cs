// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized local variable.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class SynthesizedLocal : LocalSymbol
    {
        private readonly MethodSymbol _containingMethodOpt;
        private readonly TypeWithAnnotations _type;
        private readonly SynthesizedLocalKind _kind;
        private readonly SyntaxNode _syntaxOpt;
        private readonly bool _isPinned;
        private readonly RefKind _refKind;

#if DEBUG
        private readonly int _createdAtLineNumber;
        private readonly string _createdAtFilePath;
#endif

        internal SynthesizedLocal(
            MethodSymbol containingMethodOpt,
            TypeWithAnnotations type,
            SynthesizedLocalKind kind,
            SyntaxNode syntaxOpt = null,
            bool isPinned = false,
            RefKind refKind = RefKind.None
#if DEBUG
            ,
            [CallerLineNumber]int createdAtLineNumber = 0,
            [CallerFilePath]string createdAtFilePath = null
#endif
            )
        {
            Debug.Assert(!type.IsVoidType());
            Debug.Assert(!kind.IsLongLived() || syntaxOpt != null);
            Debug.Assert(refKind != RefKind.Out);

            _containingMethodOpt = containingMethodOpt;
            _type = type;
            _kind = kind;
            _syntaxOpt = syntaxOpt;
            _isPinned = isPinned;
            _refKind = refKind;

#if DEBUG
            _createdAtLineNumber = createdAtLineNumber;
            _createdAtFilePath = createdAtFilePath;
#endif
        }

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

        public override RefKind RefKind
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

        internal override SyntaxNode ScopeDesignatorOpt
        {
            get { return null; }
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

        public override TypeWithAnnotations TypeWithAnnotations
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
            return null;
        }

        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return ImmutableArray<Diagnostic>.Empty;
        }

#if DEBUG
        private static int _nextSequence = 0;
        // Produce a token that helps distinguish one variable from another when debugging
        private int _sequence = System.Threading.Interlocked.Increment(ref _nextSequence);

        internal string DumperString()
        {
            var builder = new StringBuilder();
            builder.Append(_type.ToDisplayString(SymbolDisplayFormat.TestFormat));
            builder.Append(' ');
            builder.Append(_kind.ToString());
            builder.Append('.');
            builder.Append(_sequence);
            return builder.ToString();
        }
#endif

        override internal string GetDebuggerDisplay()
        {
            var builder = new StringBuilder();
            builder.Append('<');
            builder.Append(_kind.ToString());
            builder.Append('>');
#if DEBUG
            builder.Append('.');
            builder.Append(_sequence);
#endif
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
