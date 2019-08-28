// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This class represents an event accessor declared in source 
    /// (i.e. not one synthesized for a field-like event).
    /// </summary>
    /// <remarks>
    /// The accessors are associated with <see cref="SourceCustomEventSymbol"/>.
    /// </remarks>
    internal sealed class SourceCustomEventAccessorSymbol : SourceEventAccessorSymbol
    {
        internal SourceCustomEventAccessorSymbol(
            SourceEventSymbol @event,
            AccessorDeclarationSyntax syntax,
            EventSymbol explicitlyImplementedEventOpt,
            string aliasQualifierOpt,
            DiagnosticBag diagnostics)
            : base(@event,
                   syntax.GetReference(),
                   ImmutableArray.Create(syntax.Keyword.GetLocation()), explicitlyImplementedEventOpt, aliasQualifierOpt, isAdder: syntax.Kind() == SyntaxKind.AddAccessorDeclaration)
        {
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.Kind() == SyntaxKind.AddAccessorDeclaration || syntax.Kind() == SyntaxKind.RemoveAccessorDeclaration);

            CheckFeatureAvailabilityAndRuntimeSupport(syntax, this.Location, hasBody: true, diagnostics: diagnostics);

            if (syntax.Body != null || syntax.ExpressionBody != null)
            {
                if (IsExtern && !IsAbstract)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasBody, this.Location, this);
                }
                // Do not report error for IsAbstract && IsExtern. Dev10 reports CS0180 only
                // in that case ("member cannot be both extern and abstract").
            }

            if (syntax.Modifiers.Count > 0)
            {
                diagnostics.Add(ErrorCode.ERR_NoModifiersOnAccessor, syntax.Modifiers[0].GetLocation());
            }

            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);
        }

        internal AccessorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (AccessorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return this.AssociatedSymbol.DeclaredAccessibility;
            }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return OneOrMany.Create(GetSyntax().AttributeLists);
        }

        public override bool IsImplicitlyDeclared
        {
            get { return false; }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }

        internal override bool IsExpressionBodied
        {
            get
            {
                var syntax = GetSyntax();
                var hasBody = syntax.Body != null;
                var hasExpressionBody = syntax.ExpressionBody != null;
                return !hasBody && hasExpressionBody;
            }
        }
    }
}
