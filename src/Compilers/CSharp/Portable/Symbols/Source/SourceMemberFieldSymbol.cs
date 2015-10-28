// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SourceMemberFieldSymbol : SourceFieldSymbolWithSyntaxReference
    {
        private readonly DeclarationModifiers _modifiers;
        private readonly bool _hasInitializer;

        private TypeSymbolWithAnnotations _lazyType;

        // Non-zero if the type of the field has been inferred from the type of its initializer expression
        // and the errors of binding the initializer have been or are being reported to compilation diagnostics.
        private int _lazyFieldTypeInferred;

        internal SourceMemberFieldSymbol(
            SourceMemberContainerTypeSymbol containingType,
            VariableDeclaratorSyntax declarator,
            DeclarationModifiers modifiers,
            bool modifierErrors,
            DiagnosticBag diagnostics)
            : base(containingType, declarator.Identifier.ValueText, declarator.GetReference(), declarator.Identifier.GetLocation())
        {
            _modifiers = modifiers;
            _hasInitializer = declarator.Initializer != null;

            this.CheckAccessibility(diagnostics);

            if (!modifierErrors)
            {
                this.ReportModifiersDiagnostics(diagnostics);
            }
        }

        protected sealed override DeclarationModifiers Modifiers
        {
            get
            {
                return _modifiers;
            }
        }

        private void TypeChecks(TypeSymbol type, BaseFieldDeclarationSyntax fieldSyntax, VariableDeclaratorSyntax declarator, DiagnosticBag diagnostics)
        {
            if (type.IsStatic)
            {
                // Cannot declare a variable of static type '{0}'
                diagnostics.Add(ErrorCode.ERR_VarDeclIsStaticClass, this.ErrorLocation, type);
            }
            else if (type.SpecialType == SpecialType.System_Void)
            {
                diagnostics.Add(ErrorCode.ERR_FieldCantHaveVoidType, fieldSyntax.Declaration.Type.Location);
            }
            else if (type.IsRestrictedType())
            {
                diagnostics.Add(ErrorCode.ERR_FieldCantBeRefAny, fieldSyntax.Declaration.Type.Location, type);
            }
            else if (IsConst && !type.CanBeConst())
            {
                SyntaxToken constToken = default(SyntaxToken);
                foreach (var modifier in fieldSyntax.Modifiers)
                {
                    if (modifier.Kind() == SyntaxKind.ConstKeyword)
                    {
                        constToken = modifier;
                        break;
                    }
                }
                Debug.Assert(constToken.Kind() == SyntaxKind.ConstKeyword);

                diagnostics.Add(ErrorCode.ERR_BadConstType, constToken.GetLocation(), type);
            }
            else if (IsVolatile && !type.IsValidVolatileFieldType())
            {
                // '{0}': a volatile field cannot be of the type '{1}'
                diagnostics.Add(ErrorCode.ERR_VolatileStruct, this.ErrorLocation, this, type);
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (!this.IsNoMoreVisibleThan(type, ref useSiteDiagnostics))
            {
                // Inconsistent accessibility: field type '{1}' is less accessible than field '{0}'
                diagnostics.Add(ErrorCode.ERR_BadVisFieldType, this.ErrorLocation, this, type);
            }

            diagnostics.Add(this.ErrorLocation, useSiteDiagnostics);
        }

        public bool HasInitializer
        {
            get { return _hasInitializer; }
        }

        public VariableDeclaratorSyntax VariableDeclaratorNode
        {
            get
            {
                return (VariableDeclaratorSyntax)this.SyntaxNode;
            }
        }

        private static BaseFieldDeclarationSyntax GetFieldDeclaration(CSharpSyntaxNode declarator)
        {
            return (BaseFieldDeclarationSyntax)declarator.Parent.Parent;
        }

        protected override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
        {
            get
            {
                if (this.containingType.AnyMemberHasAttributes)
                {
                    return GetFieldDeclaration(this.SyntaxNode).AttributeLists;
                }

                return default(SyntaxList<AttributeListSyntax>);
            }
        }

        internal override bool HasPointerType
        {
            get
            {
                if ((object)_lazyType != null)
                {
                    Debug.Assert(_lazyType.TypeSymbol.IsPointerType() ==
                        IsPointerFieldSyntactically());

                    return _lazyType.TypeSymbol.IsPointerType();
                }

                return IsPointerFieldSyntactically();
            }
        }

        private bool IsPointerFieldSyntactically()
        {
            var declaration = GetFieldDeclaration(VariableDeclaratorNode).Declaration;
            if (declaration.Type.Kind() == SyntaxKind.PointerType)
            {
                // public int * Blah;   // pointer
                return true;
            }

            foreach (var singleVariable in declaration.Variables)
            {
                var argList = singleVariable.ArgumentList;
                if (argList != null && argList.Arguments.Count != 0)
                {
                    // public int Blah[10];     // fixed buffer
                    return true;
                }
            }

            return false;
        }

        internal sealed override TypeSymbolWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            Debug.Assert(fieldsBeingBound != null);

            if ((object)_lazyType != null)
            {
                return _lazyType;
            }

            var declarator = VariableDeclaratorNode;
            var fieldSyntax = GetFieldDeclaration(declarator);
            var typeSyntax = fieldSyntax.Declaration.Type;

            var compilation = this.DeclaringCompilation;

            var diagnostics = DiagnosticBag.GetInstance();
            TypeSymbolWithAnnotations type;

            // When we have multiple declarators, we report the type diagnostics on only the first.
            DiagnosticBag diagnosticsForFirstDeclarator = DiagnosticBag.GetInstance();

            Symbol associatedPropertyOrEvent = this.AssociatedSymbol;
            if ((object)associatedPropertyOrEvent != null && associatedPropertyOrEvent.Kind == SymbolKind.Event)
            {
                EventSymbol @event = (EventSymbol)associatedPropertyOrEvent;
                if (@event.IsWindowsRuntimeEvent)
                {
                    NamedTypeSymbol tokenTableType = this.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T);
                    Binder.ReportUseSiteDiagnostics(tokenTableType, diagnosticsForFirstDeclarator, this.ErrorLocation);

                    // CONSIDER: Do we want to guard against the possibility that someone has created their own EventRegistrationTokenTable<T>
                    // type that has additional generic constraints?
                    type = TypeSymbolWithAnnotations.Create(tokenTableType.Construct(@event.Type.TypeSymbol));
                }
                else
                {
                    type = TypeSymbolWithAnnotations.Create(@event.Type.TypeSymbol);
                }
            }
            else
            {
                var binderFactory = compilation.GetBinderFactory(SyntaxTree);
                var binder = binderFactory.GetBinder(typeSyntax);

                binder = binder.WithContainingMemberOrLambda(this);
                if (!ContainingType.IsScriptClass)
                {
                    type = binder.BindType(typeSyntax, diagnosticsForFirstDeclarator);
                    if (IsFixed)
                    {
                        type = TypeSymbolWithAnnotations.Create(new PointerTypeSymbol(type));
                    }
                }
                else
                {
                    bool isVar;
                    type = binder.BindType(typeSyntax, diagnostics, out isVar);

                    Debug.Assert((object)type != null || isVar);

                    if (isVar)
                    {
                        if (this.IsConst)
                        {
                            diagnosticsForFirstDeclarator.Add(ErrorCode.ERR_ImplicitlyTypedVariableCannotBeConst, typeSyntax.Location);
                        }

                        if (fieldsBeingBound.ContainsReference(this))
                        {
                            diagnostics.Add(ErrorCode.ERR_RecursivelyTypedVariable, this.ErrorLocation, this);
                            type = null;
                        }
                        else if (fieldSyntax.Declaration.Variables.Count > 1)
                        {
                            diagnosticsForFirstDeclarator.Add(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, typeSyntax.Location);
                        }
                        else
                        {
                            fieldsBeingBound = new ConsList<FieldSymbol>(this, fieldsBeingBound);

                            var initializerBinder = new ImplicitlyTypedFieldBinder(binder, fieldsBeingBound);
                            var initializerOpt = initializerBinder.BindInferredVariableInitializer(diagnostics, declarator.Initializer, declarator);

                            if (initializerOpt != null)
                            {
                                if ((object)initializerOpt.Type != null && !initializerOpt.Type.IsErrorType())
                                {
                                    type = TypeSymbolWithAnnotations.Create(initializerOpt.Type);
                                }

                                _lazyFieldTypeInferred = 1;
                            }
                        }

                        if ((object)type == null)
                        {
                            type = TypeSymbolWithAnnotations.Create(binder.CreateErrorType("var"));
                        }
                    }
                }

                if (IsFixed)
                {
                    if (ContainingType.TypeKind != TypeKind.Struct)
                    {
                        diagnostics.Add(ErrorCode.ERR_FixedNotInStruct, ErrorLocation);
                    }

                    var elementType = ((PointerTypeSymbol)type.TypeSymbol).PointedAtType.TypeSymbol;
                    int elementSize = elementType.FixedBufferElementSizeInBytes();
                    if (elementSize == 0)
                    {
                        var loc = typeSyntax.Location;
                        diagnostics.Add(ErrorCode.ERR_IllegalFixedType, loc);
                    }

                    if (!binder.InUnsafeRegion)
                    {
                        diagnosticsForFirstDeclarator.Add(ErrorCode.ERR_UnsafeNeeded, declarator.Location);
                    }
                }
            }

            // update the lazyType only if it contains value last seen by the current thread:
            if ((object)Interlocked.CompareExchange(ref _lazyType, type.WithModifiers(this.RequiredCustomModifiers), null) == null)
            {
                TypeChecks(type.TypeSymbol, fieldSyntax, declarator, diagnostics);

                // CONSIDER: SourceEventFieldSymbol would like to suppress these diagnostics.
                compilation.DeclarationDiagnostics.AddRange(diagnostics);

                bool isFirstDeclarator = fieldSyntax.Declaration.Variables[0] == declarator;
                if (isFirstDeclarator)
                {
                    compilation.DeclarationDiagnostics.AddRange(diagnosticsForFirstDeclarator);
                }

                state.NotePartComplete(CompletionPart.Type);
            }

            diagnostics.Free();
            diagnosticsForFirstDeclarator.Free();
            return _lazyType;
        }

        internal bool FieldTypeInferred(ConsList<FieldSymbol> fieldsBeingBound)
        {
            if (!ContainingType.IsScriptClass)
            {
                return false;
            }

            GetFieldType(fieldsBeingBound);

            // lazyIsImplicitlyTypedField can only transition from value 0 to 1:
            return _lazyFieldTypeInferred != 0 || Volatile.Read(ref _lazyFieldTypeInferred) != 0;
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            var compilation = this.DeclaringCompilation;
            var value = this.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);

            // Synthesize DecimalConstantAttribute when the default value is of type decimal
            if (this.IsConst && value != null
                && this.Type.SpecialType == SpecialType.System_Decimal)
            {
                var data = GetDecodedWellKnownAttributeData();

                if (data == null || data.ConstValue == CodeAnalysis.ConstantValue.Unset)
                {
                    AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDecimalConstantAttribute(value.DecimalValue));
                }
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        protected sealed override ConstantValue MakeConstantValue(HashSet<SourceFieldSymbolWithSyntaxReference> dependencies, bool earlyDecodingWellKnownAttributes, DiagnosticBag diagnostics)
        {
            EqualsValueClauseSyntax initializer;
            return !this.IsConst || ((initializer = VariableDeclaratorNode.Initializer) == null)
                ? null
                : ConstantValueUtils.EvaluateFieldConstant(this, initializer, dependencies, earlyDecodingWellKnownAttributes, diagnostics);
        }

        public override int FixedSize
        {
            get
            {
                Debug.Assert(!this.IsFixed, "Subclasses representing fixed fields must override");
                if (state.NotePartComplete(CompletionPart.FixedSize))
                {
                    // FixedSize is the last completion part for fields.
                    DeclaringCompilation.SymbolDeclaredEvent(this);
                }

                return 0;
            }
        }

        internal static DeclarationModifiers MakeModifiers(NamedTypeSymbol containingType, SyntaxToken firstIdentifier, SyntaxTokenList modifiers, DiagnosticBag diagnostics, out bool modifierErrors)
        {
            DeclarationModifiers defaultAccess =
                (containingType.IsInterface) ? DeclarationModifiers.Public : DeclarationModifiers.Private;

            DeclarationModifiers allowedModifiers =
                DeclarationModifiers.AccessibilityMask |
                DeclarationModifiers.Const |
                DeclarationModifiers.New |
                DeclarationModifiers.ReadOnly |
                DeclarationModifiers.Static |
                DeclarationModifiers.Volatile |
                DeclarationModifiers.Fixed |
                DeclarationModifiers.Unsafe |
                DeclarationModifiers.Abstract; // filtered out later

            var errorLocation = new SourceLocation(firstIdentifier);
            DeclarationModifiers result = ModifierUtils.MakeAndCheckNontypeMemberModifiers(
                modifiers, defaultAccess, allowedModifiers, errorLocation, diagnostics, out modifierErrors);

            if ((result & DeclarationModifiers.Abstract) != 0)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractField, errorLocation);
                result &= ~DeclarationModifiers.Abstract;
            }

            if ((result & DeclarationModifiers.Fixed) != 0)
            {
                if ((result & DeclarationModifiers.Static) != 0)
                {
                    // The modifier 'static' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.StaticKeyword));
                }

                if ((result & DeclarationModifiers.ReadOnly) != 0)
                {
                    // The modifier 'readonly' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.ReadOnlyKeyword));
                }

                if ((result & DeclarationModifiers.Const) != 0)
                {
                    // The modifier 'const' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.ConstKeyword));
                }

                if ((result & DeclarationModifiers.Volatile) != 0)
                {
                    // The modifier 'volatile' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.VolatileKeyword));
                }

                result &= ~(DeclarationModifiers.Static | DeclarationModifiers.ReadOnly | DeclarationModifiers.Const | DeclarationModifiers.Volatile);
                Debug.Assert((result & ~(DeclarationModifiers.AccessibilityMask | DeclarationModifiers.Fixed | DeclarationModifiers.Unsafe | DeclarationModifiers.New)) == 0);
            }


            if ((result & DeclarationModifiers.Const) != 0)
            {
                if ((result & DeclarationModifiers.Static) != 0)
                {
                    // The constant '{0}' cannot be marked static
                    diagnostics.Add(ErrorCode.ERR_StaticConstant, errorLocation, firstIdentifier.ValueText);
                }

                if ((result & DeclarationModifiers.ReadOnly) != 0)
                {
                    // The modifier 'readonly' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.ReadOnlyKeyword));
                }

                if ((result & DeclarationModifiers.Volatile) != 0)
                {
                    // The modifier 'volatile' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.VolatileKeyword));
                }

                if ((result & DeclarationModifiers.Unsafe) != 0)
                {
                    // The modifier 'unsafe' is not valid for this item
                    diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, SyntaxFacts.GetText(SyntaxKind.UnsafeKeyword));
                }

                result |= DeclarationModifiers.Static; // "constants are considered static members"
            }
            else
            {
                // NOTE: always cascading on a const, so suppress.
                // NOTE: we're being a bit sneaky here - we're using the containingType rather than this symbol
                // to determine whether or not unsafe is allowed.  Since this symbol and the containing type are
                // in the same compilation, it won't make a difference.  We do, however, have to pass the error
                // location explicitly.
                containingType.CheckUnsafeModifier(result, errorLocation, diagnostics);
            }

            return result;
        }

        internal sealed override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.Attributes:
                        GetAttributes();
                        break;

                    case CompletionPart.Type:
                        GetFieldType(ConsList<FieldSymbol>.Empty);
                        break;

                    case CompletionPart.ConstantValue:
                        GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);
                        break;

                    case CompletionPart.FixedSize:
                        int discarded = this.FixedSize;
                        break;

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        state.NotePartComplete(CompletionPart.All & ~CompletionPart.FieldSymbolAll);
                        break;
                }

                state.SpinWaitComplete(incompletePart, cancellationToken);
            }
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            Debug.Assert(!this.IsFixed, "Subclasses representing fixed fields must override");
            return null;
        }

        internal override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.SyntaxTree == tree)
            {
                if (!definedWithinSpan.HasValue)
                {
                    return true;
                }

                var fieldDeclaration = GetFieldDeclaration(this.SyntaxNode);
                return fieldDeclaration.SyntaxTree.HasCompilationUnitRoot && fieldDeclaration.Span.IntersectsWith(definedWithinSpan.Value);
            }

            return false;
        }
    }
}
