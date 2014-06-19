using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    public partial class Compilation
    {
        internal BoundBlock BindMethodBody(MethodSymbol method, DiagnosticBag diagnostics)
        {
            // TODO: remove this
            if (diagnostics == null)
                diagnostics = new DiagnosticBag();
            var sourceMethod = method as SourceMethodSymbol;
            if (sourceMethod != null)
            {
                var binder = new MethodBodyBinder(sourceMethod);
                return binder.BindBlock(sourceMethod.BlockSyntax, diagnostics);
            }

            var synthesizedMethod = method as SynthesizedInstanceConstructor;
            if (synthesizedMethod != null)
            {
                return SynthesizedConstructorBody(synthesizedMethod, diagnostics);
            }

            throw new InvalidOperationException();
        }

        private BoundBlock SynthesizedConstructorBody(SynthesizedInstanceConstructor constructor, DiagnosticBag diagnostics)
        {
            var statements = ArrayBuilder<BoundStatement>.GetInstance();
            var baseType = constructor.ContainingType.BaseType;
            if (baseType != null)
            {
                // possible if class System.Object in source doesn't have an explicit constructor
            }
            MethodSymbol baseConstructor = null;
            foreach (var s in baseType.GetMembers(MethodSymbol.InstanceConstructorName))
            {
                var m = s as MethodSymbol;
                if (m == null || m.Parameters.Count != 0) continue;
                baseConstructor = m;
                break;
            }
            if (baseConstructor != null)
            {
                var voidType = (constructor.ContainingAssembly as SourceAssemblySymbol).Compilation.GetCorLibType(CorLibTypes.TypeId.System_Void);
                var thisRef = new BoundThisReference(null, constructor.ContainingType);
                var baseInvocation = new BoundCall(null, thisRef, baseConstructor, ReadOnlyArray<BoundExpression>.Empty, ReadOnlyArray<string>.Null, false, ReadOnlyArray<int>.Null, voidType);
                statements.Add(new BoundExpressionStatement(null, baseInvocation));
            }
            return new BoundBlock(null, statements.ToReadOnlyAndFree());
        }

        internal BoundStatement AnalyzeMethodBody(MethodSymbol method, BoundBlock block, bool generateDebugInfo, DiagnosticBag diagnostics)
        {
            var diagnostics2 = DiagnosticBag.GetInstance();
            var analyzed1 = FlowAnalysisPass.Rewrite(method, block, diagnostics2);
            if (diagnostics != null)
                diagnostics.Add(diagnostics2);
            diagnostics2.Free();
            return RewritePass.Rewrite(analyzed1, generateDebugInfo);
        }

        internal MethodBody EmitMethodBody(Emit.Module module, MethodSymbol method, BoundStatement block,
            Dictionary<string, Microsoft.Cci.DebugSourceDocument> mapFileNameToDebugDoc, DiagnosticBag diagnostics = null)
        {
            using (ILBuilder builder = new ILBuilder())
            {
                CodeGenerator.Run(method, block, builder, module, mapFileNameToDebugDoc);

                return new MethodBody(builder.Bits,
                                            builder.MaxStack,
                                            method,
                                            builder.LocalSlotManager.LocalsInOrder(),
                                            builder.GetSequencePoints(),
                                            builder.LocalScopes);
            }
        }

        private class MethodCompiler : SymbolVisitor<int, int>
        {
            private Emit.Module moduleBeingBuilt;
            private Compilation compilation;
            private SpecialTypes builtInTypes;
            private DiagnosticBag diagnostics;

            public MethodCompiler(Compilation compilation, Emit.Module moduleBeingBuilt, bool generateDebugInfo)
            {
                this.compilation = compilation;
                this.moduleBeingBuilt = moduleBeingBuilt;
                this.builtInTypes = compilation.SpecialTypes;
                this.diagnostics = new DiagnosticBag();

                if (generateDebugInfo)
                    this.mapFileNameToDebugDoc = new Dictionary<string, Microsoft.Cci.DebugSourceDocument>();
            }

            public override int VisitNamespace(NamespaceSymbol symbol, int a)
            {
                foreach (var s in symbol.GetMembers())
                {
                    s.Accept(this, 0);
                }

                return 0;
            }

            public override int VisitNamedType(NamedTypeSymbol symbol, int a)
            {
                foreach (var s in symbol.GetMembers())
                {
                    if (s.Kind == SymbolKind.NamedType)
                    {
                        s.Accept(this, 0);
                    }
                    else if (s.Kind == SymbolKind.Method)
                    {
                        s.Accept(this, 0);
                    }
                }

                return 0;
            }

            private MethodSymbol BOGUSFindObjCtor(Compilation compilation)
            {
                var t = compilation.GetCorLibType(CorLibTypes.TypeId.System_Object);
                var ms = t.GetMembers(MethodSymbol.InstanceConstructorName);
                return (MethodSymbol)ms[0];
            }

            private MethodBody BOGUSDefineCtor(MethodSymbol methSym)
            {
                Compilation myCompilation = ((SourceAssemblySymbol)methSym.ContainingAssembly).Compilation;
                MethodSymbol objCtor = BOGUSFindObjCtor(myCompilation);

                ILBuilder builder = new ILBuilder();

                builder.PutOpCode(ILOpCode.Nop);
                builder.PutOpCode(ILOpCode.Ldarg_0);
                builder.PutOpCode(ILOpCode.Call, 0);
                builder.PutToken(moduleBeingBuilt.GetFakeSymbolTokenForIL(moduleBeingBuilt.Translate(objCtor)));
                builder.PutOpCode(ILOpCode.Nop);
                builder.PutOpCode(ILOpCode.Ret, -1);
                return new MethodBody(
                    builder.Bits, builder.MaxStack,
                    methSym,
                    SpecializedCollections.EmptyEnumerable<LocalDefinition>(), null, ReadOnlyArray<LocalScope>.Empty);
            }

            Dictionary<string, Microsoft.Cci.DebugSourceDocument> mapFileNameToDebugDoc;

            public override int VisitMethod(MethodSymbol symbol, int a)
            {
                if (symbol.IsAbstract)
                {
                    return 0;
                }

                // Constructors require extra analysis before IL is generated, such as
                // calling the default base constructor. If we see a constructor here
                // we emit what amounts to an empty body (with the right base call) until
                // this analysis is implemented.

                if (symbol.Name == MethodSymbol.InstanceConstructorName && symbol is SourceMethodSymbol)
                {
                    moduleBeingBuilt.SetMethodBody(symbol, this.BOGUSDefineCtor(symbol));
                    return 0;
                }
                else if (symbol.Name == MethodSymbol.StaticConstructorName)
                {
                    Debug.Fail(MethodSymbol.StaticConstructorName + " method body analysis not implemented");
                    return 0;
                }

                var block = compilation.BindMethodBody(symbol, this.diagnostics);
                var analyzedBlock = compilation.AnalyzeMethodBody(symbol, block, mapFileNameToDebugDoc != null, this.diagnostics);
                var emittedBody = compilation.EmitMethodBody(this.moduleBeingBuilt, symbol, analyzedBlock, mapFileNameToDebugDoc);

                moduleBeingBuilt.SetMethodBody(symbol, emittedBody);

                if (symbol.Name == "Main")
                {
                    // TODO: diagnostics if there is more than one of these, check signatures, etc.
                    moduleBeingBuilt.SetEntryPoint((SourceMethodSymbol)symbol);
                }

                return 0;
            }
        }
    }
}
