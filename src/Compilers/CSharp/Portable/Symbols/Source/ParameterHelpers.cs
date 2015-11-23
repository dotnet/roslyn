// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class ParameterHelpers
    {
        public static ImmutableArray<ParameterSymbol> MakeParameters(
            Binder binder,
            Symbol owner,
            BaseParameterListSyntax syntax,
            bool allowRefOrOut,
            out SyntaxToken arglistToken,
            DiagnosticBag diagnostics,
            bool beStrict)
        {
            arglistToken = default(SyntaxToken);

            int parameterIndex = 0;
            int firstDefault = -1;

            var builder = ArrayBuilder<ParameterSymbol>.GetInstance();
            ImmutableArray<ParameterSymbol> parameters;

            foreach (var parameterSyntax in syntax.Parameters)
            {
                SyntaxToken outKeyword;
                SyntaxToken refKeyword;
                SyntaxToken paramsKeyword;
                SyntaxToken thisKeyword;
                var refKind = GetModifiers(parameterSyntax.Modifiers, out outKeyword, out refKeyword, out paramsKeyword, out thisKeyword);

                if (parameterSyntax.IsArgList)
                {
                    arglistToken = parameterSyntax.Identifier;
                    // The native compiler produces "Expected type" here, in the parser. Roslyn produces
                    // the somewhat more informative "arglist not valid" error.
                    if (paramsKeyword.Kind() != SyntaxKind.None || outKeyword.Kind() != SyntaxKind.None ||
                        refKeyword.Kind() != SyntaxKind.None || thisKeyword.Kind() != SyntaxKind.None)
                    {
                        // CS1669: __arglist is not valid in this context
                        diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, arglistToken.GetLocation());
                    }
                    continue;
                }

                if (parameterSyntax.Default != null && firstDefault == -1)
                {
                    firstDefault = parameterIndex;
                }

                Debug.Assert(parameterSyntax.Type != null);
                var parameterType = binder.BindType(parameterSyntax.Type, diagnostics);

                if (!allowRefOrOut && (refKind != RefKind.None))
                {
                    var outOrRefKeyword = (outKeyword.Kind() != SyntaxKind.None) ? outKeyword : refKeyword;
                    Debug.Assert(outOrRefKeyword.Kind() != SyntaxKind.None);

                    // error CS0631: ref and out are not valid in this context
                    diagnostics.Add(ErrorCode.ERR_IllegalRefParam, outOrRefKeyword.GetLocation());
                }

                var parameter = SourceParameterSymbol.Create(
                    binder,
                    owner,
                    parameterType,
                    parameterSyntax,
                    refKind,
                    parameterSyntax.Identifier,
                    parameterIndex,
                    (paramsKeyword.Kind() != SyntaxKind.None),
                    parameterIndex == 0 && thisKeyword.Kind() != SyntaxKind.None,
                    diagnostics,
                    beStrict);

                ReportParameterErrors(owner, parameterSyntax, parameter, firstDefault, diagnostics);

                builder.Add(parameter);
                ++parameterIndex;
            }

            parameters = builder.ToImmutableAndFree();

            var methodOwner = owner as MethodSymbol;
            var typeParameters = (object)methodOwner != null ?
                methodOwner.TypeParameters :
                default(ImmutableArray<TypeParameterSymbol>);

            binder.ValidateParameterNameConflicts(typeParameters, parameters, diagnostics);
            return parameters;
        }

        private static void ReportParameterErrors(
            Symbol owner,
            ParameterSyntax parameterSyntax,
            SourceParameterSymbol parameter,
            int firstDefault,
            DiagnosticBag diagnostics)
        {
            TypeSymbol parameterType = parameter.Type.TypeSymbol;
            int parameterIndex = parameter.Ordinal;
            bool isDefault = parameterSyntax.Default != null;
            SyntaxToken thisKeyword = parameterSyntax.Modifiers.FirstOrDefault(SyntaxKind.ThisKeyword);

            if (thisKeyword.Kind() == SyntaxKind.ThisKeyword && parameterIndex != 0)
            {
                // Report CS1100 on "this". Note that is a change from Dev10
                // which reports the error on the type following "this".

                // error CS1100: Method '{0}' has a parameter modifier 'this' which is not on the first parameter
                diagnostics.Add(ErrorCode.ERR_BadThisParam, thisKeyword.GetLocation(), owner.Name);
            }
            else if (parameter.IsParams && owner.IsOperator())
            {
                // error CS1670: params is not valid in this context
                diagnostics.Add(ErrorCode.ERR_IllegalParams, parameterSyntax.Modifiers.First(t => t.Kind() == SyntaxKind.ParamsKeyword).GetLocation());
            }
            else if (parameter.IsParams && !parameterType.IsSZArray())
            {
                // error CS0225: The params parameter must be a single dimensional array
                diagnostics.Add(ErrorCode.ERR_ParamsMustBeArray, parameterSyntax.Modifiers.First(t => t.Kind() == SyntaxKind.ParamsKeyword).GetLocation());
            }
            else if (parameter.Type.IsStatic && !parameter.ContainingSymbol.ContainingType.IsInterfaceType())
            {
                // error CS0721: '{0}': static types cannot be used as parameters
                diagnostics.Add(ErrorCode.ERR_ParameterIsStaticClass, owner.Locations[0], parameter.Type.TypeSymbol);
            }
            else if (firstDefault != -1 && parameterIndex > firstDefault && !isDefault && !parameter.IsParams)
            {
                // error CS1737: Optional parameters must appear after all required parameters
                Location loc = parameterSyntax.Identifier.GetNextToken(includeZeroWidth: true).GetLocation(); //could be missing
                diagnostics.Add(ErrorCode.ERR_DefaultValueBeforeRequiredValue, loc);
            }
            else if (parameter.RefKind != RefKind.None && parameter.Type.IsRestrictedType())
            {
                // CS1601: Cannot make reference to variable of type 'System.TypedReference'
                diagnostics.Add(ErrorCode.ERR_MethodArgCantBeRefAny, parameterSyntax.Location, parameter.Type.TypeSymbol);
            }
        }

        internal static bool ReportDefaultParameterErrors(
            Binder binder,
            Symbol owner,
            ParameterSyntax parameterSyntax,
            SourceParameterSymbol parameter,
            BoundExpression defaultExpression,
            DiagnosticBag diagnostics)
        {
            bool hasErrors = false;

            // SPEC VIOLATION: The spec says that the conversion from the initializer to the 
            // parameter type is required to be either an identity or a nullable conversion, but
            // that is not right:
            //
            // void M(short myShort = 10) {}
            // * not an identity or nullable conversion but should be legal
            //
            // void M(object obj = (dynamic)null) {}
            // * an identity conversion, but should be illegal
            //
            // void M(MyStruct? myStruct = default(MyStruct)) {}
            // * a nullable conversion, but must be illegal because we cannot generate metadata for it
            // 
            // Even if the expression is thoroughly illegal, we still want to bind it and 
            // stick it in the parameter because we want to be able to analyze it for
            // IntelliSense purposes.

            TypeSymbol parameterType = parameter.Type.TypeSymbol;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion conversion = binder.Conversions.ClassifyImplicitConversionFromExpression(defaultExpression, parameterType, ref useSiteDiagnostics);
            diagnostics.Add(defaultExpression.Syntax, useSiteDiagnostics);

            // SPEC VIOLATION: 
            // By the spec an optional parameter initializer is required to be either:
            // * a constant,
            // * new S() where S is a value type
            // * default(S) where S is a value type.
            // 
            // The native compiler considers default(T) to be a valid
            // initializer regardless of whether T is a value type
            // reference type, type parameter type, and so on.
            // We should consider simply allowing this in the spec.
            //
            // Also when valuetype S has a parameterless constructor, 
            // new S() is clearly not a constant expression and should produce an error

            bool isValidDefaultValue = (defaultExpression.ConstantValue != null) ||
                                        (defaultExpression.Kind == BoundKind.DefaultOperator) ||
                                        (defaultExpression.Kind == BoundKind.ObjectCreationExpression &&
                                                ((BoundObjectCreationExpression)defaultExpression).Constructor.IsDefaultValueTypeConstructor());

            SyntaxToken outKeyword;
            SyntaxToken refKeyword;
            SyntaxToken paramsKeyword;
            SyntaxToken thisKeyword;
            GetModifiers(parameterSyntax.Modifiers, out outKeyword, out refKeyword, out paramsKeyword, out thisKeyword);

            // CONSIDER: We are inconsistent here regarding where the error is reported; is it
            // CONSIDER: reported on the parameter name, or on the value of the initializer?
            // CONSIDER: Consider making this consistent.

            if (outKeyword.Kind() == SyntaxKind.OutKeyword)
            {
                // error CS1741: A ref or out parameter cannot have a default value
                diagnostics.Add(ErrorCode.ERR_RefOutDefaultValue, outKeyword.GetLocation());
                hasErrors = true;
            }
            else if (refKeyword.Kind() == SyntaxKind.RefKeyword)
            {
                // error CS1741: A ref or out parameter cannot have a default value
                diagnostics.Add(ErrorCode.ERR_RefOutDefaultValue, refKeyword.GetLocation());
                hasErrors = true;
            }
            else if (paramsKeyword.Kind() == SyntaxKind.ParamsKeyword)
            {
                // error CS1751: Cannot specify a default value for a parameter array
                diagnostics.Add(ErrorCode.ERR_DefaultValueForParamsParameter, paramsKeyword.GetLocation());
                hasErrors = true;
            }
            else if (thisKeyword.Kind() == SyntaxKind.ThisKeyword)
            {
                // Only need to report CS1743 for the first parameter. The caller will
                // have reported CS1100 if 'this' appeared on another parameter.
                if (parameter.Ordinal == 0)
                {
                    // error CS1743: Cannot specify a default value for the 'this' parameter
                    diagnostics.Add(ErrorCode.ERR_DefaultValueForExtensionParameter, thisKeyword.GetLocation());
                    hasErrors = true;
                }
            }
            else if (!defaultExpression.HasAnyErrors && !isValidDefaultValue)
            {
                // error CS1736: Default parameter value for '{0}' must be a compile-time constant
                diagnostics.Add(ErrorCode.ERR_DefaultValueMustBeConstant, parameterSyntax.Default.Value.Location, parameterSyntax.Identifier.ValueText);
                hasErrors = true;
            }
            else if (!conversion.Exists ||
                conversion.IsUserDefined ||
                conversion.IsIdentity && parameterType.SpecialType == SpecialType.System_Object && defaultExpression.Type.IsDynamic())
            {
                // If we had no implicit conversion, or a user-defined conversion, report an error.
                //
                // Even though "object x = (dynamic)null" is a legal identity conversion, we do not allow it. 
                // CONSIDER: We could. Doesn't hurt anything.

                // error CS1750: A value of type '{0}' cannot be used as a default parameter because there are no standard conversions to type '{1}'
                diagnostics.Add(ErrorCode.ERR_NoConversionForDefaultParam, parameterSyntax.Identifier.GetLocation(),
                    defaultExpression.Type ?? defaultExpression.Display, parameterType);

                hasErrors = true;
            }
            else if (conversion.IsReference &&
                (parameterType.SpecialType == SpecialType.System_Object || parameterType.Kind == SymbolKind.DynamicType) &&
                (object)defaultExpression.Type != null &&
                defaultExpression.Type.SpecialType == SpecialType.System_String ||
                conversion.IsBoxing)
            {
                // We don't allow object x = "hello", object x = 123, dynamic x = "hello", etc.
                // error CS1763: '{0}' is of type '{1}'. A default parameter value of a reference type other than string can only be initialized with null
                diagnostics.Add(ErrorCode.ERR_NotNullRefDefaultParameter, parameterSyntax.Identifier.GetLocation(),
                    parameterSyntax.Identifier.ValueText, parameterType);

                hasErrors = true;
            }
            else if (conversion.IsNullable && !defaultExpression.Type.IsNullableType() &&
                !(parameterType.GetNullableUnderlyingType().IsEnumType() || parameterType.GetNullableUnderlyingType().IsIntrinsicType()))
            {
                // We can do:
                // M(int? x = default(int)) 
                // M(int? x = default(int?)) 
                // M(MyEnum? e = default(enum))
                // M(MyEnum? e = default(enum?))
                // M(MyStruct? s = default(MyStruct?))
                //
                // but we cannot do:
                //
                // M(MyStruct? s = default(MyStruct))

                // error CS1770: 
                // A value of type '{0}' cannot be used as default parameter for nullable parameter '{1}' because '{0}' is not a simple type
                diagnostics.Add(ErrorCode.ERR_NoConversionForNubDefaultParam, parameterSyntax.Identifier.GetLocation(),
                    defaultExpression.Type, parameterSyntax.Identifier.ValueText);

                hasErrors = true;
            }

            // Certain contexts allow default parameter values syntactically but they are ignored during
            // semantic analysis. They are:

            // 1. Explicitly implemented interface methods; since the method will always be called
            //    via the interface, the defaults declared on the implementation will not 
            //    be seen at the call site.
            //
            // UNDONE: 2. The "actual" side of a partial method; the default values are taken from the
            // UNDONE:    "declaring" side of the method.
            //
            // UNDONE: 3. An indexer with only one formal parameter; it is illegal to omit every argument
            // UNDONE:    to an indexer.
            //
            // 4. A user-defined operator; it is syntactically impossible to omit the argument.

            if (owner.IsExplicitInterfaceImplementation() ||
                owner.IsPartialImplementation() ||
                owner.IsOperator())
            {
                // CS1066: The default value specified for parameter '{0}' will have no effect because it applies to a 
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_DefaultValueForUnconsumedLocation,
                    parameterSyntax.Identifier.GetLocation(),
                    parameterSyntax.Identifier.ValueText);
            }

            return hasErrors;
        }

        internal static MethodSymbol FindContainingGenericMethod(Symbol symbol)
        {
            for (Symbol current = symbol; (object)current != null; current = current.ContainingSymbol)
            {
                if (current.Kind == SymbolKind.Method)
                {
                    MethodSymbol method = (MethodSymbol)current;
                    if (method.MethodKind != MethodKind.AnonymousFunction)
                    {
                        return method.IsGenericMethod ? method : null;
                    }
                }
            }
            return null;
        }

        private static RefKind GetModifiers(SyntaxTokenList modifiers, out SyntaxToken outKeyword, out SyntaxToken refKeyword, out SyntaxToken paramsKeyword, out SyntaxToken thisKeyword)
        {
            var refKind = RefKind.None;

            outKeyword = default(SyntaxToken);
            refKeyword = default(SyntaxToken);
            paramsKeyword = default(SyntaxToken);
            thisKeyword = default(SyntaxToken);

            foreach (var modifier in modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.OutKeyword:
                        outKeyword = modifier;
                        if (refKind == RefKind.None)
                        {
                            refKind = RefKind.Out;
                        }
                        break;
                    case SyntaxKind.RefKeyword:
                        refKeyword = modifier;
                        if (refKind == RefKind.None)
                        {
                            refKind = RefKind.Ref;
                        }
                        break;
                    case SyntaxKind.ParamsKeyword:
                        paramsKeyword = modifier;
                        break;
                    case SyntaxKind.ThisKeyword:
                        thisKeyword = modifier;
                        break;
                }
            }

            return refKind;
        }
    }
}
