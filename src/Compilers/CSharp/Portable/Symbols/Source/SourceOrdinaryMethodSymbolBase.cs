﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Unlike <see cref="SourceOrdinaryMethodSymbol"/>, this type doesn't depend
    /// on any specific kind of syntax node associated with it. Any syntax node is good enough
    /// for it.
    /// </summary>
    internal abstract class SourceOrdinaryMethodSymbolBase : SourceOrdinaryMethodOrUserDefinedOperatorSymbol
    {
        private readonly string _name;

        protected SourceOrdinaryMethodSymbolBase(
            NamedTypeSymbol containingType,
            string name,
            Location location,
            CSharpSyntaxNode syntax,
            bool isIterator,
            (DeclarationModifiers declarationModifiers, Flags flags) modifiersAndFlags) :
            base(containingType,
                 syntax.GetReference(),
                 location,
                 isIterator: isIterator,
                 modifiersAndFlags)
        {
            _name = name;
        }

        protected sealed override void LazyAsyncMethodChecks(CancellationToken cancellationToken)
        {
            Debug.Assert(this.IsPartial == state.HasComplete(CompletionPart.FinishMethodChecks),
                "Partial methods complete method checks during construction.  " +
                "Other methods can't complete method checks before executing this method.");

            if (!this.IsAsync)
            {
                CompleteAsyncMethodChecks(diagnosticsOpt: null, cancellationToken: cancellationToken);
                return;
            }

            var diagnostics = BindingDiagnosticBag.GetInstance();
            AsyncMethodChecks(diagnostics);

            CompleteAsyncMethodChecks(diagnostics, cancellationToken);
            diagnostics.Free();
        }

        private void CompleteAsyncMethodChecks(BindingDiagnosticBag diagnosticsOpt, CancellationToken cancellationToken)
        {
            if (state.NotePartComplete(CompletionPart.StartAsyncMethodChecks))
            {
                if (diagnosticsOpt != null)
                {
                    AddDeclarationDiagnostics(diagnosticsOpt);
                }

                CompleteAsyncMethodChecksBetweenStartAndFinish();
                state.NotePartComplete(CompletionPart.FinishAsyncMethodChecks);
            }
            else
            {
                state.SpinWaitComplete(CompletionPart.FinishAsyncMethodChecks, cancellationToken);
            }
        }

        protected abstract void CompleteAsyncMethodChecksBetweenStartAndFinish();

        public abstract override ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

        public abstract override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken));

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        protected abstract override SourceMemberMethodSymbol BoundAttributesSource { get; }

        internal abstract override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations();

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            if (this.IsExtensionMethod)
            {
                // No need to check if [Extension] attribute was explicitly set since
                // we'll issue CS1112 error in those cases and won't generate IL.
                var compilation = this.DeclaringCompilation;

                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor));
            }
        }
    }
}
