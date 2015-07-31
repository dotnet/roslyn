// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

        internal static SynthesizedEntryPointSymbol Create(NamedTypeSymbol containingType, TypeSymbol returnType, DiagnosticBag diagnostics)
        {
            var compilation = containingType.DeclaringCompilation;
            if (containingType.ContainingAssembly.IsInteractive)
            {
                var submissionArrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Object));
                var useSiteDiagnostic = submissionArrayType.GetUseSiteDiagnostic();
                if (useSiteDiagnostic != null)
                {
                    ReportUseSiteDiagnostic(useSiteDiagnostic, diagnostics, NoLocation.Singleton);
                }
                return new SubmissionEntryPoint(containingType, returnType, submissionArrayType);
            }
            else
            {
                return new ScriptEntryPoint(containingType, returnType);
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

        public override TypeSymbol ReturnType
        {
            get { return _returnType; }
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
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

        private sealed class ScriptEntryPoint : SynthesizedEntryPointSymbol
        {
            internal ScriptEntryPoint(NamedTypeSymbol containingType, TypeSymbol returnType) :
                base(containingType, returnType)
            {
                Debug.Assert(containingType.IsScriptClass);
                Debug.Assert(returnType.SpecialType == SpecialType.System_Void);
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
            //     script.<Initialize>();
            // }
            internal override BoundBlock CreateBody()
            {
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
                        // script.<Initialize>();
                        new BoundExpressionStatement(
                            syntax,
                            new BoundCall(
                                syntax,
                                scriptLocal,
                                initializer,
                                ImmutableArray<BoundExpression>.Empty,
                                default(ImmutableArray<string>),
                                default(ImmutableArray<RefKind>),
                                isDelegateCall: false,
                                expanded: false,
                                invokedAsExtensionMethod: false,
                                argsToParamsOpt: default(ImmutableArray<int>),
                                resultKind: LookupResultKind.Viable,
                                type: initializer.ReturnType)
                            { WasCompilerGenerated = true })
                        { WasCompilerGenerated = true },
                        // return;
                        new BoundReturnStatement(
                            syntax,
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
                var initializeResult = new BoundCall(
                    syntax,
                    submissionLocal,
                    initializer,
                    ImmutableArray<BoundExpression>.Empty,
                    default(ImmutableArray<string>),
                    default(ImmutableArray<RefKind>),
                    isDelegateCall: false,
                    expanded: false,
                    invokedAsExtensionMethod: false,
                    argsToParamsOpt: default(ImmutableArray<int>),
                    resultKind: LookupResultKind.Viable,
                    type: initializer.ReturnType)
                { WasCompilerGenerated = true };
                Debug.Assert(initializeResult.Type == _returnType);
                var returnStatement = new BoundReturnStatement(
                    syntax,
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