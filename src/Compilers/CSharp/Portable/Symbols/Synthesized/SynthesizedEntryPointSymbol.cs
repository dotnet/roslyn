﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        internal static SynthesizedEntryPointSymbol Create(SynthesizedInteractiveInitializerMethod initializerMethod, DiagnosticBag diagnostics)
        {
            var containingType = initializerMethod.ContainingType;
            var compilation = containingType.DeclaringCompilation;
            if (compilation.IsSubmission)
            {
                var systemObject = Binder.GetSpecialType(compilation, SpecialType.System_Object, DummySyntax(), diagnostics);
                var submissionArrayType = compilation.CreateArrayTypeSymbol(systemObject);
                ReportUseSiteDiagnostics(submissionArrayType, diagnostics);
                return new SubmissionEntryPoint(
                    containingType,
                    initializerMethod.ReturnType,
                    submissionArrayType);
            }
            else
            {
                var systemVoid = Binder.GetSpecialType(compilation, SpecialType.System_Void, DummySyntax(), diagnostics);
                return new ScriptEntryPoint(containingType, TypeSymbolWithAnnotations.Create(systemVoid));
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

        internal abstract BoundBlock CreateBody(DiagnosticBag diagnostics);

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

        public override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments
        {
            get { return ImmutableArray<TypeSymbolWithAnnotations>.Empty; }
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
            get { return ReturnType.SpecialType == SpecialType.System_Void; }
        }

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
            throw ExceptionUtilities.Unreachable;
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }

        private static CSharpSyntaxNode DummySyntax()
        {
            var syntaxTree = CSharpSyntaxTree.Dummy;
            return (CSharpSyntaxNode)syntaxTree.GetRoot();
        }

        private static void ReportUseSiteDiagnostics(Symbol symbol, DiagnosticBag diagnostics)
        {
            var useSiteDiagnostic = symbol.GetUseSiteDiagnostic();
            if (useSiteDiagnostic != null)
            {
                ReportUseSiteDiagnostic(useSiteDiagnostic, diagnostics, NoLocation.Singleton);
            }
        }

        private static BoundCall CreateParameterlessCall(CSharpSyntaxNode syntax, BoundExpression receiver, MethodSymbol method)
        {
            return new BoundCall(
                syntax,
                receiver,
                method,
                ImmutableArray<BoundExpression>.Empty,
                default(ImmutableArray<string>),
                default(ImmutableArray<RefKind>),
                isDelegateCall: false,
                expanded: false,
                invokedAsExtensionMethod: false,
                argsToParamsOpt: default(ImmutableArray<int>),
                resultKind: LookupResultKind.Viable,
                binderOpt: null,
                type: method.ReturnType.TypeSymbol)
            { WasCompilerGenerated = true };
        }

        /// <summary> A synthesized entrypoint that forwards all calls to an async Main Method </summary>
        internal sealed class AsyncForwardEntryPoint : SynthesizedEntryPointSymbol
        {
            /// <summary> The user-defined asynchronous main method. </summary>
            private readonly CSharpSyntaxNode _userMainReturnTypeSyntax;

            private readonly BoundExpression _getAwaiterGetResultCall;

            private readonly ImmutableArray<ParameterSymbol> _parameters;

            internal AsyncForwardEntryPoint(CSharpCompilation compilation, NamedTypeSymbol containingType, MethodSymbol userMain) :
                base(containingType)
            {
                // There should be no way for a userMain to be passed in unless it already passed the 
                // parameter checks for determining entrypoint validity.
                Debug.Assert(userMain.ParameterCount == 0 || userMain.ParameterCount == 1);

                _userMainReturnTypeSyntax = userMain.ExtractReturnTypeSyntax();
                var binder = compilation.GetBinder(_userMainReturnTypeSyntax);
                _parameters = SynthesizedParameterSymbol.DeriveParameters(userMain, this);

                var arguments = Parameters.SelectAsArray((p, s) => (BoundExpression)new BoundParameter(s, p, p.Type.TypeSymbol), _userMainReturnTypeSyntax);

                // Main(args) or Main()
                BoundCall userMainInvocation = new BoundCall(
                        syntax: _userMainReturnTypeSyntax,
                        receiverOpt: null,
                        method: userMain,
                        arguments: arguments,
                        argumentNamesOpt: default(ImmutableArray<string>),
                        argumentRefKindsOpt: default(ImmutableArray<RefKind>),
                        isDelegateCall: false,
                        expanded: false,
                        invokedAsExtensionMethod: false,
                        argsToParamsOpt: default(ImmutableArray<int>),
                        resultKind: LookupResultKind.Viable,
                        binderOpt: binder,
                        type: userMain.ReturnType.TypeSymbol)
                { WasCompilerGenerated = true };

                // The diagnostics that would be produced here will already have been captured and returned.
                var droppedBag = DiagnosticBag.GetInstance();
                var success = binder.GetAwaitableExpressionInfo(userMainInvocation, out _, out _, out _, out _getAwaiterGetResultCall, _userMainReturnTypeSyntax, droppedBag);
                droppedBag.Free();

                Debug.Assert(
                    ReturnType.SpecialType == SpecialType.System_Void ||
                    ReturnType.SpecialType == SpecialType.System_Int32);
            }

            public override string Name => MainName;

            public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

            public override TypeSymbolWithAnnotations ReturnType => TypeSymbolWithAnnotations.Create(_getAwaiterGetResultCall.Type);

            internal override BoundBlock CreateBody(DiagnosticBag diagnostics)
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
                                expressionOpt: null
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
                                expressionOpt: _getAwaiterGetResultCall
                            )
                        )
                    )
                    { WasCompilerGenerated = true };
                }
            }
        }

        private sealed class ScriptEntryPoint : SynthesizedEntryPointSymbol
        {
            private readonly TypeSymbolWithAnnotations _returnType;

            internal ScriptEntryPoint(NamedTypeSymbol containingType, TypeSymbolWithAnnotations returnType) :
                base(containingType)
            {
                Debug.Assert(containingType.IsScriptClass);
                Debug.Assert(returnType.SpecialType == SpecialType.System_Void);
                _returnType = returnType;
            }

            public override string Name => MainName;

            public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

            public override TypeSymbolWithAnnotations ReturnType => _returnType;

            // private static void <Main>()
            // {
            //     var script = new Script();
            //     script.<Initialize>().GetAwaiter().GetResult();
            // }
            internal override BoundBlock CreateBody(DiagnosticBag diagnostics)
            {
                var syntax = DummySyntax();
                var compilation = _containingType.DeclaringCompilation;

                // Creates a new top-level binder that just contains the global imports for the compilation.
                // The imports are required if a consumer of the scripting API is using a Task implementation 
                // that uses extension methods.
                var binder = new InContainerBinder(
                    container: null,
                    next: new BuckStopsHereBinder(compilation),
                    imports: compilation.GlobalImports);
                binder = new InContainerBinder(compilation.GlobalNamespace, binder);

                var ctor = _containingType.GetScriptConstructor();
                Debug.Assert(ctor.ParameterCount == 0);

                var initializer = _containingType.GetScriptInitializer();
                Debug.Assert(initializer.ParameterCount == 0);

                var scriptLocal = new BoundLocal(
                    syntax,
                    new SynthesizedLocal(this, TypeSymbolWithAnnotations.Create(_containingType), SynthesizedLocalKind.LoweringTemp),
                    null,
                    _containingType)
                { WasCompilerGenerated = true };

                var initializeCall = CreateParameterlessCall(syntax, scriptLocal, initializer);
                BoundExpression getAwaiterGetResultCall;
                if (!binder.GetAwaitableExpressionInfo(initializeCall, out _, out _, out _, out getAwaiterGetResultCall, syntax, diagnostics))
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
                                    ctor,
                                    null)
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
                            null)
                        { WasCompilerGenerated = true }))
                { WasCompilerGenerated = true };
            }
        }

        private sealed class SubmissionEntryPoint : SynthesizedEntryPointSymbol
        {
            private readonly ImmutableArray<ParameterSymbol> _parameters;
            private readonly TypeSymbolWithAnnotations _returnType;

            internal SubmissionEntryPoint(NamedTypeSymbol containingType, TypeSymbolWithAnnotations returnType, TypeSymbol submissionArrayType) :
                base(containingType)
            {
                Debug.Assert(containingType.IsSubmissionClass);
                Debug.Assert(returnType.SpecialType != SpecialType.System_Void);
                _parameters = ImmutableArray.Create(SynthesizedParameterSymbol.Create(this,
                    TypeSymbolWithAnnotations.Create(submissionArrayType), 0, RefKind.None, "submissionArray"));

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

            public override TypeSymbolWithAnnotations ReturnType => _returnType;

            // private static T <Factory>(object[] submissionArray) 
            // {
            //     var submission = new Submission#N(submissionArray);
            //     return submission.<Initialize>();
            // }
            internal override BoundBlock CreateBody(DiagnosticBag diagnostics)
            {
                var syntax = DummySyntax();

                var ctor = _containingType.GetScriptConstructor();
                Debug.Assert(ctor.ParameterCount == 1);

                var initializer = _containingType.GetScriptInitializer();
                Debug.Assert(initializer.ParameterCount == 0);

                var submissionArrayParameter = new BoundParameter(syntax, _parameters[0]) { WasCompilerGenerated = true };
                var submissionLocal = new BoundLocal(
                    syntax,
                    new SynthesizedLocal(this, TypeSymbolWithAnnotations.Create(_containingType), SynthesizedLocalKind.LoweringTemp),
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
                            default(ImmutableArray<string>),
                            default(ImmutableArray<RefKind>),
                            false,
                            default(ImmutableArray<int>),
                            null,
                            null,
                            null,
                            _containingType)
                        { WasCompilerGenerated = true },
                        _containingType)
                    { WasCompilerGenerated = true })
                { WasCompilerGenerated = true };

                // return submission.<Initialize>();
                var initializeResult = CreateParameterlessCall(
                    syntax,
                    submissionLocal,
                    initializer);
                Debug.Assert(TypeSymbol.Equals(initializeResult.Type, _returnType.TypeSymbol, TypeCompareKind.ConsiderEverything2));
                var returnStatement = new BoundReturnStatement(
                    syntax,
                    RefKind.None,
                    initializeResult)
                { WasCompilerGenerated = true };

                return new BoundBlock(syntax,
                    ImmutableArray.Create<LocalSymbol>(submissionLocal.LocalSymbol),
                    ImmutableArray.Create<BoundStatement>(submissionAssignment, returnStatement))
                { WasCompilerGenerated = true };
            }
        }
    }
}
