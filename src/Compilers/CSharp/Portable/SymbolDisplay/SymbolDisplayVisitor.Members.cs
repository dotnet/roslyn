// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
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
            symbol.Type.Accept(this.NotFirstVisitor);
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

        private static bool ShouldPropertyDisplayReadOnly(IPropertySymbol property)
        {
            if (property.ContainingType?.IsReadOnly == true)
            {
                return false;
            }

            // If at least one accessor is present and all present accessors are readonly, the property should be marked readonly.

            var getMethod = property.GetMethod;
            if (getMethod is object && !ShouldMethodDisplayReadOnly(getMethod, property))
            {
                return false;
            }

            var setMethod = property.SetMethod;
            if (setMethod is object && !ShouldMethodDisplayReadOnly(setMethod, property))
            {
                return false;
            }

            return getMethod is object || setMethod is object;
        }

        private static bool ShouldMethodDisplayReadOnly(IMethodSymbol method, IPropertySymbol propertyOpt = null)
        {
            if (method.ContainingType?.IsReadOnly == true)
            {
                return false;
            }

            if ((method as Symbols.PublicModel.MethodSymbol)?.UnderlyingMethodSymbol is SourcePropertyAccessorSymbol sourceAccessor &&
                (propertyOpt as Symbols.PublicModel.PropertySymbol)?.UnderlyingSymbol is SourcePropertySymbolBase sourceProperty)
            {
                // only display if the accessor is explicitly readonly
                return sourceAccessor.LocalDeclaredReadOnly || sourceProperty.HasReadOnlyModifier;
            }
            else if (method is Symbols.PublicModel.MethodSymbol m)
            {
                return m.UnderlyingMethodSymbol.IsDeclaredReadOnly;
            }

            return false;
        }

        public override void VisitProperty(IPropertySymbol symbol)
        {
            AddAccessibilityIfRequired(symbol);
            AddMemberModifiersIfRequired(symbol);

            if (ShouldPropertyDisplayReadOnly(symbol))
            {
                AddReadOnlyIfRequired();
            }

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

                symbol.Type.Accept(this.NotFirstVisitor);
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
                var keywordForSetAccessor = IsInitOnly(symbol.SetMethod) ? SyntaxKind.InitKeyword : SyntaxKind.SetKeyword;
                AddAccessor(symbol, symbol.SetMethod, keywordForSetAccessor);

                AddSpace();
                AddPunctuation(SyntaxKind.CloseBraceToken);
            }
        }

        private static bool IsInitOnly(IMethodSymbol symbol)
        {
            return symbol?.IsInitOnly == true;
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

            var accessor = symbol.AddMethod ?? symbol.RemoveMethod;
            if (accessor is object && ShouldMethodDisplayReadOnly(accessor))
            {
                AddReadOnlyIfRequired();
            }

            if (format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeMemberKeyword))
            {
                AddKeyword(SyntaxKind.EventKeyword);
                AddSpace();
            }

            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType))
            {
                symbol.Type.Accept(this.NotFirstVisitor);
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
            else if ((symbol as Symbols.PublicModel.MethodSymbol)?.UnderlyingMethodSymbol is SynthesizedGlobalMethodSymbol) // It would be nice to handle VB symbols too, but it's not worth the effort.
            {
                // Represents a compiler generated synthesized method symbol with a null containing
                // type.

                // TODO(cyrusn); Why is this a literal?
                builder.Add(CreatePart(SymbolDisplayPartKind.NumericLiteral, symbol, symbol.Name));
                return;
            }
            else if (symbol.MethodKind == MethodKind.FunctionPointerSignature)
            {
                visitFunctionPointerSignature(symbol);
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

                if (ShouldMethodDisplayReadOnly(symbol))
                {
                    AddReadOnlyIfRequired();
                }

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
                            // Otherwise we eschew it since it is redundant in a conversion
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
                        AddKeyword(symbol.MethodKind == MethodKind.PropertyGet ? SyntaxKind.GetKeyword :
                            IsInitOnly(symbol) ? SyntaxKind.InitKeyword : SyntaxKind.SetKeyword);
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

                        if (!format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) &&
                            symbol.GetSymbol()?.OriginalDefinition is SourceUserDefinedOperatorSymbolBase sourceUserDefinedOperatorSymbolBase)
                        {
                            var operatorName = symbol.MetadataName;
                            var lastDotPosition = operatorName.LastIndexOf('.');

                            if (lastDotPosition >= 0)
                            {
                                operatorName = operatorName.Substring(lastDotPosition + 1);
                            }

                            if (sourceUserDefinedOperatorSymbolBase is SourceUserDefinedConversionSymbol)
                            {
                                addUserDefinedConversionName(symbol, operatorName);
                            }
                            else
                            {
                                addUserDefinedOperatorName(symbol, operatorName);
                            }
                            break;
                        }

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
                            addUserDefinedOperatorName(symbol, symbol.MetadataName);
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
                            addUserDefinedConversionName(symbol, symbol.MetadataName);
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

            void visitFunctionPointerSignature(IMethodSymbol symbol)
            {
                AddKeyword(SyntaxKind.DelegateKeyword);
                AddPunctuation(SyntaxKind.AsteriskToken);

                if (symbol.CallingConvention != SignatureCallingConvention.Default)
                {
                    AddSpace();
                    AddKeyword(SyntaxKind.UnmanagedKeyword);

                    var conventionTypes = symbol.UnmanagedCallingConventionTypes;

                    if (symbol.CallingConvention != SignatureCallingConvention.Unmanaged || !conventionTypes.IsEmpty)
                    {
                        AddPunctuation(SyntaxKind.OpenBracketToken);

                        switch (symbol.CallingConvention)
                        {
                            case SignatureCallingConvention.CDecl:
                                builder.Add(CreatePart(SymbolDisplayPartKind.ClassName, symbol, "Cdecl"));
                                break;
                            case SignatureCallingConvention.StdCall:
                                builder.Add(CreatePart(SymbolDisplayPartKind.ClassName, symbol, "Stdcall"));
                                break;
                            case SignatureCallingConvention.ThisCall:
                                builder.Add(CreatePart(SymbolDisplayPartKind.ClassName, symbol, "Thiscall"));
                                break;
                            case SignatureCallingConvention.FastCall:
                                builder.Add(CreatePart(SymbolDisplayPartKind.ClassName, symbol, "Fastcall"));
                                break;

                            case SignatureCallingConvention.Unmanaged:
                                Debug.Assert(!conventionTypes.IsDefaultOrEmpty);
                                bool isFirst = true;
                                foreach (var conventionType in conventionTypes)
                                {
                                    if (!isFirst)
                                    {
                                        AddPunctuation(SyntaxKind.CommaToken);
                                        AddSpace();
                                    }

                                    isFirst = false;
                                    Debug.Assert(conventionType.Name.StartsWith("CallConv"));
                                    const int CallConvLength = 8;
                                    builder.Add(CreatePart(SymbolDisplayPartKind.ClassName, conventionType, conventionType.Name[CallConvLength..]));
                                }

                                break;
                        }

                        AddPunctuation(SyntaxKind.CloseBracketToken);
                    }
                }

                AddPunctuation(SyntaxKind.LessThanToken);

                foreach (var param in symbol.Parameters)
                {
                    AddParameterRefKind(param.RefKind);

                    AddCustomModifiersIfRequired(param.RefCustomModifiers);

                    param.Type.Accept(this.NotFirstVisitor);

                    AddCustomModifiersIfRequired(param.CustomModifiers, leadingSpace: true, trailingSpace: false);

                    AddPunctuation(SyntaxKind.CommaToken);
                    AddSpace();
                }

                if (symbol.ReturnsByRef)
                {
                    AddRef();
                }
                else if (symbol.ReturnsByRefReadonly)
                {
                    AddRefReadonly();
                }

                AddCustomModifiersIfRequired(symbol.RefCustomModifiers);

                symbol.ReturnType.Accept(this.NotFirstVisitor);

                AddCustomModifiersIfRequired(symbol.ReturnTypeCustomModifiers, leadingSpace: true, trailingSpace: false);

                AddPunctuation(SyntaxKind.GreaterThanToken);
            }

            void addUserDefinedOperatorName(IMethodSymbol symbol, string operatorName)
            {
                AddKeyword(SyntaxKind.OperatorKeyword);
                AddSpace();

                if (operatorName == WellKnownMemberNames.TrueOperatorName)
                {
                    AddKeyword(SyntaxKind.TrueKeyword);
                }
                else if (operatorName == WellKnownMemberNames.FalseOperatorName)
                {
                    AddKeyword(SyntaxKind.FalseKeyword);
                }
                else
                {
                    if (SyntaxFacts.IsCheckedOperator(operatorName))
                    {
                        AddKeyword(SyntaxKind.CheckedKeyword);
                        AddSpace();
                    }

                    builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol,
                        SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(operatorName))));
                }
            }

            void addUserDefinedConversionName(IMethodSymbol symbol, string operatorName)
            {
                // "System.IntPtr.explicit operator System.IntPtr(int)"

                bool isChecked = false;

                if (operatorName == WellKnownMemberNames.ExplicitConversionName)
                {
                    AddKeyword(SyntaxKind.ExplicitKeyword);
                }
                else if (operatorName == WellKnownMemberNames.CheckedExplicitConversionName)
                {
                    isChecked = true;
                    AddKeyword(SyntaxKind.ExplicitKeyword);
                }
                else if (operatorName == WellKnownMemberNames.ImplicitConversionName)
                {
                    AddKeyword(SyntaxKind.ImplicitKeyword);
                }
                else
                {
                    builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol,
                        SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(operatorName))));
                }

                AddSpace();
                AddKeyword(SyntaxKind.OperatorKeyword);
                AddSpace();

                if (isChecked)
                {
                    AddKeyword(SyntaxKind.CheckedKeyword);
                    AddSpace();
                }

                AddReturnType(symbol);
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
            symbol.ReturnType.Accept(this.NotFirstVisitor);
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
            var includeName = format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeName) &&
                symbol.Name.Length != 0;
            var includeBrackets = format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeOptionalBrackets);
            var includeDefaultValue = format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeDefaultValue) &&
                format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeName) &&
                symbol.HasExplicitDefaultValue &&
                CanAddConstant(symbol.Type, symbol.ExplicitDefaultValue);

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

                symbol.Type.Accept(this.NotFirstVisitor);
                AddCustomModifiersIfRequired(symbol.CustomModifiers, leadingSpace: true, trailingSpace: false);
            }

            if (includeName)
            {
                if (includeType)
                {
                    AddSpace();
                }
                var kind = symbol.IsThis ? SymbolDisplayPartKind.Keyword : SymbolDisplayPartKind.ParameterName;
                builder.Add(CreatePart(kind, symbol, symbol.Name));
            }

            if (includeDefaultValue)
            {
                if (includeName || includeType)
                {
                    AddSpace();
                }
                AddPunctuation(SyntaxKind.EqualsToken);
                AddSpace();

                AddConstantValue(symbol.Type, symbol.ExplicitDefaultValue);
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

        private void AddAccessor(IPropertySymbol property, IMethodSymbol method, SyntaxKind keyword)
        {
            if (method != null)
            {
                AddSpace();
                if (method.DeclaredAccessibility != property.DeclaredAccessibility)
                {
                    AddAccessibility(method);
                }

                if (!ShouldPropertyDisplayReadOnly(property) && ShouldMethodDisplayReadOnly(method, property))
                {
                    AddReadOnlyIfRequired();
                }

                AddKeyword(keyword);
                AddPunctuation(SyntaxKind.SemicolonToken);
            }
        }

        private void AddExplicitInterfaceIfRequired<T>(ImmutableArray<T> implementedMembers) where T : ISymbol
        {
            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeExplicitInterface) && !implementedMembers.IsEmpty)
            {
                var implementedMember = implementedMembers[0];
                Debug.Assert(implementedMember.ContainingType != null);

                INamedTypeSymbol containingType = implementedMember.ContainingType;
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
                AddRef();
            }
        }

        private void AddRef()
        {
            AddKeyword(SyntaxKind.RefKeyword);
            AddSpace();
        }

        private void AddRefReadonlyIfRequired()
        {
            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeRef))
            {
                AddRefReadonly();
            }
        }

        private void AddRefReadonly()
        {
            AddKeyword(SyntaxKind.RefKeyword);
            AddSpace();
            AddKeyword(SyntaxKind.ReadOnlyKeyword);
            AddSpace();
        }

        private void AddReadOnlyIfRequired()
        {
            // 'readonly' in this context is effectively a 'ref' modifier
            // because it affects whether the 'this' parameter is 'ref' or 'in'.
            if (format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeRef))
            {
                AddKeyword(SyntaxKind.ReadOnlyKeyword);
                AddSpace();
            }
        }

        private void AddParameterRefKindIfRequired(RefKind refKind)
        {
            if (format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeParamsRefOut))
            {
                AddParameterRefKind(refKind);
            }
        }

        private void AddParameterRefKind(RefKind refKind)
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
