// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedHotReloadExceptionConstructorSymbol : SynthesizedInstanceConstructor
    {
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private readonly NamedTypeSymbol _exceptionType;

        internal SynthesizedHotReloadExceptionConstructorSymbol(
            NamedTypeSymbol containingType,
            NamedTypeSymbol exceptionType,
            TypeSymbol stringType,
            TypeSymbol intType) :
            base(containingType)
        {
            _exceptionType = exceptionType;
            _parameters =
            [
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(stringType), ordinal: 0, RefKind.None),
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(intType), ordinal: 1, RefKind.None)
            ];
        }

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

        /// <summary>
        /// Exception message.
        /// </summary>
        public ParameterSymbol MessageParameter => _parameters[0];

        /// <summary>
        /// Integer value of <see cref="HotReloadExceptionCode"/>.
        /// </summary>
        public ParameterSymbol CodeParameter => _parameters[1];

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var containingType = (SynthesizedHotReloadExceptionSymbol)ContainingType;

            var factory = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            factory.CurrentFunction = this;

            var exceptionConstructor = (MethodSymbol?)factory.WellKnownMember(WellKnownMember.System_Exception__ctorString, isOptional: true);
            if (exceptionConstructor is null)
            {
                diagnostics.Add(ErrorCode.ERR_EncUpdateFailedMissingSymbol,
                    Location.None,
                    CodeAnalysisResources.Constructor,
                    "System.Exception..ctor(string)");

                factory.CloseMethod(factory.Block());
                return;
            }

            // Get AppContext.GetData method
            var appContextGetData = (MethodSymbol?)factory.WellKnownMember(WellKnownMember.System_AppContext__GetData, isOptional: true);
            if (appContextGetData is null)
            {
                diagnostics.Add(ErrorCode.ERR_EncUpdateFailedMissingSymbol,
                    Location.None,
                    CodeAnalysisResources.Method,
                    "System.AppContext.GetData(string)");

                factory.CloseMethod(factory.Block());
                return;
            }

            // Get Action<Exception> type
            var actionOfException = factory.WellKnownType(WellKnownType.System_Action_T);
            if (actionOfException is null)
            {
                // If we can't get the Action type, we'll just skip the delegate invocation
                factory.CloseMethod(factory.Block(
                    // base(message)
                    factory.ExpressionStatement(factory.Call(
                        factory.This(),
                        exceptionConstructor,
                        factory.Parameter(MessageParameter))),

                    // this.CodeField = code;
                    factory.Assignment(factory.Field(factory.This(), containingType.CodeField), factory.Parameter(CodeParameter)),

                    factory.Return()));
                return;
            }

            var actionType = actionOfException.Construct(_exceptionType);
            var delegateInvoke = actionType.DelegateInvokeMethod;
            if (delegateInvoke is null ||
                delegateInvoke.ReturnType.SpecialType != SpecialType.System_Void ||
                delegateInvoke.GetParameters() is not [{ RefKind: RefKind.None } parameter] ||
                !parameter.Type.Equals(_exceptionType))
            {
                // If delegate invoke is invalid, skip invocation
                factory.CloseMethod(factory.Block(
                    // base(message)
                    factory.ExpressionStatement(factory.Call(
                        factory.This(),
                        exceptionConstructor,
                        factory.Parameter(MessageParameter))),

                    // this.CodeField = code;
                    factory.Assignment(factory.Field(factory.This(), containingType.CodeField), factory.Parameter(CodeParameter)),

                    factory.Return()));
                return;
            }

            // Call AppContext.GetData("DOTNET_HOT_RELOAD_RUNTIME_RUDE_EDIT_HOOK")
            var getData = factory.Call(
                receiver: null,
                appContextGetData,
                factory.StringLiteral("DOTNET_HOT_RELOAD_RUNTIME_RUDE_EDIT_HOOK"));

            // Cast to Action<Exception>
            var actionCast = factory.Convert(actionType, getData, Conversion.ExplicitReference);

            // Store in temp variable
            var actionTemp = factory.StoreToTemp(actionCast, out var storeAction);

            var block = factory.Block(
                [actionTemp.LocalSymbol],

                // base(message)
                factory.ExpressionStatement(factory.Call(
                    factory.This(),
                    exceptionConstructor,
                    factory.Parameter(MessageParameter))),

                // this.CodeField = code;
                factory.Assignment(factory.Field(factory.This(), containingType.CodeField), factory.Parameter(CodeParameter)),

                // action?.Invoke(this);
                factory.If(
                    factory.IsNotNullReference(storeAction),
                    factory.ExpressionStatement(
                        factory.Call(
                            actionTemp,
                            delegateInvoke,
                            factory.This()))),

                factory.Return());

            factory.CloseMethod(block);
        }
    }
}
