// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        private readonly TypeSymbol _returnType;

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
                Debug.Assert(taskType.IsErrorType() || initializerMethod.ReturnType.IsDerivedFrom(taskType, TypeCompareKind.IgnoreDynamicAndTupleNames, useSiteDiagnostics: ref useSiteDiagnostics));
#endif
                ReportUseSiteDiagnostics(taskType, diagnostics);
                var getAwaiterMethod = taskType.IsErrorType() ?
                    null :
                    GetRequiredMethod(taskType, WellKnownMemberNames.GetAwaiter, diagnostics);
                var getResultMethod = ((object)getAwaiterMethod == null) ?
                    null :
                    GetRequiredMethod(getAwaiterMethod.ReturnType, WellKnownMemberNames.GetResult, diagnostics);
                return new ScriptEntryPoint(
                    containingType,
                    compilation.GetSpecialType(SpecialType.System_Void),
                    getAwaiterMethod,
                    getResultMethod);
            }
        }

        private SynthesizedEntryPointSymbol(NamedTypeSymbol containingType, TypeSymbol returnType)
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

        public override TypeSymbol ReturnType
        {
            get { return _returnType; }
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get { return ImmutableArray<TypeSymbol>.Empty; }
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
                type: method.ReturnType)
            { WasCompilerGenerated = true };
        }

        internal sealed class AsyncForwardEntryPoint : SynthesizedEntryPointSymbol
        {
            // if _paramType is null, this is a main method that takes no args.
            private MethodSymbol _userMain;
            private DiagnosticBag _diagnosticBag;
            private CSharpCompilation _compilation;

            private ReturnKind _returnKind;
            private ParamKind _paramKind;

            private ImmutableArray<ParameterSymbol> _parameters;

            private enum ReturnKind
            {
                VoidReturning,
                IntReturning,
            }
            private enum ParamKind
            {
                NoArgs,
                StringArrayArgs,
            }

            private static TypeSymbol TranslateReturnType(CSharpCompilation compilation, TypeSymbol returnType) {
                if (returnType.IsGenericTaskType(compilation)) {
                    return compilation.GetSpecialType(SpecialType.System_Int32);
                } 
                else {
                    return compilation.GetSpecialType(SpecialType.System_Void);
                }
            }

            private static ReturnKind GetReturnKind(CSharpCompilation compilation, TypeSymbol returnType) {
                if (returnType.IsGenericTaskType(compilation)) {
                    return ReturnKind.IntReturning;
                } 
                else {
                    return ReturnKind.VoidReturning;
                }
            }


            internal AsyncForwardEntryPoint(CSharpCompilation compilation, DiagnosticBag diagnosticBag, NamedTypeSymbol containingType, MethodSymbol userMain): 
                base(containingType, TranslateReturnType(compilation, userMain.ReturnType)) {
                Debug.Assert(userMain != null);

                _userMain = userMain;
                _diagnosticBag = diagnosticBag;
                _compilation = compilation;
                _paramKind = userMain.ParameterCount == 0 ? ParamKind.NoArgs : ParamKind.StringArrayArgs;
                _returnKind = GetReturnKind(compilation, userMain.ReturnType);


                switch (_paramKind)
                {
                    case ParamKind.StringArrayArgs:
                        var stringType = _compilation.GetSpecialType(SpecialType.System_String);
                        var stringArrayType = ArrayTypeSymbol.CreateCSharpArray(_compilation.Assembly, stringType);
                        _parameters = ImmutableArray.Create(
                            SynthesizedParameterSymbol.Create(this, stringArrayType, 0, RefKind.None));
                        break;
                    default:
                        _parameters = ImmutableArray<ParameterSymbol>.Empty;
                        break;
                }
            }
             
            public override string Name => MainName;

            public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

            internal override BoundBlock CreateBody()
            {
                var syntax = this.GetSyntax();

                BoundCall userMainInvocation = new BoundCall(
                        syntax: this.GetSyntax(),
                        receiverOpt: null,
                        method: _userMain,
                        arguments:
                            (from parameterSymbol in Parameters
                             select new BoundParameter(
                                 syntax: syntax,
                                 parameterSymbol: parameterSymbol,
                                 type: parameterSymbol.Type) as BoundExpression).ToImmutableArray(),
                        argumentNamesOpt: ImmutableArray<string>.Empty,
                        argumentRefKindsOpt: ImmutableArray.Create(RefKind.None),
                        isDelegateCall: false,
                        expanded: false,
                        invokedAsExtensionMethod: false,
                        argsToParamsOpt: ImmutableArray<int>.Empty,
                        resultKind: LookupResultKind.Viable,
                        type: _userMain.ReturnType)
                { WasCompilerGenerated = true };


                var getAwaiterMethod = GetRequiredMethod(_userMain.ReturnType, WellKnownMemberNames.GetAwaiter, _diagnosticBag);
                Debug.Assert(getAwaiterMethod != null);
                var getResultMethod = GetRequiredMethod(getAwaiterMethod.ReturnType, WellKnownMemberNames.GetResult, _diagnosticBag);
                Debug.Assert(getResultMethod != null);

                BoundCall getAwaiterGetResult = 
                    CreateParameterlessCall(
                            syntax: syntax,
                            method: getResultMethod,
                            receiver: CreateParameterlessCall(
                                syntax: syntax,
                                method: getAwaiterMethod,
                                receiver: userMainInvocation
                            )
                    );

                BoundStatement statement = null;
                switch (_returnKind)
                {
                    case ReturnKind.IntReturning:
                        statement = new BoundReturnStatement(
                            syntax: syntax,
                            refKind: RefKind.None,
                            expressionOpt: getAwaiterGetResult
                        );
                        break;

                    case ReturnKind.VoidReturning:
                        statement = new BoundBlock (
                            syntax: syntax,
                            locals: ImmutableArray<LocalSymbol>.Empty,
                            statements: ImmutableArray.Create<BoundStatement>(
                                new BoundExpressionStatement(
                                    syntax: syntax,
                                    expression: getAwaiterGetResult
                                ),
                                new BoundReturnStatement(
                                    syntax: syntax,
                                    refKind: RefKind.None,
                                    expressionOpt: null
                                )
                            )
                        );
                        break;
                };

                return new BoundBlock(
                    syntax: syntax,
                    locals: ImmutableArray<LocalSymbol>.Empty,
                    statements: ImmutableArray.Create<BoundStatement>(statement));
                
            }
        }

        private sealed class ScriptEntryPoint : SynthesizedEntryPointSymbol
        {
            private readonly MethodSymbol _getAwaiterMethod;
            private readonly MethodSymbol _getResultMethod;

            internal ScriptEntryPoint(NamedTypeSymbol containingType, TypeSymbol returnType, MethodSymbol getAwaiterMethod, MethodSymbol getResultMethod) :
                base(containingType, returnType)
            {
                Debug.Assert(containingType.IsScriptClass);
                Debug.Assert(returnType.SpecialType == SpecialType.System_Void);

                _getAwaiterMethod = getAwaiterMethod;
                _getResultMethod = getResultMethod;
            }

            public override string Name => MainName;

            public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

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
                    new SynthesizedLocal(this, _containingType, SynthesizedLocalKind.LoweringTemp),
                    null,
                    _containingType)
                { WasCompilerGenerated = true };

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

            internal SubmissionEntryPoint(NamedTypeSymbol containingType, TypeSymbol returnType, TypeSymbol submissionArrayType) :
                base(containingType, returnType)
            {
                Debug.Assert(containingType.IsSubmissionClass);
                Debug.Assert(returnType.SpecialType != SpecialType.System_Void);
                _parameters = ImmutableArray.Create<ParameterSymbol>(SynthesizedParameterSymbol.Create(this, submissionArrayType, 0, RefKind.None, "submissionArray"));
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
                    new SynthesizedLocal(this, _containingType, SynthesizedLocalKind.LoweringTemp),
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
                Debug.Assert(initializeResult.Type == _returnType);
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