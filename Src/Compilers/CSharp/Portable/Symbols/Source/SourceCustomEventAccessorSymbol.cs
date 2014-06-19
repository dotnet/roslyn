// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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
        private readonly ImmutableArray<MethodSymbol> explicitInterfaceImplementations;
        private readonly string name;

        internal SourceCustomEventAccessorSymbol(
            SourceEventSymbol @event,
            AccessorDeclarationSyntax syntax,
            EventSymbol explicitlyImplementedEventOpt,
            string aliasQualifierOpt,
            DiagnosticBag diagnostics)
            : base(@event,
                   syntax.GetReference(),
                   syntax.Body.GetReferenceOrNull(),
                   ImmutableArray.Create(syntax.Keyword.GetLocation()))
        {
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.Kind == SyntaxKind.AddAccessorDeclaration || syntax.Kind == SyntaxKind.RemoveAccessorDeclaration);

            bool isAdder = syntax.Kind == SyntaxKind.AddAccessorDeclaration;

            string name;
            ImmutableArray<MethodSymbol> explicitInterfaceImplementations;
            if ((object)explicitlyImplementedEventOpt == null)
            {
                name = SourceEventSymbol.GetAccessorName(@event.Name, isAdder);
                explicitInterfaceImplementations = ImmutableArray<MethodSymbol>.Empty;
            }
            else
            {
                MethodSymbol implementedAccessor = isAdder ? explicitlyImplementedEventOpt.AddMethod : explicitlyImplementedEventOpt.RemoveMethod;
                string accessorName = (object)implementedAccessor != null ? implementedAccessor.Name : SourceEventSymbol.GetAccessorName(explicitlyImplementedEventOpt.Name, isAdder);

                name = ExplicitInterfaceHelpers.GetMemberName(accessorName, explicitlyImplementedEventOpt.ContainingType, aliasQualifierOpt);
                explicitInterfaceImplementations = (object)implementedAccessor == null ? ImmutableArray<MethodSymbol>.Empty : ImmutableArray.Create<MethodSymbol>(implementedAccessor);
            }

            this.explicitInterfaceImplementations = explicitInterfaceImplementations;
            this.name = name;
            this.flags = MakeFlags(
                isAdder ? MethodKind.EventAdd : MethodKind.EventRemove,
                @event.Modifiers,
                returnsVoid: false, // until we learn otherwise (in LazyMethodChecks).
                isExtensionMethod: false,
                isMetadataVirtualIgnoringModifiers: explicitInterfaceImplementations.Any());

            if (@event.ContainingType.IsInterface)
            {
                diagnostics.Add(ErrorCode.ERR_EventPropertyInInterface, this.Location);
            }
            else
            {
                var bodyOpt = syntax.Body;
                if (bodyOpt != null)
                {
                    if (IsExtern && !IsAbstract)
                    {
                        diagnostics.Add(ErrorCode.ERR_ExternHasBody, this.Location, this);
                    }
                    else if (IsAbstract && !IsExtern)
                    {
                        diagnostics.Add(ErrorCode.ERR_AbstractHasBody, this.Location, this);
                    }
                    // Do not report error for IsAbstract && IsExtern. Dev10 reports CS0180 only
                    // in that case ("member cannot be both extern and abstract").
                }
            }

            this.name = GetOverriddenAccessorName(@event, isAdder) ?? this.name;
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return this.AssociatedSymbol.DeclaredAccessibility; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return this.explicitInterfaceImplementations;
            }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return OneOrMany.Create(((AccessorDeclarationSyntax)this.SyntaxNode).AttributeLists);
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return false;
            }
        }
    }
}
