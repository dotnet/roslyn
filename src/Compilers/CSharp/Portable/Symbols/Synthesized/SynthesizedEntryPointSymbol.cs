// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private readonly TypeSymbolWithAnnotations _returnType;

        internal static SynthesizedEntryPointSymbol Create(SynthesizedInteractiveInitializerMethod initializerMethod, DiagnosticBag diagnostics)
        {
            var containingType = initializerMethod.ContainingType;
            var compilation = containingType.DeclaringCompilation;
            if (compilation.IsSubmission)
            {
                var submissionArrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Object));
                ReportUseSiteDiagnostics(submissionArrayType, diagnostics);
                return new SubmissionEntryPoint(
                    containingType,
                    initializerMethod.ReturnType,
                    submissionArrayType);
            }
            else
            {
                var taskType = compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task);
#if DEBUG
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                Debug.Assert(taskType.IsErrorType() || initializerMethod.ReturnType.TypeSymbol.IsDerivedFrom(taskType, ignoreDynamic: true, useSiteDiagnostics: ref useSiteDiagnostics));
#endif
                ReportUseSiteDiagnostics(taskType, diagnostics);
                var getAwaiterMethod = taskType.IsErrorType() ?
                    null :
                    GetRequiredMethod(taskType, WellKnownMemberNames.GetAwaiter, diagnostics);
                var getResultMethod = ((object)getAwaiterMethod == null) ?
                    null :
                    GetRequiredMethod(getAwaiterMethod.ReturnType.TypeSymbol, WellKnownMemberNames.GetResult, diagnostics);
                return new ScriptEntryPoint(
                    containingType,
                    TypeSymbolWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Void)),
                    getAwaiterMethod,
                    getResultMethod);
            }
        }

        private SynthesizedEntryPointSymbol(NamedTypeSymbol containingType, TypeSymbolWithAnnotations returnType)
        {
            Debug.Assert((object)containingType != null);
            Debug.Assert((object)returnType != null);

            _containingType = containingType;
            _returnType = returnType;
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        internal abstract BoundBlock CreateBody();

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

        internal override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override TypeSymbolWithAnnotations ReturnType
        {
            get { return _returnType; }
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
            get { return _returnType.SpecialType == SpecialType.System_Void; }
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

        private CSharpSyntaxNode GetSyntax()
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

        private static MethodSymbol GetRequiredMethod(TypeSymbol type, string methodName, DiagnosticBag diagnostics)
        {
            var method = type.GetMembers(methodName).SingleOrDefault() as MethodSymbol;
            if ((object)method == null)
            {
                diagnostics.Add(ErrorCode.ERR_MissingPredefinedMember, NoLocation.Singleton, type, methodName);
            }
            return method;
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
                type: method.ReturnType.TypeSymbol)
            { WasCompilerGenerated = true };
        }

        private sealed class ScriptEntryPoint : SynthesizedEntryPointSymbol
        {
            private readonly MethodSymbol _getAwaiterMethod;
            private readonly MethodSymbol _getResultMethod;

            internal ScriptEntryPoint(NamedTypeSymbol containingType, TypeSymbolWithAnnotations returnType, MethodSymbol getAwaiterMethod, MethodSymbol getResultMethod) :
                base(containingType, returnType)
            {
                Debug.Assert(containingType.IsScriptClass);
                Debug.Assert(returnType.SpecialType == SpecialType.System_Void);

                _getAwaiterMethod = getAwaiterMethod;
                _getResultMethod = getResultMethod;
            }

            public override string Name
            {
                get { return MainName; }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return ImmutableArray<ParameterSymbol>.Empty; }
            }

            // private static void <Main>()
            // {
            //     var script = new Script();
            //     script.<Initialize>().GetAwaiter().GetResult();
            // }
            internal override BoundBlock CreateBody()
            {
                // CreateBody should only be called if no errors.
                Debug.Assert((object)_getAwaiterMethod != null);
                Debug.Assert((object)_getResultMethod != null);

                var syntax = this.GetSyntax();

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

                return new BoundBlock(syntax,
                    ImmutableArray.Create<LocalSymbol>(scriptLocal.LocalSymbol),
                    ImmutableArray<LocalFunctionSymbol>.Empty,
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
                        new BoundExpressionStatement(
                            syntax,
                            CreateParameterlessCall(
                                syntax,
                                CreateParameterlessCall(
                                    syntax,
                                    CreateParameterlessCall(
                                        syntax,
                                        scriptLocal,
                                        initializer),
                                    _getAwaiterMethod),
                                _getResultMethod))
                        { WasCompilerGenerated = true },
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

            internal SubmissionEntryPoint(NamedTypeSymbol containingType, TypeSymbolWithAnnotations returnType, TypeSymbol submissionArrayType) :
                base(containingType, returnType)
            {
                Debug.Assert(containingType.IsSubmissionClass);
                Debug.Assert(returnType.SpecialType != SpecialType.System_Void);
                _parameters = ImmutableArray.Create<ParameterSymbol>(new SynthesizedParameterSymbol(this, submissionArrayType, 0, RefKind.None, "submissionArray"));
            }

            public override string Name
            {
                get { return FactoryName; }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return _parameters; }
            }

            // private static T <Factory>(object[] submissionArray) 
            // {
            //     var submission = new Submission#N(submissionArray);
            //     return submission.<Initialize>();
            // }
            internal override BoundBlock CreateBody()
            {
                var syntax = this.GetSyntax();

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
                Debug.Assert(initializeResult.Type == _returnType.TypeSymbol);
                var returnStatement = new BoundReturnStatement(
                    syntax,
                    RefKind.None,
                    initializeResult)
                { WasCompilerGenerated = true };

                return new BoundBlock(syntax,
                    ImmutableArray.Create<LocalSymbol>(submissionLocal.LocalSymbol),
                    ImmutableArray<LocalFunctionSymbol>.Empty,
                    ImmutableArray.Create<BoundStatement>(submissionAssignment, returnStatement))
                { WasCompilerGenerated = true };
            }
        }
    }
}