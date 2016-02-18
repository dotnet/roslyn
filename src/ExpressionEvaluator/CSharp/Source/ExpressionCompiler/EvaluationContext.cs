// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EvaluationContext : EvaluationContextBase
    {
        private const string TypeName = "<>x";
        private const string MethodName = "<>m0";
        internal const bool IsLocalScopeEndInclusive = false;

        internal readonly MethodContextReuseConstraints? MethodContextReuseConstraints;
        internal readonly CSharpCompilation Compilation;

        private readonly MethodSymbol _currentFrame;
        private readonly ImmutableArray<LocalSymbol> _locals;
        private readonly InScopeHoistedLocals _inScopeHoistedLocals;
        private readonly MethodDebugInfo<TypeSymbol, LocalSymbol> _methodDebugInfo;

        private EvaluationContext(
            MethodContextReuseConstraints? methodContextReuseConstraints,
            CSharpCompilation compilation,
            MethodSymbol currentFrame,
            ImmutableArray<LocalSymbol> locals,
            InScopeHoistedLocals inScopeHoistedLocals,
            MethodDebugInfo<TypeSymbol, LocalSymbol> methodDebugInfo)
        {
            Debug.Assert(inScopeHoistedLocals != null);
            Debug.Assert(methodDebugInfo != null);

            this.MethodContextReuseConstraints = methodContextReuseConstraints;
            this.Compilation = compilation;
            _currentFrame = currentFrame;
            _locals = locals;
            _inScopeHoistedLocals = inScopeHoistedLocals;
            _methodDebugInfo = methodDebugInfo;
        }

        /// <summary>
        /// Create a context for evaluating expressions at a type scope.
        /// </summary>
        /// <param name="previous">Previous context, if any, for possible re-use.</param>
        /// <param name="metadataBlocks">Module metadata</param>
        /// <param name="moduleVersionId">Module containing type</param>
        /// <param name="typeToken">Type metadata token</param>
        /// <returns>Evaluation context</returns>
        /// <remarks>
        /// No locals since locals are associated with methods, not types.
        /// </remarks>
        internal static EvaluationContext CreateTypeContext(
            CSharpMetadataContext previous,
            ImmutableArray<MetadataBlock> metadataBlocks,
            Guid moduleVersionId,
            int typeToken)
        {
            // Re-use the previous compilation if possible.
            var compilation = previous.Matches(metadataBlocks) ?
                previous.Compilation :
                metadataBlocks.ToCompilation();

            return CreateTypeContext(compilation, moduleVersionId, typeToken);
        }

        internal static EvaluationContext CreateTypeContext(
            CSharpCompilation compilation,
            Guid moduleVersionId,
            int typeToken)
        {
            Debug.Assert(MetadataTokens.Handle(typeToken).Kind == HandleKind.TypeDefinition);

            var currentType = compilation.GetType(moduleVersionId, typeToken);
            Debug.Assert((object)currentType != null);
            var currentFrame = new SynthesizedContextMethodSymbol(currentType);
            return new EvaluationContext(
                null,
                compilation,
                currentFrame,
                default(ImmutableArray<LocalSymbol>),
                InScopeHoistedLocals.Empty,
                MethodDebugInfo<TypeSymbol, LocalSymbol>.None);
        }

        /// <summary>
        /// Create a context for evaluating expressions within a method scope.
        /// </summary>
        /// <param name="previous">Previous context, if any, for possible re-use.</param>
        /// <param name="metadataBlocks">Module metadata</param>
        /// <param name="symReader"><see cref="ISymUnmanagedReader"/> for PDB associated with <paramref name="moduleVersionId"/></param>
        /// <param name="moduleVersionId">Module containing method</param>
        /// <param name="methodToken">Method metadata token</param>
        /// <param name="methodVersion">Method version.</param>
        /// <param name="ilOffset">IL offset of instruction pointer in method</param>
        /// <param name="localSignatureToken">Method local signature token</param>
        /// <returns>Evaluation context</returns>
        internal static EvaluationContext CreateMethodContext(
            CSharpMetadataContext previous,
            ImmutableArray<MetadataBlock> metadataBlocks,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken)
        {
            var offset = NormalizeILOffset(ilOffset);

            // Re-use the previous compilation if possible.
            CSharpCompilation compilation;
            if (previous.Matches(metadataBlocks))
            {
                // Re-use entire context if method scope has not changed.
                var previousContext = previous.EvaluationContext;
                if (previousContext != null &&
                    previousContext.MethodContextReuseConstraints.HasValue &&
                    previousContext.MethodContextReuseConstraints.GetValueOrDefault().AreSatisfied(moduleVersionId, methodToken, methodVersion, offset))
                {
                    return previousContext;
                }
                compilation = previous.Compilation;
            }
            else
            {
                compilation = metadataBlocks.ToCompilation();
            }

            return CreateMethodContext(
                compilation,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                offset,
                localSignatureToken);
        }

        internal static EvaluationContext CreateMethodContext(
            CSharpCompilation compilation,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken)
        {
            return CreateMethodContext(
                compilation,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                NormalizeILOffset(ilOffset),
                localSignatureToken);
        }

        private static EvaluationContext CreateMethodContext(
            CSharpCompilation compilation,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            int ilOffset,
            int localSignatureToken)
        {
            var methodHandle = (MethodDefinitionHandle)MetadataTokens.Handle(methodToken);
            var localSignatureHandle = (localSignatureToken != 0) ? (StandaloneSignatureHandle)MetadataTokens.Handle(localSignatureToken) : default(StandaloneSignatureHandle);

            var currentFrame = compilation.GetMethod(moduleVersionId, methodHandle);
            Debug.Assert((object)currentFrame != null);
            var sourceAssembly = compilation.SourceAssembly;
            var symbolProvider = new CSharpEESymbolProvider(sourceAssembly, (PEModuleSymbol)currentFrame.ContainingModule, currentFrame);

            var metadataDecoder = new MetadataDecoder((PEModuleSymbol)currentFrame.ContainingModule, currentFrame);
            var localInfo = metadataDecoder.GetLocalInfo(localSignatureHandle);

            var typedSymReader = (ISymUnmanagedReader)symReader;
            var inScopeHoistedLocals = InScopeHoistedLocals.Empty;

            var debugInfo = MethodDebugInfo<TypeSymbol, LocalSymbol>.ReadMethodDebugInfo(typedSymReader, symbolProvider, methodToken, methodVersion, ilOffset, isVisualBasicMethod: false);

            var reuseSpan = debugInfo.ReuseSpan;
            var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
            MethodDebugInfo<TypeSymbol, LocalSymbol>.GetLocals(localsBuilder, symbolProvider, debugInfo.LocalVariableNames, localInfo, debugInfo.DynamicLocalMap);
            if (!debugInfo.HoistedLocalScopeRecords.IsDefaultOrEmpty)
            {
                inScopeHoistedLocals = new CSharpInScopeHoistedLocals(debugInfo.GetInScopeHoistedLocalIndices(ilOffset, ref reuseSpan));
            }

            localsBuilder.AddRange(debugInfo.LocalConstants);

            return new EvaluationContext(
                new MethodContextReuseConstraints(moduleVersionId, methodToken, methodVersion, reuseSpan),
                compilation,
                currentFrame,
                localsBuilder.ToImmutableAndFree(),
                inScopeHoistedLocals,
                debugInfo);
        }

        internal CompilationContext CreateCompilationContext(CSharpSyntaxNode syntax)
        {
            return new CompilationContext(
                this.Compilation,
                _currentFrame,
                _locals,
                _inScopeHoistedLocals,
                _methodDebugInfo,
                syntax);
        }

        internal override CompileResult CompileExpression(
            string expr,
            DkmEvaluationFlags compilationFlags,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out ResultProperties resultProperties,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData)
        {
            ReadOnlyCollection<string> formatSpecifiers;
            var syntax = Parse(expr, (compilationFlags & DkmEvaluationFlags.TreatAsExpression) != 0, diagnostics, out formatSpecifiers);
            if (syntax == null)
            {
                resultProperties = default(ResultProperties);
                return null;
            }

            var context = this.CreateCompilationContext(syntax);
            ResultProperties properties;
            var moduleBuilder = context.CompileExpression(TypeName, MethodName, aliases, testData, diagnostics, out properties);
            if (moduleBuilder == null)
            {
                resultProperties = default(ResultProperties);
                return null;
            }

            using (var stream = new MemoryStream())
            {
                Cci.PeWriter.WritePeToStream(
                    new EmitContext((Cci.IModule)moduleBuilder, null, diagnostics),
                    context.MessageProvider,
                    () => stream,
                    getPortablePdbStreamOpt: null,
                    nativePdbWriterOpt: null,
                    pdbPathOpt: null,
                    allowMissingMethodBodies: false,
                    deterministic: false,
                    cancellationToken: default(CancellationToken));

                if (diagnostics.HasAnyErrors())
                {
                    resultProperties = default(ResultProperties);
                    return null;
                }

                resultProperties = properties;
                return new CSharpCompileResult(
                    stream.ToArray(),
                    GetSynthesizedMethod(moduleBuilder),
                    formatSpecifiers: formatSpecifiers);
            }
        }

        private static MethodSymbol GetSynthesizedMethod(CommonPEModuleBuilder moduleBuilder)
        {
            var method = ((EEAssemblyBuilder)moduleBuilder).Methods.Single(m => m.MetadataName == MethodName);
            Debug.Assert(method.ContainingType.MetadataName == TypeName);
            return method;
        }

        private static CSharpSyntaxNode Parse(
            string expr,
            bool treatAsExpression,
            DiagnosticBag diagnostics,
            out ReadOnlyCollection<string> formatSpecifiers)
        {
            if (!treatAsExpression)
            {
                // Try to parse as a statement. If that fails, parse as an expression.
                var statementDiagnostics = DiagnosticBag.GetInstance();
                var statementSyntax = expr.ParseStatement(statementDiagnostics);
                Debug.Assert((statementSyntax == null) || !statementDiagnostics.HasAnyErrors());
                statementDiagnostics.Free();

                if (statementSyntax != null && !statementSyntax.IsKind(SyntaxKind.ExpressionStatement)) // Prefer to parse expression statements as expressions.
                {
                    formatSpecifiers = null;

                    if (statementSyntax.IsKind(SyntaxKind.LocalDeclarationStatement))
                    {
                        return statementSyntax;
                    }

                    diagnostics.Add(ErrorCode.ERR_ExpressionOrDeclarationExpected, Location.None);
                    return null;
                }
            }

            return expr.ParseExpression(diagnostics, allowFormatSpecifiers: true, formatSpecifiers: out formatSpecifiers);
        }

        internal override CompileResult CompileAssignment(
            string target,
            string expr,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out ResultProperties resultProperties,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData)
        {
            var assignment = target.ParseAssignment(expr, diagnostics);
            if (assignment == null)
            {
                resultProperties = default(ResultProperties);
                return null;
            }

            var context = this.CreateCompilationContext(assignment);
            ResultProperties properties;
            var moduleBuilder = context.CompileAssignment(TypeName, MethodName, aliases, testData, diagnostics, out properties);
            if (moduleBuilder == null)
            {
                resultProperties = default(ResultProperties);
                return null;
            }

            using (var stream = new MemoryStream())
            {
                Cci.PeWriter.WritePeToStream(
                    new EmitContext((Cci.IModule)moduleBuilder, null, diagnostics),
                    context.MessageProvider,
                    () => stream,
                    getPortablePdbStreamOpt: null,
                    nativePdbWriterOpt: null,
                    pdbPathOpt: null,
                    allowMissingMethodBodies: false,
                    deterministic: false,
                    cancellationToken: default(CancellationToken));

                if (diagnostics.HasAnyErrors())
                {
                    resultProperties = default(ResultProperties);
                    return null;
                }

                resultProperties = properties;
                return new CSharpCompileResult(
                    stream.ToArray(),
                    GetSynthesizedMethod(moduleBuilder),
                    formatSpecifiers: null);
            }
        }

        private static readonly ReadOnlyCollection<byte> s_emptyBytes = new ReadOnlyCollection<byte>(new byte[0]);

        internal override ReadOnlyCollection<byte> CompileGetLocals(
            ArrayBuilder<LocalAndMethod> locals,
            bool argumentsOnly,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out string typeName,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData)
        {
            var context = this.CreateCompilationContext(null);
            var moduleBuilder = context.CompileGetLocals(TypeName, locals, argumentsOnly, aliases, testData, diagnostics);
            ReadOnlyCollection<byte> assembly = null;

            if ((moduleBuilder != null) && (locals.Count > 0))
            {
                using (var stream = new MemoryStream())
                {
                    Cci.PeWriter.WritePeToStream(
                        new EmitContext((Cci.IModule)moduleBuilder, null, diagnostics),
                        context.MessageProvider,
                        () => stream,
                        getPortablePdbStreamOpt: null,
                        nativePdbWriterOpt: null,
                        pdbPathOpt: null,
                        allowMissingMethodBodies: false,
                        deterministic: false,
                        cancellationToken: default(CancellationToken));

                    if (!diagnostics.HasAnyErrors())
                    {
                        assembly = new ReadOnlyCollection<byte>(stream.ToArray());
                    }
                }
            }

            if (assembly == null)
            {
                locals.Clear();
                assembly = s_emptyBytes;
            }

            typeName = TypeName;
            return assembly;
        }

        internal override bool HasDuplicateTypesOrAssemblies(Diagnostic diagnostic)
        {
            switch ((ErrorCode)diagnostic.Code)
            {
                case ErrorCode.ERR_DuplicateImport:
                case ErrorCode.ERR_DuplicateImportSimple:
                case ErrorCode.ERR_SameFullNameAggAgg:
                case ErrorCode.ERR_AmbigCall:
                    return true;
                default:
                    return false;
            }
        }

        internal override ImmutableArray<AssemblyIdentity> GetMissingAssemblyIdentities(Diagnostic diagnostic, AssemblyIdentity linqLibrary)
        {
            return GetMissingAssemblyIdentitiesHelper((ErrorCode)diagnostic.Code, diagnostic.Arguments, linqLibrary);
        }

        /// <remarks>
        /// Internal for testing.
        /// </remarks>
        internal static ImmutableArray<AssemblyIdentity> GetMissingAssemblyIdentitiesHelper(ErrorCode code, IReadOnlyList<object> arguments, AssemblyIdentity linqLibrary)
        {
            Debug.Assert(linqLibrary != null);

            switch (code)
            {
                case ErrorCode.ERR_NoTypeDef:
                case ErrorCode.ERR_GlobalSingleTypeNameNotFoundFwd:
                case ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd:
                case ErrorCode.ERR_SingleTypeNameNotFoundFwd:
                case ErrorCode.ERR_NameNotInContextPossibleMissingReference: // Probably can't happen.
                    foreach (var argument in arguments)
                    {
                        var identity = (argument as AssemblyIdentity) ?? (argument as AssemblySymbol)?.Identity;
                        if (identity != null && !identity.Equals(MissingCorLibrarySymbol.Instance.Identity))
                        {
                            return ImmutableArray.Create(identity);
                        }
                    }
                    break;
                case ErrorCode.ERR_DottedTypeNameNotFoundInNS:
                    if (arguments.Count == 2)
                    {
                        var namespaceName = arguments[0] as string;
                        var containingNamespace = arguments[1] as NamespaceSymbol;
                        if (namespaceName != null && (object)containingNamespace != null &&
                            containingNamespace.ConstituentNamespaces.Any(n => n.ContainingAssembly.Identity.IsWindowsAssemblyIdentity()))
                        {
                            // This is just a heuristic, but it has the advantage of being portable, particularly 
                            // across different versions of (desktop) windows.
                            var identity = new AssemblyIdentity($"{containingNamespace.ToDisplayString()}.{namespaceName}", contentType: System.Reflection.AssemblyContentType.WindowsRuntime);
                            return ImmutableArray.Create(identity);
                        }
                    }
                    break;
                case ErrorCode.ERR_NoSuchMemberOrExtension: // Commonly, but not always, caused by absence of System.Core.
                case ErrorCode.ERR_DynamicAttributeMissing:
                case ErrorCode.ERR_DynamicRequiredTypesMissing:
                // MSDN says these might come from System.Dynamic.Runtime
                case ErrorCode.ERR_QueryNoProviderStandard:
                case ErrorCode.ERR_ExtensionAttrNotFound: // Probably can't happen.
                    return ImmutableArray.Create(linqLibrary);
                case ErrorCode.ERR_BadAwaitArg_NeedSystem:
                    Debug.Assert(false, "Roslyn no longer produces ERR_BadAwaitArg_NeedSystem");
                    break;
            }

            return default(ImmutableArray<AssemblyIdentity>);
        }
    }
}
