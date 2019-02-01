﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SymbolDisplayVisitor
    {
        private const string IL_KEYWORD_MODOPT = "modopt";
        private const string IL_KEYWORD_MODREQ = "modreq";

        private void VisitFieldType(IFieldSymbol symbol)
        {
            var fieldSymbol = symbol as FieldSymbol;
            if ((object)fieldSymbol == null)
            {
                symbol.Type.Accept(this.NotFirstVisitor);
            }
            else
            {
                VisitTypeSymbolWithAnnotations(fieldSymbol.Type);
            }
        }

        public override void VisitField(IFieldSymbol symbol)
        {
            AddAccessibilityIfRequired(symbol);
            AddMemberModifiersIfRequired(symbol);
            AddFieldModifiersIfRequired(symbol);

            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) &&
                this.isFirstSymbolVisited &&
                !IsEnumMember(symbol))
            {
                VisitFieldType(symbol);
                AddSpace();

                AddCustomModifiersIfRequired(symbol.CustomModifiers);
            }

            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) &&
                IncludeNamedType(symbol.ContainingType))
            {
                symbol.ContainingType.Accept(this.NotFirstVisitor);
                AddPunctuation(SyntaxKind.DotToken);
            }

            if (symbol.ContainingType.TypeKind == TypeKind.Enum)
            {
                builder.Add(CreatePart(SymbolDisplayPartKind.EnumMemberName, symbol, symbol.Name));
            }
            else if (symbol.IsConst)
            {
                builder.Add(CreatePart(SymbolDisplayPartKind.ConstantName, symbol, symbol.Name));
            }
            else
            {
                builder.Add(CreatePart(SymbolDisplayPartKind.FieldName, symbol, symbol.Name));
            }

            if (this.isFirstSymbolVisited &&
                format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeConstantValue) &&
                symbol.IsConst &&
                symbol.HasConstantValue &&
                CanAddConstant(symbol.Type, symbol.ConstantValue))
            {
                AddSpace();
                AddPunctuation(SyntaxKind.EqualsToken);
                AddSpace();

                AddConstantValue(symbol.Type, symbol.ConstantValue, preferNumericValueOrExpandedFlagsForEnum: IsEnumMember(symbol));
            }
        }

        public override void VisitProperty(IPropertySymbol symbol)
        {
            AddAccessibilityIfRequired(symbol);
            AddMemberModifiersIfRequired(symbol);

            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType))
            {
                if (symbol.ReturnsByRef)
                {
                    AddRefIfRequired();
                }
                else if (symbol.ReturnsByRefReadonly)
                {
                    AddRefReadonlyIfRequired();
                }

                AddCustomModifiersIfRequired(symbol.RefCustomModifiers);

                var propertySymbol = symbol as PropertySymbol;
                if ((object)propertySymbol == null)
                {
                    symbol.Type.Accept(this.NotFirstVisitor);
                }
                else
                {
                    VisitTypeSymbolWithAnnotations(propertySymbol.Type);
                }

                AddSpace();

                AddCustomModifiersIfRequired(symbol.TypeCustomModifiers);
            }

            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) &&
                IncludeNamedType(symbol.ContainingType))
            {
                symbol.ContainingType.Accept(this.NotFirstVisitor);
                AddPunctuation(SyntaxKind.DotToken);
            }

            AddPropertyNameAndParameters(symbol);

            if (format.PropertyStyle == SymbolDisplayPropertyStyle.ShowReadWriteDescriptor)
            {
                AddSpace();
                AddPunctuation(SyntaxKind.OpenBraceToken);

                AddAccessor(symbol, symbol.GetMethod, SyntaxKind.GetKeyword);
                AddAccessor(symbol, symbol.SetMethod, SyntaxKind.SetKeyword);

                AddSpace();
                AddPunctuation(SyntaxKind.CloseBraceToken);
            }
        }

        private void AddPropertyNameAndParameters(IPropertySymbol symbol)
        {
            bool getMemberNameWithoutInterfaceName = symbol.Name.LastIndexOf('.') > 0;

            if (getMemberNameWithoutInterfaceName)
            {
                AddExplicitInterfaceIfRequired(symbol.ExplicitInterfaceImplementations);
            }

            if (symbol.IsIndexer)
            {
                AddKeyword(SyntaxKind.ThisKeyword);
            }
            else if (getMemberNameWithoutInterfaceName)
            {
                this.builder.Add(CreatePart(SymbolDisplayPartKind.PropertyName, symbol,
                    ExplicitInterfaceHelpers.GetMemberNameWithoutInterfaceName(symbol.Name)));
            }
            else
            {
                this.builder.Add(CreatePart(SymbolDisplayPartKind.PropertyName, symbol, symbol.Name));
            }

            if (this.format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters) && symbol.Parameters.Any())
            {
                AddPunctuation(SyntaxKind.OpenBracketToken);
                AddParametersIfRequired(hasThisParameter: false, isVarargs: false, parameters: symbol.Parameters);
                AddPunctuation(SyntaxKind.CloseBracketToken);
            }
        }

        public override void VisitEvent(IEventSymbol symbol)
        {
            AddAccessibilityIfRequired(symbol);
            AddMemberModifiersIfRequired(symbol);

            if (format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeMemberKeyword))
            {
                AddKeyword(SyntaxKind.EventKeyword);
                AddSpace();
            }

            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType))
            {
                var eventSymbol = symbol as EventSymbol;

                if ((object)eventSymbol == null)
                {
                    symbol.Type.Accept(this.NotFirstVisitor);
                }
                else
                {
                    VisitTypeSymbolWithAnnotations(eventSymbol.Type);
                }

                AddSpace();
            }

            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) &&
                IncludeNamedType(symbol.ContainingType))
            {
                symbol.ContainingType.Accept(this.NotFirstVisitor);
                AddPunctuation(SyntaxKind.DotToken);
            }

            AddEventName(symbol);
        }

        private void AddEventName(IEventSymbol symbol)
        {
            if (symbol.Name.LastIndexOf('.') > 0)
            {
                AddExplicitInterfaceIfRequired(symbol.ExplicitInterfaceImplementations);

                this.builder.Add(CreatePart(SymbolDisplayPartKind.EventName, symbol,
                    ExplicitInterfaceHelpers.GetMemberNameWithoutInterfaceName(symbol.Name)));
            }
            else
            {
                this.builder.Add(CreatePart(SymbolDisplayPartKind.EventName, symbol, symbol.Name));
            }
        }

        public override void VisitMethod(IMethodSymbol symbol)
        {
            if (symbol.MethodKind == MethodKind.AnonymousFunction)
            {
                // TODO(cyrusn): Why is this a literal?  Why don't we give the appropriate signature
                // of the method as asked?
                builder.Add(CreatePart(SymbolDisplayPartKind.NumericLiteral, symbol, "lambda expression"));
                return;
            }
            else if (symbol is SynthesizedGlobalMethodSymbol) // It would be nice to handle VB symbols too, but it's not worth the effort.
            {
                // Represents a compiler generated synthesized method symbol with a null containing
                // type.

                // TODO(cyrusn); Why is this a literal?
                builder.Add(CreatePart(SymbolDisplayPartKind.NumericLiteral, symbol, symbol.Name));
                return;
            }

            if (symbol.IsExtensionMethod && format.ExtensionMethodStyle != SymbolDisplayExtensionMethodStyle.Default)
            {
                if (symbol.MethodKind == MethodKind.ReducedExtension && format.ExtensionMethodStyle == SymbolDisplayExtensionMethodStyle.StaticMethod)
                {
                    symbol = symbol.GetConstructedReducedFrom();
                }
                else if (symbol.MethodKind != MethodKind.ReducedExtension && format.ExtensionMethodStyle == SymbolDisplayExtensionMethodStyle.InstanceMethod)
                {
                    // If we cannot reduce this to an instance form then display in the static form
                    symbol = symbol.ReduceExtensionMethod(symbol.Parameters.First().Type) ?? symbol;
                }
            }

            // Method members always have a type unless (1) this is a lambda method symbol, which we 
            // have dealt with already, or (2) this is an error method symbol. If we have an error method
            // symbol then we do not know its accessibility, modifiers, etc, all of which require knowing
            // the containing type, so we'll skip them.

            if ((object)symbol.ContainingType != null || (symbol.ContainingSymbol is ITypeSymbol))
            {
                AddAccessibilityIfRequired(symbol);
                AddMemberModifiersIfRequired(symbol);

                if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType))
                {
                    switch (symbol.MethodKind)
                    {
                        case MethodKind.Constructor:
                        case MethodKind.StaticConstructor:
                            break;
                        case MethodKind.Destructor:
                        case MethodKind.Conversion:
                            // If we're using the metadata format, then include the return type.  
                            // Otherwise we eschew it since it is redundant in an conversion
                            // signature.
                            if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames))
                            {
                                goto default;
                            }

                            break;
                        default:
                            // The display code is called by the debugger; if a developer is debugging Roslyn and attempts
                            // to visualize a symbol *during its construction*, the parameters and return type might 
                            // still be null. 

                            if (symbol.ReturnsByRef)
                            {
                                AddRefIfRequired();
                            }
                            else if (symbol.ReturnsByRefReadonly)
                            {
                                AddRefReadonlyIfRequired();
                            }

                            AddCustomModifiersIfRequired(symbol.RefCustomModifiers);

                            if (symbol.ReturnsVoid)
                            {
                                AddKeyword(SyntaxKind.VoidKeyword);
                            }
                            else if (symbol.ReturnType != null)
                            {
                                AddReturnType(symbol);
                            }

                            AddSpace();
                            AddCustomModifiersIfRequired(symbol.ReturnTypeCustomModifiers);
                            break;
                    }
                }

                if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType))
                {
                    ITypeSymbol containingType;
                    bool includeType;

                    if (symbol.MethodKind == MethodKind.LocalFunction)
                    {
                        includeType = false;
                        containingType = null;
                    }
                    else if (symbol.MethodKind == MethodKind.ReducedExtension)
                    {
                        containingType = symbol.ReceiverType;
                        includeType = true;
                        Debug.Assert(containingType != null);
                    }
                    else
                    {
                        containingType = symbol.ContainingType;

                        if ((object)containingType != null)
                        {
                            includeType = IncludeNamedType(symbol.ContainingType);
                        }
                        else
                        {
                            containingType = (ITypeSymbol)symbol.ContainingSymbol;
                            includeType = true;
                        }
                    }

                    if (includeType)
                    {
                        containingType.Accept(this.NotFirstVisitor);
                        AddPunctuation(SyntaxKind.DotToken);
                    }
                }
            }

            bool isAccessor = false;
            switch (symbol.MethodKind)
            {
                case MethodKind.Ordinary:
                case MethodKind.DelegateInvoke:
                case MethodKind.LocalFunction:
                    {
                        //containing type will be the delegate type, name will be Invoke
                        builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name));
                        break;
                    }
                case MethodKind.ReducedExtension:
                    {
                        // Note: Extension methods invoked off of their static class will be tagged as methods.
                        //       This behavior matches the semantic classification done in NameSyntaxClassifier.
                        builder.Add(CreatePart(SymbolDisplayPartKind.ExtensionMethodName, symbol, symbol.Name));
                        break;
                    }
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    {
                        isAccessor = true;
                        var associatedProperty = (IPropertySymbol)symbol.AssociatedSymbol;
                        if (associatedProperty == null)
                        {
                            goto case MethodKind.Ordinary;
                        }
                        AddPropertyNameAndParameters(associatedProperty);
                        AddPunctuation(SyntaxKind.DotToken);
                        AddKeyword(symbol.MethodKind == MethodKind.PropertyGet ? SyntaxKind.GetKeyword : SyntaxKind.SetKeyword);
                        break;
                    }
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                    {
                        isAccessor = true;
                        var associatedEvent = (IEventSymbol)symbol.AssociatedSymbol;
                        if (associatedEvent == null)
                        {
                            goto case MethodKind.Ordinary;
                        }
                        AddEventName(associatedEvent);
                        AddPunctuation(SyntaxKind.DotToken);
                        AddKeyword(symbol.MethodKind == MethodKind.EventAdd ? SyntaxKind.AddKeyword : SyntaxKind.RemoveKeyword);
                        break;
                    }
                case MethodKind.Constructor:
                case MethodKind.StaticConstructor:
                    {
                        // Note: we are using the metadata name also in the case that
                        // symbol.containingType is null (which should never be the case here) or is an
                        //       anonymous type (which 'does not have a name').
                        var name = format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) || symbol.ContainingType == null || symbol.ContainingType.IsAnonymousType
                            ? symbol.Name
                            : symbol.ContainingType.Name;

                        var partKind = GetPartKindForConstructorOrDestructor(symbol);

                        builder.Add(CreatePart(partKind, symbol, name));
                        break;
                    }
                case MethodKind.Destructor:
                    {
                        var partKind = GetPartKindForConstructorOrDestructor(symbol);

                        // Note: we are using the metadata name also in the case that symbol.containingType is null, which should never be the case here.
                        if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) || symbol.ContainingType == null)
                        {
                            builder.Add(CreatePart(partKind, symbol, symbol.Name));
                        }
                        else
                        {
                            AddPunctuation(SyntaxKind.TildeToken);
                            builder.Add(CreatePart(partKind, symbol, symbol.ContainingType.Name));
                        }
                        break;
                    }
                case MethodKind.ExplicitInterfaceImplementation:
                    {
                        AddExplicitInterfaceIfRequired(symbol.ExplicitInterfaceImplementations);
                        builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol,
                            ExplicitInterfaceHelpers.GetMemberNameWithoutInterfaceName(symbol.Name)));
                        break;
                    }
                case MethodKind.UserDefinedOperator:
                case MethodKind.BuiltinOperator:
                    {
                        if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames))
                        {
                            builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.MetadataName));
                        }
                        else
                        {
                            AddKeyword(SyntaxKind.OperatorKeyword);
                            AddSpace();
                            if (symbol.MetadataName == WellKnownMemberNames.TrueOperatorName)
                            {
                                AddKeyword(SyntaxKind.TrueKeyword);
                            }
                            else if (symbol.MetadataName == WellKnownMemberNames.FalseOperatorName)
                            {
                                AddKeyword(SyntaxKind.FalseKeyword);
                            }
                            else
                            {
                                builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol,
                                    SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(symbol.MetadataName))));
                            }
                        }
                        break;
                    }
                case MethodKind.Conversion:
                    {
                        if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames))
                        {
                            builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.MetadataName));
                        }
                        else
                        {
                            // "System.IntPtr.explicit operator System.IntPtr(int)"

                            if (symbol.MetadataName == WellKnownMemberNames.ExplicitConversionName)
                            {
                                AddKeyword(SyntaxKind.ExplicitKeyword);
                            }
                            else if (symbol.MetadataName == WellKnownMemberNames.ImplicitConversionName)
                            {
                                AddKeyword(SyntaxKind.ImplicitKeyword);
                            }
                            else
                            {
                                builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol,
                                    SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(symbol.MetadataName))));
                            }

                            AddSpace();
                            AddKeyword(SyntaxKind.OperatorKeyword);
                            AddSpace();
                            AddReturnType(symbol);
                        }
                        break;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.MethodKind);
            }

            if (!isAccessor)
            {
                AddTypeArguments(symbol, default(ImmutableArray<ImmutableArray<CustomModifier>>));
                AddParameters(symbol);
                AddTypeParameterConstraints(symbol);
            }
        }

        private static SymbolDisplayPartKind GetPartKindForConstructorOrDestructor(IMethodSymbol symbol)
        {
            // In the case that symbol.containingType is null (which should never be the case here) we will fallback to the MethodName symbol part
            if (symbol.ContainingType is null)
            {
                return SymbolDisplayPartKind.MethodName;
            }

            return GetPartKind(symbol.ContainingType);
        }

        private void AddReturnType(IMethodSymbol symbol)
        {
            var methodSymbol = symbol as MethodSymbol;

            if ((object)methodSymbol == null)
            {
                symbol.ReturnType.Accept(this.NotFirstVisitor);
            }
            else
            {
                VisitTypeSymbolWithAnnotations(methodSymbol.ReturnType);
            }
        }

        private void AddTypeParameterConstraints(IMethodSymbol symbol)
        {
            if (format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeConstraints))
            {
                AddTypeParameterConstraints(symbol.TypeArguments);
            }
        }

        private void AddParameters(IMethodSymbol symbol)
        {
            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters))
            {
                AddPunctuation(SyntaxKind.OpenParenToken);
                AddParametersIfRequired(
                    hasThisParameter: symbol.IsExtensionMethod && symbol.MethodKind != MethodKind.ReducedExtension,
                    isVarargs: symbol.IsVararg,
                    parameters: symbol.Parameters);
                AddPunctuation(SyntaxKind.CloseParenToken);
            }
        }

        public override void VisitParameter(IParameterSymbol symbol)
        {
            // Note the asymmetry between VisitParameter and VisitTypeParameter: VisitParameter
            // decorates the parameter, whereas VisitTypeParameter leaves that to the corresponding
            // type or method. This is because type parameters are frequently used in other contexts
            // (e.g. field types, param types, etc), which just want the name whereas parameters are
            // used on their own or in the context of methods.

            var includeType = format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeType);
            var includeName = format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeName);
            var includeBrackets = format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeOptionalBrackets);

            if (includeBrackets && symbol.IsOptional)
            {
                AddPunctuation(SyntaxKind.OpenBracketToken);
            }

            if (includeType)
            {
                AddParameterRefKindIfRequired(symbol.RefKind);
                AddCustomModifiersIfRequired(symbol.RefCustomModifiers, leadingSpace: false, trailingSpace: true);

                if (symbol.IsParams && format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeParamsRefOut))
                {
                    AddKeyword(SyntaxKind.ParamsKeyword);
                    AddSpace();
                }

                var parameter = symbol as ParameterSymbol;
                if ((object)parameter != null)
                {
                    VisitTypeSymbolWithAnnotations(parameter.Type);
                }
                else
                {
                    symbol.Type.Accept(this.NotFirstVisitor);
                }
                AddCustomModifiersIfRequired(symbol.CustomModifiers, leadingSpace: true, trailingSpace: false);
            }

            if (includeName && includeType)
            {
                AddSpace();
            }

            if (includeName)
            {
                var kind = symbol.IsThis ? SymbolDisplayPartKind.Keyword : SymbolDisplayPartKind.ParameterName;
                builder.Add(CreatePart(kind, symbol, symbol.Name));

                if (format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeDefaultValue) &&
                    symbol.HasExplicitDefaultValue &&
                    CanAddConstant(symbol.Type, symbol.ExplicitDefaultValue))
                {
                    AddSpace();
                    AddPunctuation(SyntaxKind.EqualsToken);
                    AddSpace();

                    AddConstantValue(symbol.Type, symbol.ExplicitDefaultValue);
                }
            }

            if (includeBrackets && symbol.IsOptional)
            {
                AddPunctuation(SyntaxKind.CloseBracketToken);
            }
        }

        private static bool CanAddConstant(ITypeSymbol type, object value)
        {
            if (type.TypeKind == TypeKind.Enum)
            {
                return true;
            }

            if (value == null)
            {
                return true;
            }

            return value.GetType().GetTypeInfo().IsPrimitive || value is string || value is decimal;
        }

        private void AddFieldModifiersIfRequired(IFieldSymbol symbol)
        {
            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeModifiers) &&
                !IsEnumMember(symbol))
            {
                if (symbol.IsConst)
                {
                    AddKeyword(SyntaxKind.ConstKeyword);
                    AddSpace();
                }

                if (symbol.IsReadOnly)
                {
                    AddKeyword(SyntaxKind.ReadOnlyKeyword);
                    AddSpace();
                }

                if (symbol.IsVolatile)
                {
                    AddKeyword(SyntaxKind.VolatileKeyword);
                    AddSpace();
                }

                //TODO: event
            }
        }

        private void AddMemberModifiersIfRequired(ISymbol symbol)
        {
            INamedTypeSymbol containingType = symbol.ContainingType;

            // all members (that end up here) must have a containing type or a containing symbol should be a TypeSymbol.
            Debug.Assert(containingType != null || (symbol.ContainingSymbol is ITypeSymbol));

            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeModifiers) &&
                (containingType == null ||
                 (containingType.TypeKind != TypeKind.Interface && !IsEnumMember(symbol) && !IsLocalFunction(symbol))))
            {
                var isConst = symbol is IFieldSymbol && ((IFieldSymbol)symbol).IsConst;
                if (symbol.IsStatic && !isConst)
                {
                    AddKeyword(SyntaxKind.StaticKeyword);
                    AddSpace();
                }

                if (symbol.IsOverride)
                {
                    AddKeyword(SyntaxKind.OverrideKeyword);
                    AddSpace();
                }

                if (symbol.IsAbstract)
                {
                    AddKeyword(SyntaxKind.AbstractKeyword);
                    AddSpace();
                }

                if (symbol.IsSealed)
                {
                    AddKeyword(SyntaxKind.SealedKeyword);
                    AddSpace();
                }

                if (symbol.IsExtern)
                {
                    AddKeyword(SyntaxKind.ExternKeyword);
                    AddSpace();
                }

                if (symbol.IsVirtual)
                {
                    AddKeyword(SyntaxKind.VirtualKeyword);
                    AddSpace();
                }
            }
        }

        private void AddParametersIfRequired(bool hasThisParameter, bool isVarargs, ImmutableArray<IParameterSymbol> parameters)
        {
            if (format.ParameterOptions == SymbolDisplayParameterOptions.None)
            {
                return;
            }

            var first = true;

            // The display code is called by the debugger; if a developer is debugging Roslyn and attempts
            // to visualize a symbol *during its construction*, the parameters and return type might 
            // still be null. 

            if (!parameters.IsDefault)
            {
                foreach (var param in parameters)
                {
                    if (!first)
                    {
                        AddPunctuation(SyntaxKind.CommaToken);
                        AddSpace();
                    }
                    else if (hasThisParameter)
                    {
                        if (format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeExtensionThis))
                        {
                            AddKeyword(SyntaxKind.ThisKeyword);
                            AddSpace();
                        }
                    }

                    first = false;
                    param.Accept(this.NotFirstVisitor);
                }
            }

            if (isVarargs)
            {
                if (!first)
                {
                    AddPunctuation(SyntaxKind.CommaToken);
                    AddSpace();
                }

                AddKeyword(SyntaxKind.ArgListKeyword);
            }
        }

        private void AddAccessor(ISymbol property, IMethodSymbol method, SyntaxKind keyword)
        {
            if (method != null)
            {
                AddSpace();
                if (method.DeclaredAccessibility != property.DeclaredAccessibility)
                {
                    AddAccessibility(method);
                }

                AddKeyword(keyword);
                AddPunctuation(SyntaxKind.SemicolonToken);
            }
        }

        private void AddExplicitInterfaceIfRequired<T>(ImmutableArray<T> implementedMethods) where T : ISymbol
        {
            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeExplicitInterface) && !implementedMethods.IsEmpty)
            {
                var implementedMethod = implementedMethods[0];
                Debug.Assert(implementedMethod.ContainingType != null);

                INamedTypeSymbol containingType = implementedMethod.ContainingType;
                if (containingType != null)
                {
                    containingType.Accept(this.NotFirstVisitor);
                    AddPunctuation(SyntaxKind.DotToken);
                }
            }
        }

        private void AddCustomModifiersIfRequired(ImmutableArray<CustomModifier> customModifiers, bool leadingSpace = false, bool trailingSpace = true)
        {
            if (this.format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers) && !customModifiers.IsEmpty)
            {
                bool first = true;
                foreach (CustomModifier customModifier in customModifiers)
                {
                    if (!first || leadingSpace)
                    {
                        AddSpace();
                    }
                    first = false;

                    this.builder.Add(CreatePart(InternalSymbolDisplayPartKind.Other, null, customModifier.IsOptional ? IL_KEYWORD_MODOPT : IL_KEYWORD_MODREQ));
                    AddPunctuation(SyntaxKind.OpenParenToken);
                    customModifier.Modifier.Accept(this.NotFirstVisitor);
                    AddPunctuation(SyntaxKind.CloseParenToken);
                }
                if (trailingSpace)
                {
                    AddSpace();
                }
            }
        }

        private void AddRefIfRequired()
        {
            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeRef))
            {
                AddKeyword(SyntaxKind.RefKeyword);
                AddSpace();
            }
        }

        private void AddRefReadonlyIfRequired()
        {
            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeRef))
            {
                AddKeyword(SyntaxKind.RefKeyword);
                AddSpace();
                AddKeyword(SyntaxKind.ReadOnlyKeyword);
                AddSpace();
            }
        }

        private void AddParameterRefKindIfRequired(RefKind refKind)
        {
            if (format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeParamsRefOut))
            {
                switch (refKind)
                {
                    case RefKind.Out:
                        AddKeyword(SyntaxKind.OutKeyword);
                        AddSpace();
                        break;
                    case RefKind.Ref:
                        AddKeyword(SyntaxKind.RefKeyword);
                        AddSpace();
                        break;
                    case RefKind.In:
                        AddKeyword(SyntaxKind.InKeyword);
                        AddSpace();
                        break;
                }
            }
        }
    }
}
