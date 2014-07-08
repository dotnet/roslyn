// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents an interactive code entry point that is inserted into the compilation if there is not an existing one. 
    /// </summary>
    internal sealed class SynthesizedEntryPointSymbol : MethodSymbol
    {
        private readonly NamedTypeSymbol containingType;
        private readonly ImmutableArray<ParameterSymbol> parameters;
        private readonly TypeSymbol returnType;
        private readonly string name;

        internal SynthesizedEntryPointSymbol(NamedTypeSymbol containingType, TypeSymbol returnType, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)containingType != null);
            this.containingType = containingType;

            if (containingType.ContainingAssembly.IsInteractive)
            {
                var interactiveSessionType = this.DeclaringCompilation.GetWellKnownType(WellKnownType.Microsoft_CSharp_RuntimeHelpers_Session);
                var useSiteDiagnostic = interactiveSessionType.GetUseSiteDiagnostic();
                if (useSiteDiagnostic != null)
                {
                    Symbol.ReportUseSiteDiagnostic(useSiteDiagnostic, diagnostics, NoLocation.Singleton);
                }

                this.parameters = ImmutableArray.Create<ParameterSymbol>(new SynthesizedParameterSymbol(this, interactiveSessionType, 0, RefKind.None, "session"));
                this.name = "<Factory>";
            }
            else
            {
                this.parameters = ImmutableArray<ParameterSymbol>.Empty;
                this.name = "<Main>";
            }

            this.returnType = returnType;
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        internal BoundBlock CreateBody()
        {
            return this.DeclaringCompilation.IsSubmission ? CreateSubmissionFactoryBody() : CreateScriptBody();
        }

        // Generates:
        //
        // private static void {Main}()
        // {
        //     new {ThisScriptClass}();
        // }
        private BoundBlock CreateScriptBody()
        {
            Debug.Assert(containingType.IsScriptClass);

            SyntaxTree syntaxTree = CSharpSyntaxTree.Dummy;
            CSharpSyntaxNode syntax = (CSharpSyntaxNode)syntaxTree.GetRoot();

            return new BoundBlock(syntax,
                ImmutableArray<LocalSymbol>.Empty,
                ImmutableArray.Create<BoundStatement>(
                    new BoundExpressionStatement(syntax,
                        new BoundObjectCreationExpression(
                            syntax,
                            containingType.InstanceConstructors.Single())
            { WasCompilerGenerated = true })
            { WasCompilerGenerated = true },
                    new BoundReturnStatement(syntax, null) { WasCompilerGenerated = true })
            );
        }

        // Generates:
        // 
        // private static T {Factory}(InteractiveSession session) 
        // {
        //    T submissionResult;
        //    new {ThisScriptClass}(session, out submissionResult);
        //    return submissionResult;
        // }
        private BoundBlock CreateSubmissionFactoryBody()
        {
            Debug.Assert(containingType.TypeKind == TypeKind.Submission);

            SyntaxTree syntaxTree = CSharpSyntaxTree.Dummy;
            CSharpSyntaxNode syntax = (CSharpSyntaxNode)syntaxTree.GetRoot();

            var interactiveSessionParam = new BoundParameter(syntax, parameters[0]) { WasCompilerGenerated = true };

            var ctor = containingType.InstanceConstructors.Single();
            Debug.Assert(ctor is SynthesizedInstanceConstructor);
            Debug.Assert(ctor.ParameterCount == 2);

            var submissionResultType = ctor.Parameters[1].Type;
            var submissionResult = new BoundLocal(syntax, new SynthesizedLocal(ctor, submissionResultType, SynthesizedLocalKind.LoweringTemp), null, submissionResultType) { WasCompilerGenerated = true };

            return new BoundBlock(syntax,
                // T submissionResult;
                ImmutableArray.Create<LocalSymbol>(submissionResult.LocalSymbol),
                ImmutableArray.Create<BoundStatement>(
                    // new Submission(interactiveSession, out submissionResult);
                    new BoundExpressionStatement(syntax,
                        new BoundObjectCreationExpression(
                            syntax,
                            ctor,
                            ImmutableArray.Create<BoundExpression>(interactiveSessionParam, submissionResult),
                            ImmutableArray<string>.Empty,
                            ImmutableArray.Create<RefKind>(RefKind.None, RefKind.Ref),
                            false,
                            default(ImmutableArray<int>),
                            null,
                            null,
                            containingType
                        )
            { WasCompilerGenerated = true })
            { WasCompilerGenerated = true },
                    // return submissionResult;
                    new BoundReturnStatement(syntax, submissionResult) { WasCompilerGenerated = true }))
            { WasCompilerGenerated = true };
        }

        public override Symbol ContainingSymbol
        {
            get { return containingType; }
        }

        public override string Name
        {
            get { return name; }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
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

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return parameters; }
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
            get { return returnType; }
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
            get { return this.ReturnType.SpecialType == SpecialType.System_Void; }
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

        internal override Microsoft.Cci.CallingConvention CallingConvention
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

        internal override bool IsMetadataFinal()
        {
            return false;
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

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }
    }
}
