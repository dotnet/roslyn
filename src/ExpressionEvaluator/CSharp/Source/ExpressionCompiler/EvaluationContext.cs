// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Utilities;

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
        private readonly MethodSymbol? _currentSourceMethod;
        private readonly ImmutableArray<LocalSymbol> _locals;
        private readonly ImmutableSortedSet<int> _inScopeHoistedLocalSlots;
        private readonly MethodDebugInfo<TypeSymbol, LocalSymbol> _methodDebugInfo;

        private EvaluationContext(
            MethodContextReuseConstraints? methodContextReuseConstraints,
            CSharpCompilation compilation,
            MethodSymbol currentFrame,
            MethodSymbol? currentSourceMethod,
            ImmutableArray<LocalSymbol> locals,
            ImmutableSortedSet<int> inScopeHoistedLocalSlots,
            MethodDebugInfo<TypeSymbol, LocalSymbol> methodDebugInfo)
        {
            RoslynDebug.AssertNotNull(inScopeHoistedLocalSlots);
            RoslynDebug.AssertNotNull(methodDebugInfo);

            MethodContextReuseConstraints = methodContextReuseConstraints;
            Compilation = compilation;
            _currentFrame = currentFrame;
            _currentSourceMethod = currentSourceMethod;
            _locals = locals;
            _inScopeHoistedLocalSlots = inScopeHoistedLocalSlots;
            _methodDebugInfo = methodDebugInfo;
        }

        /// <summary>
        /// Create a context for evaluating expressions at a type scope.
        /// </summary>
        /// <param name="compilation">Compilation.</param>
        /// <param name="moduleId">Module containing type</param>
        /// <param name="typeToken">Type metadata token</param>
        /// <returns>Evaluation context</returns>
        /// <remarks>
        /// No locals since locals are associated with methods, not types.
        /// </remarks>
        /// <exception cref="BadMetadataModuleException">Module wasn't included in the compilation due to bad metadata.</exception>
        internal static EvaluationContext CreateTypeContext(
            CSharpCompilation compilation,
            ModuleId moduleId,
            int typeToken)
        {
            Debug.Assert(MetadataTokens.Handle(typeToken).Kind == HandleKind.TypeDefinition);

            var currentType = compilation.GetType(moduleId, typeToken);
            RoslynDebug.Assert(currentType is object);
            var currentFrame = new SynthesizedContextMethodSymbol(currentType);
            return new EvaluationContext(
                null,
                compilation,
                currentFrame,
                currentSourceMethod: null,
                locals: default,
                inScopeHoistedLocalSlots: ImmutableSortedSet<int>.Empty,
                methodDebugInfo: MethodDebugInfo<TypeSymbol, LocalSymbol>.None);
        }

        // Used by VS debugger (/src/debugger/ProductionDebug/CodeAnalysis/CodeAnalysis/ExpressionEvaluator.cs)
        internal static EvaluationContext CreateMethodContext(
            ImmutableArray<MetadataBlock> metadataBlocks,
            object symReader,
            Guid moduleId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken)
            => CreateMethodContext(metadataBlocks, symReader, new ModuleId(moduleId, "<unknown>"), methodToken, methodVersion, ilOffset, localSignatureToken);

        /// <summary>
        /// Create a context for evaluating expressions within a method scope.
        /// </summary>
        /// <param name="metadataBlocks">Module metadata</param>
        /// <param name="symReader"><see cref="ISymUnmanagedReader"/> for PDB associated with <paramref name="moduleId"/></param>
        /// <param name="moduleId">Module containing method</param>
        /// <param name="methodVersion">Method version.</param>
        /// <param name="ilOffset">IL offset of instruction pointer in method</param>
        /// <param name="localSignatureToken">Method local signature token</param>
        /// <returns>Evaluation context</returns>
        internal static EvaluationContext CreateMethodContext(
            ImmutableArray<MetadataBlock> metadataBlocks,
            object symReader,
            ModuleId moduleId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken)
        {
            var offset = NormalizeILOffset(ilOffset);

            var compilation = metadataBlocks.ToCompilation(moduleId: default, MakeAssemblyReferencesKind.AllAssemblies);

            return CreateMethodContext(
                compilation,
                symReader,
                moduleId,
                methodToken,
                methodVersion,
                offset,
                localSignatureToken);
        }

        /// <summary>
        /// Create a context for evaluating expressions within a method scope.
        /// </summary>
        /// <param name="compilation">Compilation.</param>
        /// <param name="symReader"><see cref="ISymUnmanagedReader"/> for PDB associated with <paramref name="moduleId"/></param>
        /// <param name="moduleId">Module containing method</param>
        /// <param name="methodToken">Method metadata token</param>
        /// <param name="methodVersion">Method version.</param>
        /// <param name="ilOffset">IL offset of instruction pointer in method</param>
        /// <param name="localSignatureToken">Method local signature token</param>
        /// <returns>Evaluation context</returns>
        internal static EvaluationContext CreateMethodContext(
            CSharpCompilation compilation,
            object? symReader,
            ModuleId moduleId,
            int methodToken,
            int methodVersion,
            int ilOffset,
            int localSignatureToken)
        {
            var methodHandle = (MethodDefinitionHandle)MetadataTokens.Handle(methodToken);
            var currentSourceMethod = compilation.GetSourceMethod(moduleId, methodHandle);
            var localSignatureHandle = (localSignatureToken != 0) ? (StandaloneSignatureHandle)MetadataTokens.Handle(localSignatureToken) : default;

            var currentFrame = compilation.GetMethod(moduleId, methodHandle);
            RoslynDebug.AssertNotNull(currentFrame);
            var symbolProvider = new CSharpEESymbolProvider(compilation.SourceAssembly, (PEModuleSymbol)currentFrame.ContainingModule, currentFrame);

            var metadataDecoder = new MetadataDecoder((PEModuleSymbol)currentFrame.ContainingModule, currentFrame);
            var localInfo = metadataDecoder.GetLocalInfo(localSignatureHandle);

            var typedSymReader = (ISymUnmanagedReader3?)symReader;

            var debugInfo = MethodDebugInfo<TypeSymbol, LocalSymbol>.ReadMethodDebugInfo(typedSymReader, symbolProvider, methodToken, methodVersion, ilOffset, isVisualBasicMethod: false);

            var reuseSpan = debugInfo.ReuseSpan;
            var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
            MethodDebugInfo<TypeSymbol, LocalSymbol>.GetLocals(
                localsBuilder,
                symbolProvider,
                debugInfo.LocalVariableNames,
                localInfo,
                debugInfo.DynamicLocalMap,
                debugInfo.TupleLocalMap);

            var inScopeHoistedLocals = debugInfo.GetInScopeHoistedLocalIndices(ilOffset, ref reuseSpan);

            localsBuilder.AddRange(debugInfo.LocalConstants);

            return new EvaluationContext(
                new MethodContextReuseConstraints(moduleId, methodToken, methodVersion, reuseSpan),
                compilation,
                currentFrame,
                currentSourceMethod,
                localsBuilder.ToImmutableAndFree(),
                inScopeHoistedLocals,
                debugInfo);
        }

        internal CompilationContext CreateCompilationContext()
        {
            return new CompilationContext(
                Compilation,
                _currentFrame,
                _currentSourceMethod,
                _locals,
                _inScopeHoistedLocalSlots,
                _methodDebugInfo);
        }

        /// <summary>
        /// Compile a collection of expressions at the same location. If all expressions
        /// compile successfully, a single assembly is returned along with the method
        /// tokens for the expression evaluation methods. If there are errors compiling
        /// any expression, null is returned along with the collection of error messages
        /// for all expressions.
        /// </summary>
        /// <remarks>
        /// Errors are returned as a single collection rather than grouped by expression
        /// since some errors (such as those detected during emit) are not easily
        /// attributed to a particular expression.
        /// </remarks>
        internal byte[]? CompileExpressions(
            ImmutableArray<string> expressions,
            out ImmutableArray<int> methodTokens,
            out ImmutableArray<string> errorMessages)
        {
            var context = CreateCompilationContext();
            bool isInFieldKeywordContext = context.IsInFieldKeywordContext();
            var diagnostics = DiagnosticBag.GetInstance();
            var syntaxNodes = expressions.SelectAsArray(expr => Parse(expr, isInFieldKeywordContext, treatAsExpression: true, diagnostics, out var formatSpecifiers));
            byte[]? assembly = null;
            if (!diagnostics.HasAnyErrors())
            {
                RoslynDebug.Assert(syntaxNodes.All(s => s != null));

                if (context.TryCompileExpressions(syntaxNodes!, TypeName, MethodName, diagnostics, out var moduleBuilder))
                {
                    using var stream = new MemoryStream();

                    Cci.PeWriter.WritePeToStream(
                        new EmitContext(moduleBuilder, null, diagnostics, metadataOnly: false, includePrivateMembers: true),
                        context.MessageProvider,
                        () => stream,
                        getPortablePdbStreamOpt: null,
                        nativePdbWriterOpt: null,
                        pdbPathOpt: null,
                        metadataOnly: false,
                        isDeterministic: false,
                        emitTestCoverageData: false,
                        privateKeyOpt: null,
                        CancellationToken.None);

                    if (!diagnostics.HasAnyErrors())
                    {
                        assembly = stream.ToArray();
                    }
                }
            }

            if (assembly == null)
            {
                methodTokens = ImmutableArray<int>.Empty;
                errorMessages = ImmutableArray.CreateRange(
                    diagnostics.AsEnumerable().
                        Where(d => d.Severity == DiagnosticSeverity.Error).
                        Select(d => GetErrorMessage(d, CSharpDiagnosticFormatter.Instance, preferredUICulture: null)));
            }
            else
            {
                methodTokens = MetadataUtilities.GetSynthesizedMethods(assembly, MethodName);
                Debug.Assert(methodTokens.Length == expressions.Length);
                errorMessages = ImmutableArray<string>.Empty;
            }
            diagnostics.Free();
            return assembly;
        }

        internal override CompileResult? CompileExpression(
            string expr,
            DkmEvaluationFlags compilationFlags,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out ResultProperties resultProperties,
            CompilationTestData? testData)
        {
            var context = CreateCompilationContext();

            var syntax = Parse(expr, context.IsInFieldKeywordContext(), (compilationFlags & DkmEvaluationFlags.TreatAsExpression) != 0, diagnostics, out var formatSpecifiers);
            if (syntax == null)
            {
                resultProperties = default;
                return null;
            }

            if (!context.TryCompileExpression(syntax, TypeName, MethodName, aliases, testData, diagnostics, out var moduleBuilder, out var synthesizedMethod))
            {
                resultProperties = default;
                return null;
            }

            using var stream = new MemoryStream();

            Cci.PeWriter.WritePeToStream(
                new EmitContext(moduleBuilder, null, diagnostics, metadataOnly: false, includePrivateMembers: true),
                context.MessageProvider,
                () => stream,
                getPortablePdbStreamOpt: null,
                nativePdbWriterOpt: null,
                pdbPathOpt: null,
                metadataOnly: false,
                isDeterministic: false,
                emitTestCoverageData: false,
                privateKeyOpt: null,
                CancellationToken.None);

            if (diagnostics.HasAnyErrors())
            {
                resultProperties = default;
                return null;
            }

            Debug.Assert(synthesizedMethod.ContainingType.MetadataName == TypeName);
            Debug.Assert(synthesizedMethod.MetadataName == MethodName);

            resultProperties = synthesizedMethod.ResultProperties;
            return new CSharpCompileResult(
                stream.ToArray(),
                synthesizedMethod,
                formatSpecifiers: formatSpecifiers);
        }

        private CSharpSyntaxNode? Parse(
            string expr,
            bool isInFieldKeywordContext,
            bool treatAsExpression,
            DiagnosticBag diagnostics,
            out ReadOnlyCollection<string>? formatSpecifiers)
        {
            if (!treatAsExpression)
            {
                // Try to parse as a statement. If that fails, parse as an expression.
                var statementDiagnostics = DiagnosticBag.GetInstance();
                var statementSyntax = expr.ParseStatement(isInFieldKeywordContext, statementDiagnostics);
                Debug.Assert((statementSyntax == null) || !statementDiagnostics.HasAnyErrors());
                statementDiagnostics.Free();
                var isExpressionStatement = statementSyntax.IsKind(SyntaxKind.ExpressionStatement);
                if (statementSyntax != null && !isExpressionStatement)
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

            return expr.ParseExpression(isInFieldKeywordContext, diagnostics, allowFormatSpecifiers: true, out formatSpecifiers);
        }

        internal override CompileResult? CompileAssignment(
            string target,
            string expr,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out ResultProperties resultProperties,
            CompilationTestData? testData)
        {
            var context = CreateCompilationContext();
            var assignment = target.ParseAssignment(expr, context.IsInFieldKeywordContext(), diagnostics);
            if (assignment == null)
            {
                resultProperties = default;
                return null;
            }

            if (!context.TryCompileAssignment(assignment, TypeName, MethodName, aliases, testData, diagnostics, out var moduleBuilder, out var synthesizedMethod))
            {
                resultProperties = default;
                return null;
            }

            using var stream = new MemoryStream();

            Cci.PeWriter.WritePeToStream(
                new EmitContext(moduleBuilder, null, diagnostics, metadataOnly: false, includePrivateMembers: true),
                context.MessageProvider,
                () => stream,
                getPortablePdbStreamOpt: null,
                nativePdbWriterOpt: null,
                pdbPathOpt: null,
                metadataOnly: false,
                isDeterministic: false,
                emitTestCoverageData: false,
                privateKeyOpt: null,
                CancellationToken.None);

            if (diagnostics.HasAnyErrors())
            {
                resultProperties = default;
                return null;
            }

            Debug.Assert(synthesizedMethod.ContainingType.MetadataName == TypeName);
            Debug.Assert(synthesizedMethod.MetadataName == MethodName);

            resultProperties = synthesizedMethod.ResultProperties;
            return new CSharpCompileResult(
                stream.ToArray(),
                synthesizedMethod,
                formatSpecifiers: null);
        }

        private static readonly ReadOnlyCollection<byte> s_emptyBytes =
            new ReadOnlyCollection<byte>(Array.Empty<byte>());

        internal override ReadOnlyCollection<byte> CompileGetLocals(
            ArrayBuilder<LocalAndMethod> locals,
            bool argumentsOnly,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out string typeName,
            CompilationTestData? testData)
        {
            var context = CreateCompilationContext();
            var moduleBuilder = context.CompileGetLocals(TypeName, locals, argumentsOnly, aliases, testData, diagnostics);
            ReadOnlyCollection<byte>? assembly = null;

            if (moduleBuilder != null && locals.Count > 0)
            {
                using var stream = new MemoryStream();

                Cci.PeWriter.WritePeToStream(
                    new EmitContext(moduleBuilder, null, diagnostics, metadataOnly: false, includePrivateMembers: true),
                    context.MessageProvider,
                    () => stream,
                    getPortablePdbStreamOpt: null,
                    nativePdbWriterOpt: null,
                    pdbPathOpt: null,
                    metadataOnly: false,
                    isDeterministic: false,
                    emitTestCoverageData: false,
                    privateKeyOpt: null,
                    CancellationToken.None);

                if (!diagnostics.HasAnyErrors())
                {
                    assembly = new ReadOnlyCollection<byte>(stream.ToArray());
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
        internal static ImmutableArray<AssemblyIdentity> GetMissingAssemblyIdentitiesHelper(ErrorCode code, IReadOnlyList<object?> arguments, AssemblyIdentity linqLibrary)
        {
            RoslynDebug.AssertNotNull(linqLibrary);

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
                    if (arguments.Count == 2 &&
                        arguments[0] is string namespaceName &&
                        arguments[1] is NamespaceSymbol containingNamespace &&
                        containingNamespace.ConstituentNamespaces.Any(static n => n.ContainingAssembly.Identity.IsWindowsAssemblyIdentity()))
                    {
                        // This is just a heuristic, but it has the advantage of being portable, particularly 
                        // across different versions of (desktop) windows.
                        var identity = new AssemblyIdentity($"{containingNamespace.ToDisplayString()}.{namespaceName}", contentType: System.Reflection.AssemblyContentType.WindowsRuntime);
                        return ImmutableArray.Create(identity);
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

            return default;
        }
    }
}
