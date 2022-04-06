// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public abstract class ExpressionCompilerTestBase : CSharpTestBase, IDisposable
    {
        private readonly ArrayBuilder<IDisposable> _runtimeInstances = ArrayBuilder<IDisposable>.GetInstance();

        internal static readonly ImmutableArray<Alias> NoAliases = ImmutableArray<Alias>.Empty;

        protected ExpressionCompilerTestBase()
        {
            // We never want to swallow Exceptions (generate a non-fatal Watson) when running tests.
            ExpressionEvaluatorFatalError.IsFailFastEnabled = true;
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var instance in _runtimeInstances)
            {
                instance.Dispose();
            }
            _runtimeInstances.Free();
        }

        internal static void WithRuntimeInstance(Compilation compilation, Action<RuntimeInstance> validator)
        {
            WithRuntimeInstance(compilation, null, validator: validator);
        }

        internal static void WithRuntimeInstance(Compilation compilation, IEnumerable<MetadataReference> references, Action<RuntimeInstance> validator)
        {
            WithRuntimeInstance(compilation, references, includeLocalSignatures: true, includeIntrinsicAssembly: true, validator: validator);
        }

        internal static void WithRuntimeInstance(
            Compilation compilation,
            IEnumerable<MetadataReference> references,
            bool includeLocalSignatures,
            bool includeIntrinsicAssembly,
            Action<RuntimeInstance> validator)
        {
            foreach (var debugFormat in new[] { DebugInformationFormat.Pdb, DebugInformationFormat.PortablePdb })
            {
                using (var instance = RuntimeInstance.Create(compilation, references, debugFormat, includeLocalSignatures, includeIntrinsicAssembly))
                {
                    validator(instance);
                }
            }
        }

        internal RuntimeInstance CreateRuntimeInstance(IEnumerable<ModuleInstance> modules)
        {
            var instance = RuntimeInstance.Create(modules);
            _runtimeInstances.Add(instance);
            return instance;
        }

        internal RuntimeInstance CreateRuntimeInstance(
            Compilation compilation,
            IEnumerable<MetadataReference> references = null,
            DebugInformationFormat debugFormat = DebugInformationFormat.Pdb,
            bool includeLocalSignatures = true)
        {
            var instance = RuntimeInstance.Create(compilation, references, debugFormat, includeLocalSignatures, includeIntrinsicAssembly: true);
            _runtimeInstances.Add(instance);
            return instance;
        }

        internal RuntimeInstance CreateRuntimeInstance(
            ModuleInstance module,
            IEnumerable<MetadataReference> references)
        {
            var instance = RuntimeInstance.Create(module, references, DebugInformationFormat.Pdb);
            _runtimeInstances.Add(instance);
            return instance;
        }

        internal sealed class AppDomain
        {
            private MetadataContext<CSharpMetadataContext> _metadataContext;

            internal MetadataContext<CSharpMetadataContext> GetMetadataContext()
            {
                return _metadataContext;
            }

            internal void SetMetadataContext(MetadataContext<CSharpMetadataContext> metadataContext)
            {
                _metadataContext = metadataContext;
            }

            internal void RemoveMetadataContext()
            {
                _metadataContext = default;
            }
        }

        internal static EvaluationContext CreateTypeContext(
            AppDomain appDomain,
            ImmutableArray<MetadataBlock> blocks,
            Guid moduleVersionId,
            int typeToken,
            MakeAssemblyReferencesKind kind = MakeAssemblyReferencesKind.AllAssemblies)
        {
            return CSharpExpressionCompiler.CreateTypeContext(
                appDomain,
                ad => ad.GetMetadataContext(),
                blocks,
                moduleVersionId,
                typeToken,
                kind);
        }

        internal static EvaluationContext CreateMethodContext(
            AppDomain appDomain,
            ImmutableArray<MetadataBlock> blocks,
            ISymUnmanagedReader symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken,
            MakeAssemblyReferencesKind kind = MakeAssemblyReferencesKind.AllAssemblies)
        {
            return CSharpExpressionCompiler.CreateMethodContext(
                appDomain,
                ad => ad.GetMetadataContext(),
                (ad, mc, report) => ad.SetMetadataContext(mc),
                blocks,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken,
                kind);
        }

        internal static EvaluationContext CreateMethodContext(
            AppDomain appDomain,
            ImmutableArray<MetadataBlock> blocks,
            (Guid ModuleVersionId, ISymUnmanagedReader SymReader, int MethodToken, int LocalSignatureToken, uint ILOffset) state,
            MakeAssemblyReferencesKind kind = MakeAssemblyReferencesKind.AllReferences)
        {
            return CreateMethodContext(
                appDomain,
                blocks,
                state.SymReader,
                state.ModuleVersionId,
                state.MethodToken,
                methodVersion: 1,
                state.ILOffset,
                state.LocalSignatureToken,
                kind);
        }

        internal static CSharpMetadataContext GetMetadataContext(MetadataContext<CSharpMetadataContext> appDomainContext, Guid mvid = default)
        {
            var assemblyContexts = appDomainContext.AssemblyContexts;
            return assemblyContexts != null && assemblyContexts.TryGetValue(new MetadataContextId(mvid), out CSharpMetadataContext context) ?
                context :
                default;
        }

        internal static MetadataContext<CSharpMetadataContext> SetMetadataContext(MetadataContext<CSharpMetadataContext> appDomainContext, Guid mvid, CSharpMetadataContext context)
        {
            return new MetadataContext<CSharpMetadataContext>(
                appDomainContext.MetadataBlocks,
                appDomainContext.AssemblyContexts.SetItem(new MetadataContextId(mvid), context));
        }

        internal static (Guid ModuleVersionId, ISymUnmanagedReader SymReader, int MethodToken, int LocalSignatureToken, uint ILOffset) GetContextState(RuntimeInstance runtime, string methodName)
        {
            Guid moduleVersionId;
            ISymUnmanagedReader symReader;
            int methodToken;
            int localSignatureToken;
            GetContextState(
                runtime,
                methodName,
                out _,
                out moduleVersionId,
                out symReader,
                out methodToken,
                out localSignatureToken);
            uint ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader);
            return (moduleVersionId, symReader, methodToken, localSignatureToken, ilOffset);
        }

        internal static void GetContextState(
            RuntimeInstance runtime,
            string methodOrTypeName,
            out ImmutableArray<MetadataBlock> blocks,
            out Guid moduleVersionId,
            out ISymUnmanagedReader symReader,
            out int methodOrTypeToken,
            out int localSignatureToken)
        {
            var moduleInstances = runtime.Modules;
            blocks = moduleInstances.SelectAsArray(m => m.MetadataBlock);

            var compilation = blocks.ToCompilation(default(Guid), MakeAssemblyReferencesKind.AllAssemblies);

            var methodOrType = GetMethodOrTypeBySignature(compilation, methodOrTypeName);

            var module = (PEModuleSymbol)methodOrType.ContainingModule;
            var id = module.Module.GetModuleVersionIdOrThrow();
            var moduleInstance = moduleInstances.First(m => m.ModuleVersionId == id);

            moduleVersionId = id;
            symReader = (ISymUnmanagedReader)moduleInstance.SymReader;

            EntityHandle methodOrTypeHandle;
            if (methodOrType.Kind == SymbolKind.Method)
            {
                methodOrTypeHandle = ((PEMethodSymbol)methodOrType).Handle;
                localSignatureToken = moduleInstance.GetLocalSignatureToken((MethodDefinitionHandle)methodOrTypeHandle);
            }
            else
            {
                methodOrTypeHandle = ((PENamedTypeSymbol)methodOrType).Handle;
                localSignatureToken = -1;
            }

            MetadataReader reader = null; // null should be ok
            methodOrTypeToken = reader.GetToken(methodOrTypeHandle);
        }

        internal static EvaluationContext CreateMethodContext(
            RuntimeInstance runtime,
            string methodName,
            int atLineNumber = -1)
        {
            ImmutableArray<MetadataBlock> blocks;
            Guid moduleVersionId;
            ISymUnmanagedReader symReader;
            int methodToken;
            int localSignatureToken;
            GetContextState(runtime, methodName, out blocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);

            uint ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader, atLineNumber);

            return CreateMethodContext(
                new AppDomain(),
                blocks,
                symReader,
                moduleVersionId,
                methodToken: methodToken,
                methodVersion: 1,
                ilOffset: ilOffset,
                localSignatureToken: localSignatureToken,
                kind: MakeAssemblyReferencesKind.AllAssemblies);
        }

        internal static EvaluationContext CreateTypeContext(
            RuntimeInstance runtime,
            string typeName)
        {
            ImmutableArray<MetadataBlock> blocks;
            Guid moduleVersionId;
            ISymUnmanagedReader symReader;
            int typeToken;
            int localSignatureToken;
            GetContextState(runtime, typeName, out blocks, out moduleVersionId, out symReader, out typeToken, out localSignatureToken);
            return CreateTypeContext(
                new AppDomain(),
                blocks,
                moduleVersionId,
                typeToken,
                kind: MakeAssemblyReferencesKind.AllAssemblies);
        }

        internal CompilationTestData Evaluate(
            string source,
            OutputKind outputKind,
            string methodName,
            string expr,
            int atLineNumber = -1,
            bool includeSymbols = true,
            TargetFramework targetFramework = TargetFramework.Standard)
        {
            var result = Evaluate(source, outputKind, methodName, expr, out _, out string error, atLineNumber, includeSymbols, targetFramework: targetFramework);
            Assert.Null(error);
            return result;
        }

        internal CompilationTestData Evaluate(
            CSharpCompilation compilation,
            string methodName,
            string expr,
            int atLineNumber = -1,
            bool includeSymbols = true)
        {
            var result = Evaluate(compilation, methodName, expr, out _, out string error, atLineNumber, includeSymbols);
            Assert.Null(error);
            return result;
        }

        internal CompilationTestData Evaluate(
            string source,
            OutputKind outputKind,
            string methodName,
            string expr,
            out ResultProperties resultProperties,
            out string error,
            int atLineNumber = -1,
            bool includeSymbols = true,
            TargetFramework targetFramework = TargetFramework.Standard)
        {
            var compilation = CreateCompilation(
                source,
                parseOptions: SyntaxHelpers.ParseOptions,
                options: (outputKind == OutputKind.DynamicallyLinkedLibrary) ? TestOptions.DebugDll : TestOptions.DebugExe,
                targetFramework: targetFramework);

            return Evaluate(compilation, methodName, expr, out resultProperties, out error, atLineNumber, includeSymbols);
        }

        internal CompilationTestData Evaluate(
            CSharpCompilation compilation,
            string methodName,
            string expr,
            out ResultProperties resultProperties,
            out string error,
            int atLineNumber = -1,
            bool includeSymbols = true)
        {
            var runtime = CreateRuntimeInstance(compilation, debugFormat: includeSymbols ? DebugInformationFormat.Pdb : 0);
            var context = CreateMethodContext(runtime, methodName, atLineNumber);
            var testData = new CompilationTestData();
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            var result = context.CompileExpression(
                expr,
                DkmEvaluationFlags.TreatAsExpression,
                NoAliases,
                DebuggerDiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData);
            Assert.Empty(missingAssemblyIdentities);
            return testData;
        }

        /// <summary>
        /// Verify all type parameters from the method
        /// are from that method or containing types.
        /// </summary>
        internal static void VerifyTypeParameters(MethodSymbol method)
        {
            Assert.True(method.IsContainingSymbolOfAllTypeParameters(method.ReturnType));
            AssertEx.All(method.TypeParameters, typeParameter => method.IsContainingSymbolOfAllTypeParameters(typeParameter));
            AssertEx.All(method.TypeArgumentsWithAnnotations, typeArgument => method.IsContainingSymbolOfAllTypeParameters(typeArgument.Type));
            AssertEx.All(method.Parameters, parameter => method.IsContainingSymbolOfAllTypeParameters(parameter.Type));
            VerifyTypeParameters(method.ContainingType);
        }

        internal static void VerifyLocal(
            CompilationTestData testData,
            string typeName,
            LocalAndMethod localAndMethod,
            string expectedMethodName,
            string expectedLocalName,
            string expectedLocalDisplayName = null,
            DkmClrCompilationResultFlags expectedFlags = DkmClrCompilationResultFlags.None,
            string expectedILOpt = null,
            bool expectedGeneric = false,
            [CallerFilePath] string expectedValueSourcePath = null,
            [CallerLineNumber] int expectedValueSourceLine = 0)
        {
            ExpressionCompilerTestHelpers.VerifyLocal<MethodSymbol>(
                testData,
                typeName,
                localAndMethod,
                expectedMethodName,
                expectedLocalName,
                expectedLocalDisplayName ?? expectedLocalName,
                expectedFlags,
                VerifyTypeParameters,
                expectedILOpt,
                expectedGeneric,
                expectedValueSourcePath,
                expectedValueSourceLine);
        }

        /// <summary>
        /// Verify all type parameters from the type
        /// are from that type or containing types.
        /// </summary>
        internal static void VerifyTypeParameters(NamedTypeSymbol type)
        {
            AssertEx.All(type.TypeParameters, typeParameter => type.IsContainingSymbolOfAllTypeParameters(typeParameter));
            AssertEx.All(type.TypeArguments(), typeArgument => type.IsContainingSymbolOfAllTypeParameters(typeArgument));
            var container = type.ContainingType;
            if ((object)container != null)
            {
                VerifyTypeParameters(container);
            }
        }

        internal static Symbol GetMethodOrTypeBySignature(Compilation compilation, string signature)
        {
            string[] parameterTypeNames;
            var methodOrTypeName = ExpressionCompilerTestHelpers.GetMethodOrTypeSignatureParts(signature, out parameterTypeNames);

            var candidates = compilation.GetMembers(methodOrTypeName);
            var methodOrType = (parameterTypeNames == null) ?
                candidates.FirstOrDefault() :
                candidates.FirstOrDefault(c => parameterTypeNames.SequenceEqual(((MethodSymbol)c).Parameters.Select(p => p.TypeWithAnnotations.Type.Name)));

            Assert.False(methodOrType == null, "Could not find method or type with signature '" + signature + "'.");
            return methodOrType;
        }

        internal static Alias VariableAlias(string name, Type type = null)
        {
            return VariableAlias(name, (type ?? typeof(object)).AssemblyQualifiedName);
        }

        internal static Alias VariableAlias(string name, string typeAssemblyQualifiedName)
        {
            return new Alias(DkmClrAliasKind.Variable, name, name, typeAssemblyQualifiedName, default(Guid), null);
        }

        internal static Alias ObjectIdAlias(uint id, Type type = null)
        {
            return ObjectIdAlias(id, (type ?? typeof(object)).AssemblyQualifiedName);
        }

        internal static Alias ObjectIdAlias(uint id, string typeAssemblyQualifiedName)
        {
            Assert.NotEqual(0u, id); // Not a valid id.
            var name = $"${id}";
            return new Alias(DkmClrAliasKind.ObjectId, name, name, typeAssemblyQualifiedName, default(Guid), null);
        }

        internal static Alias ReturnValueAlias(int id = -1, Type type = null)
        {
            return ReturnValueAlias(id, (type ?? typeof(object)).AssemblyQualifiedName);
        }

        internal static Alias ReturnValueAlias(int id, string typeAssemblyQualifiedName)
        {
            var name = $"Method M{(id < 0 ? "" : id.ToString())} returned";
            var fullName = id < 0 ? "$ReturnValue" : $"$ReturnValue{id}";
            return new Alias(DkmClrAliasKind.ReturnValue, name, fullName, typeAssemblyQualifiedName, default(Guid), null);
        }

        internal static Alias ExceptionAlias(Type type = null, bool stowed = false)
        {
            return ExceptionAlias((type ?? typeof(Exception)).AssemblyQualifiedName, stowed);
        }

        internal static Alias ExceptionAlias(string typeAssemblyQualifiedName, bool stowed = false)
        {
            var name = "Error";
            var fullName = stowed ? "$stowedexception" : "$exception";
            var kind = stowed ? DkmClrAliasKind.StowedException : DkmClrAliasKind.Exception;
            return new Alias(kind, name, fullName, typeAssemblyQualifiedName, default(Guid), null);
        }

        internal static Alias Alias(DkmClrAliasKind kind, string name, string fullName, string type, ReadOnlyCollection<byte> payload)
        {
            return new Alias(kind, name, fullName, type, (payload == null) ? default(Guid) : CustomTypeInfo.PayloadTypeId, payload);
        }

        internal static MethodDebugInfo<TypeSymbol, LocalSymbol> GetMethodDebugInfo(RuntimeInstance runtime, string qualifiedMethodName, int ilOffset = 0)
        {
            var peCompilation = runtime.Modules.SelectAsArray(m => m.MetadataBlock).ToCompilation(default(Guid), MakeAssemblyReferencesKind.AllAssemblies);
            var peMethod = peCompilation.GlobalNamespace.GetMember<PEMethodSymbol>(qualifiedMethodName);
            var peModule = (PEModuleSymbol)peMethod.ContainingModule;

            var symReader = runtime.Modules.Single(mi => mi.ModuleVersionId == peModule.Module.GetModuleVersionIdOrThrow()).SymReader;
            var symbolProvider = new CSharpEESymbolProvider(peCompilation.SourceAssembly, peModule, peMethod);

            return MethodDebugInfo<TypeSymbol, LocalSymbol>.ReadMethodDebugInfo((ISymUnmanagedReader3)symReader, symbolProvider, MetadataTokens.GetToken(peMethod.Handle), methodVersion: 1, ilOffset: ilOffset, isVisualBasicMethod: false);
        }

        internal static void CheckAttribute(IEnumerable<byte> assembly, MethodSymbol method, AttributeDescription description, bool expected)
        {
            var module = AssemblyMetadata.CreateFromImage(assembly).GetModules().Single().Module;

            var typeName = method.ContainingType.Name;
            var typeHandle = module.MetadataReader.TypeDefinitions
                .Single(handle => module.GetTypeDefNameOrThrow(handle) == typeName);

            var methodName = method.Name;
            var methodHandle = module
                .GetMethodsOfTypeOrThrow(typeHandle)
                .Single(handle => module.GetMethodDefNameOrThrow(handle) == methodName);

            var returnParamHandle = module.GetParametersOfMethodOrThrow(methodHandle).FirstOrDefault();

            if (returnParamHandle.IsNil)
            {
                Assert.False(expected);
            }
            else
            {
                var attributes = module
                    .GetCustomAttributesOrThrow(returnParamHandle)
                    .Where(handle => module.GetTargetAttributeSignatureIndex(handle, description) != -1);

                if (expected)
                {
                    Assert.Equal(1, attributes.Count());
                }
                else
                {
                    Assert.Empty(attributes);
                }
            }
        }
    }
}
