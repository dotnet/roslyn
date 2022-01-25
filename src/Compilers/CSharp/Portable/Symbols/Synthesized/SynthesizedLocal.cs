﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal class SynthesizedLocal : LocalSymbol
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
            [CallerLineNumber] int createdAtLineNumber = 0,
            [CallerFilePath] string createdAtFilePath = null
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

        internal sealed override LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax)
        {
            return new SynthesizedLocal(
                _containingMethodOpt,
                _type,
                kind,
                syntax,
                _isPinned,
                _refKind);
        }

        public sealed override RefKind RefKind
        {
            get { return _refKind; }
        }

        internal sealed override bool IsImportedFromMetadata
        {
            get { return false; }
        }

        internal sealed override LocalDeclarationKind DeclarationKind
        {
            get { return LocalDeclarationKind.None; }
        }

        internal sealed override SynthesizedLocalKind SynthesizedKind
        {
            get { return _kind; }
        }

        internal sealed override SyntaxNode ScopeDesignatorOpt
        {
            get { return null; }
        }

        internal sealed override SyntaxToken IdentifierToken
        {
            get { return default(SyntaxToken); }
        }

        public sealed override Symbol ContainingSymbol
        {
            get { return _containingMethodOpt; }
        }

        public sealed override string Name
        {
            get { return null; }
        }

        public sealed override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _type; }
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get { return (_syntaxOpt == null) ? ImmutableArray<Location>.Empty : ImmutableArray.Create(_syntaxOpt.GetLocation()); }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return (_syntaxOpt == null) ? ImmutableArray<SyntaxReference>.Empty : ImmutableArray.Create(_syntaxOpt.GetReference()); }
        }

        internal sealed override SyntaxNode GetDeclaratorSyntax()
        {
            Debug.Assert(_syntaxOpt != null);
            return _syntaxOpt;
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal sealed override bool IsPinned
        {
            get { return _isPinned; }
        }

        internal sealed override bool IsCompilerGenerated
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
        internal sealed override uint RefEscapeScope => throw ExceptionUtilities.Unreachable;

        internal sealed override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, BindingDiagnosticBag diagnostics)
        {
            return null;
        }

        internal sealed override ImmutableBindingDiagnostic<AssemblySymbol> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return ImmutableBindingDiagnostic<AssemblySymbol>.Empty;
        }

#if DEBUG
        private static int _nextSequence = 0;
        // Produce a token that helps distinguish one variable from another when debugging
        private readonly int _sequence = System.Threading.Interlocked.Increment(ref _nextSequence);

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

        internal sealed override string GetDebuggerDisplay()
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
