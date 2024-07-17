// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents an interactive code entry point that is inserted into the compilation if there is not an existing one. 
    /// </summary>
    internal abstract class SynthesizedEntryPointSymbol : MethodSymbol
    {
        internal const string MainName = "<Main>";
        internal const string FactoryName = "<Factory>";

        private readonly NamedTypeSymbol _containingType;

        internal static SynthesizedEntryPointSymbol Create(SynthesizedInteractiveInitializerMethod initializerMethod, BindingDiagnosticBag diagnostics)
        {
            var containingType = initializerMethod.ContainingType;
            var compilation = containingType.DeclaringCompilation;
            if (compilation.IsSubmission)
            {
                var systemObject = Binder.GetSpecialType(compilation, SpecialType.System_Object, DummySyntax(), diagnostics);
                var submissionArrayType = compilation.CreateArrayTypeSymbol(systemObject);
                diagnostics.ReportUseSite(submissionArrayType, NoLocation.Singleton);
                return new SubmissionEntryPoint(
                    containingType,
                    initializerMethod.ReturnTypeWithAnnotations,
                    submissionArrayType);
            }
            else
            {
                var systemVoid = Binder.GetSpecialType(compilation, SpecialType.System_Void, DummySyntax(), diagnostics);
                return new ScriptEntryPoint(containingType, TypeWithAnnotations.Create(systemVoid));
            }
        }

        private SynthesizedEntryPointSymbol(NamedTypeSymbol containingType)
        {
            Debug.Assert((object)containingType != null);

            _containingType = containingType;
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        internal abstract BoundBlock CreateBody(BindingDiagnosticBag diagnostics);

        public override Symbol ContainingSymbol
        {
            get { return _containingType; }
        }

        public abstract override string Name
        {
            get;
        }

        internal override bool HasSpecialName
        {
            get { return true; }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        internal override bool RequiresSecurityObject
        {
            get { return false; }
        }

        public override bool IsVararg
        {
            get { return false; }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Private; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return ImmutableArray<TypeWithAnnotations>.Empty; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public override int Arity
        {
            get { return 0; }
        }

        public override bool ReturnsVoid
        {
            get { return ReturnType.IsVoidType(); }
        }

        public sealed override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public sealed override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public sealed override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override MethodKind MethodKind
        {
            get { return MethodKind.Ordinary; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return false; }
        }

        public override bool IsStatic
        {
            get { return true; }
        }

        public override bool IsAsync
        {
            get { return false; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal sealed override UnmanagedCallersOnlyAttributeData GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => null;

        internal override Cci.CallingConvention CallingConvention
        {
            get { return 0; }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        internal sealed override bool IsDeclaredReadOnly => false;

        internal sealed override bool IsInitOnly => false;

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        public override DllImportData GetDllImportData()
        {
            return null;
        }

        public sealed override bool AreLocalsZeroed
        {
            get { return ContainingType.AreLocalsZeroed; }
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        private static CSharpSyntaxNode DummySyntax()
        {
            var syntaxTree = CSharpSyntaxTree.Dummy;
            return (CSharpSyntaxNode)syntaxTree.GetRoot();
        }

        private static BoundCall CreateParameterlessCall(CSharpSyntaxNode syntax, BoundExpression receiver, ThreeState receiverIsSubjectToCloning, MethodSymbol method)
        {
            return new BoundCall(
                syntax,
                receiver,
                initialBindingReceiverIsSubjectToCloning: receiverIsSubjectToCloning,
                method,
                ImmutableArray<BoundExpression>.Empty,
                default(ImmutableArray<string>),
                default(ImmutableArray<RefKind>),
                isDelegateCall: false,
                expanded: false,
                invokedAsExtensionMethod: false,
                argsToParamsOpt: default(ImmutableArray<int>),
                defaultArguments: default(BitVector),
                resultKind: LookupResultKind.Viable,
                type: method.ReturnType)
            { WasCompilerGenerated = true };
        }

        protected sealed override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal sealed override bool HasUnscopedRefAttribute => false;

        internal sealed override bool UseUpdatedEscapeRules => ContainingModule.UseUpdatedEscapeRules;

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal override int? TryGetOverloadResolutionPriority()
        {
            return null;
        }

        /// <summary> A synthesized entrypoint that forwards all calls to an async Main Method </summary>
        internal sealed class AsyncForwardEntryPoint : SynthesizedEntryPointSymbol
        {
            /// <summary> The syntax for the user-defined asynchronous main method. </summary>
            private readonly CSharpSyntaxNode _userMainReturnTypeSyntax;

            private readonly BoundExpression _getAwaiterGetResultCall;

            private readonly ImmutableArray<ParameterSymbol> _parameters;

            /// <summary> The user-defined asynchronous main method. </summary>
            internal readonly MethodSymbol UserMain;

            internal AsyncForwardEntryPoint(CSharpCompilation compilation, NamedTypeSymbol containingType, MethodSymbol userMain) :
                base(containingType)
            {
                // There should be no way for a userMain to be passed in unless it already passed the 
                // parameter checks for determining entrypoint validity.
                Debug.Assert(userMain.ParameterCount == 0 || userMain.ParameterCount == 1);

                UserMain = userMain;
                _userMainReturnTypeSyntax = userMain.ExtractReturnTypeSyntax();
                var binder = compilation.GetBinder(_userMainReturnTypeSyntax);
                _parameters = SynthesizedParameterSymbol.DeriveParameters(userMain, this);

                var arguments = Parameters.SelectAsArray(map: (p, s) => (BoundExpression)new BoundParameter(s, p, p.Type), arg: _userMainReturnTypeSyntax);

                // Main(args) or Main()
                BoundCall userMainInvocation = new BoundCall(
                        syntax: _userMainReturnTypeSyntax,
                        receiverOpt: null,
                        initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                        method: userMain,
                        arguments: arguments,
                        argumentNamesOpt: default(ImmutableArray<string>),
                        argumentRefKindsOpt: default(ImmutableArray<RefKind>),
                        isDelegateCall: false,
                        expanded: false,
                        invokedAsExtensionMethod: false,
                        argsToParamsOpt: default(ImmutableArray<int>),
                        defaultArguments: default(BitVector),
                        resultKind: LookupResultKind.Viable,
                        type: userMain.ReturnType)
                { WasCompilerGenerated = true };

                // The diagnostics that would be produced here will already have been captured and returned.
                var success = binder.GetAwaitableExpressionInfo(userMainInvocation, out _getAwaiterGetResultCall!, _userMainReturnTypeSyntax, BindingDiagnosticBag.Discarded);

                Debug.Assert(
                    ReturnType.IsVoidType() ||
                    ReturnType.SpecialType == SpecialType.System_Int32);
            }

            internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
            {
                base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

                AddSynthesizedAttribute(ref attributes, this.DeclaringCompilation.SynthesizeDebuggerStepThroughAttribute());
            }

            public override string Name => MainName;

            public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

            public override TypeWithAnnotations ReturnTypeWithAnnotations => TypeWithAnnotations.Create(_getAwaiterGetResultCall.Type);

            internal override BoundBlock CreateBody(BindingDiagnosticBag diagnostics)
            {
                var syntax = _userMainReturnTypeSyntax;

                if (ReturnsVoid)
                {
                    return new BoundBlock(
                        syntax: syntax,
                        locals: ImmutableArray<LocalSymbol>.Empty,
                        statements: ImmutableArray.Create<BoundStatement>(
                            new BoundExpressionStatement(
                                syntax: syntax,
                                expression: _getAwaiterGetResultCall
                            )
                            { WasCompilerGenerated = true },
                            new BoundReturnStatement(
                                syntax: syntax,
                                refKind: RefKind.None,
                                expressionOpt: null,
                                @checked: false
                            )
                            { WasCompilerGenerated = true }
                        )
                    )
                    { WasCompilerGenerated = true };

                }
                else
                {
                    return new BoundBlock(
                        syntax: syntax,
                        locals: ImmutableArray<LocalSymbol>.Empty,
                        statements: ImmutableArray.Create<BoundStatement>(
                            new BoundReturnStatement(
                                syntax: syntax,
                                refKind: RefKind.None,
                                expressionOpt: _getAwaiterGetResultCall,
                                @checked: false
                            )
                        )
                    )
                    { WasCompilerGenerated = true };
                }
            }
        }

        internal sealed override bool IsNullableAnalysisEnabled() => false;

        private sealed class ScriptEntryPoint : SynthesizedEntryPointSymbol
        {
            private readonly TypeWithAnnotations _returnType;

            internal ScriptEntryPoint(NamedTypeSymbol containingType, TypeWithAnnotations returnType) :
                base(containingType)
            {
                Debug.Assert(containingType.IsScriptClass);
                Debug.Assert(returnType.IsVoidType());
                _returnType = returnType;
            }

            public override string Name => MainName;

            public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

            public override TypeWithAnnotations ReturnTypeWithAnnotations => _returnType;

            // private static void <Main>()
            // {
            //     var script = new Script();
            //     script.<Initialize>().GetAwaiter().GetResult();
            // }
            internal override BoundBlock CreateBody(BindingDiagnosticBag diagnostics)
            {
                var syntax = DummySyntax();
                var compilation = _containingType.DeclaringCompilation;

                // Creates a new top-level binder that just contains the global imports for the compilation.
                // The imports are required if a consumer of the scripting API is using a Task implementation 
                // that uses extension methods.
                Binder binder = WithUsingNamespacesAndTypesBinder.Create(compilation.GlobalImports, next: new BuckStopsHereBinder(compilation, null), withImportChainEntry: true);
                binder = new InContainerBinder(compilation.GlobalNamespace, binder);

                var ctor = _containingType.GetScriptConstructor();
                Debug.Assert(ctor.ParameterCount == 0);

                var initializer = _containingType.GetScriptInitializer();
                Debug.Assert(initializer.ParameterCount == 0);

                var scriptLocal = new BoundLocal(
                    syntax,
                    new SynthesizedLocal(this, TypeWithAnnotations.Create(_containingType), SynthesizedLocalKind.LoweringTemp),
                    null,
                    _containingType)
                { WasCompilerGenerated = true };

                Debug.Assert(!initializer.ReturnType.IsDynamic());
                var initializeCall = CreateParameterlessCall(syntax, scriptLocal, receiverIsSubjectToCloning: ThreeState.False, initializer);
                BoundExpression getAwaiterGetResultCall;
                if (!binder.GetAwaitableExpressionInfo(initializeCall, out getAwaiterGetResultCall, syntax, diagnostics))
                {
                    return new BoundBlock(
                        syntax: syntax,
                        locals: ImmutableArray<LocalSymbol>.Empty,
                        statements: ImmutableArray<BoundStatement>.Empty,
                        hasErrors: true);
                }

                return new BoundBlock(syntax,
                    ImmutableArray.Create<LocalSymbol>(scriptLocal.LocalSymbol),
                    ImmutableArray.Create<BoundStatement>(
                        // var script = new Script();
                        new BoundExpressionStatement(
                            syntax,
                            new BoundAssignmentOperator(
                                syntax,
                                scriptLocal,
                                new BoundObjectCreationExpression(
                                    syntax,
                                    ctor)
                                { WasCompilerGenerated = true },
                                _containingType)
                            { WasCompilerGenerated = true })
                        { WasCompilerGenerated = true },
                        // script.<Initialize>().GetAwaiter().GetResult();
                        new BoundExpressionStatement(syntax, getAwaiterGetResultCall) { WasCompilerGenerated = true },
                        // return;
                        new BoundReturnStatement(
                            syntax,
                            RefKind.None,
                            null,
                            @checked: false)
                        { WasCompilerGenerated = true }))
                { WasCompilerGenerated = true };
            }
        }

        private sealed class SubmissionEntryPoint : SynthesizedEntryPointSymbol
        {
            private readonly ImmutableArray<ParameterSymbol> _parameters;
            private readonly TypeWithAnnotations _returnType;

            internal SubmissionEntryPoint(NamedTypeSymbol containingType, TypeWithAnnotations returnType, TypeSymbol submissionArrayType) :
                base(containingType)
            {
                Debug.Assert(containingType.IsSubmissionClass);
                Debug.Assert(!returnType.IsVoidType());
                _parameters = ImmutableArray.Create(SynthesizedParameterSymbol.Create(this,
                    TypeWithAnnotations.Create(submissionArrayType), 0, RefKind.None, "submissionArray"));

                _returnType = returnType;
            }

            public override string Name
            {
                get { return FactoryName; }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return _parameters; }
            }

            public override TypeWithAnnotations ReturnTypeWithAnnotations => _returnType;

            // private static T <Factory>(object[] submissionArray) 
            // {
            //     var submission = new Submission#N(submissionArray);
            //     return submission.<Initialize>();
            // }
            internal override BoundBlock CreateBody(BindingDiagnosticBag diagnostics)
            {
                var syntax = DummySyntax();

                var ctor = _containingType.GetScriptConstructor();
                Debug.Assert(ctor.ParameterCount == 1);

                var initializer = _containingType.GetScriptInitializer();
                Debug.Assert(initializer.ParameterCount == 0);

                var submissionArrayParameter = new BoundParameter(syntax, _parameters[0]) { WasCompilerGenerated = true };
                var submissionLocal = new BoundLocal(
                    syntax,
                    new SynthesizedLocal(this, TypeWithAnnotations.Create(_containingType), SynthesizedLocalKind.LoweringTemp),
                    null,
                    _containingType)
                { WasCompilerGenerated = true };

                // var submission = new Submission#N(submissionArray);
                var submissionAssignment = new BoundExpressionStatement(
                    syntax,
                    new BoundAssignmentOperator(
                        syntax,
                        submissionLocal,
                        new BoundObjectCreationExpression(
                            syntax,
                            ctor,
                            ImmutableArray.Create<BoundExpression>(submissionArrayParameter),
                            argumentNamesOpt: default(ImmutableArray<string>),
                            argumentRefKindsOpt: default(ImmutableArray<RefKind>),
                            expanded: false,
                            argsToParamsOpt: default(ImmutableArray<int>),
                            defaultArguments: default(BitVector),
                            constantValueOpt: null,
                            initializerExpressionOpt: null,
                            type: _containingType)
                        { WasCompilerGenerated = true },
                        _containingType)
                    { WasCompilerGenerated = true })
                { WasCompilerGenerated = true };

                // return submission.<Initialize>();
                var initializeResult = CreateParameterlessCall(
                    syntax,
                    submissionLocal,
                    receiverIsSubjectToCloning: ThreeState.False,
                    initializer);
                Debug.Assert(TypeSymbol.Equals(initializeResult.Type, _returnType.Type, TypeCompareKind.ConsiderEverything2));
                var returnStatement = new BoundReturnStatement(
                    syntax,
                    RefKind.None,
                    initializeResult,
                    @checked: false)
                { WasCompilerGenerated = true };

                return new BoundBlock(syntax,
                    ImmutableArray.Create<LocalSymbol>(submissionLocal.LocalSymbol),
                    ImmutableArray.Create<BoundStatement>(submissionAssignment, returnStatement))
                { WasCompilerGenerated = true };
            }
        }
    }
}
