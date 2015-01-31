// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.Test
{
    public class ScriptingTestHelpers
    {
        private delegate bool ReflectionEmitter(bool refEmitSupported, bool fallback);

        // The purpose of this method is simply to check that the signature of the 'ReflectionEmit' method 
        // matches the 'Emitter' delegate type that it will be dynamically assigned to...
        // That is, we will catch mismatches due to changes in the signatures at compile time instead
        // of getting an opaque ArgumentException from the call to CreateDelegate.
        private static void TestEmitSignature()
        {
            CommonTestBase.Emitter emitter = ReflectionEmit;
        }

        static internal CommonTestBase.CompilationVerifier ReflectionEmit(
            CommonTestBase test,
            Compilation compilation,
            IEnumerable<ModuleData> dependencies,
            TestEmitters emitOptions,
            IEnumerable<ResourceDescription> manifestResources,
            SignatureDescription[] expectedSignatures,
            string expectedOutput,
            Action<PEAssembly, TestEmitters> assemblyValidator,
            Action<IModuleSymbol, TestEmitters> symbolValidator,
            bool collectEmittedAssembly,
            bool verify)
        {
            CommonTestBase.CompilationVerifier verifier = null;

            ReflectionEmitter emit = (refEmitSupported, fallback) =>
            {
                bool requestPEImage = verify || assemblyValidator != null || symbolValidator != null;
                ImmutableArray<byte> peImage;

                // TODO(tomat): we should ref.emit in the AppDomain where we load all the dependencies, otherwise ref.emit fails to emit compilation-compilation references.
                bool emitSuccess = ReflectionEmitInternal(compilation,
                                                          expectedOutput,
                                                          requestPEImage,
                                                          out peImage,
                                                          refEmitSupported,
                                                          fallback,
                                                          collectEmittedAssembly,
                                                          peVerify: verify,
                                                          tempRoot: verifier.Temp);

                Assert.Equal(!peImage.IsDefault, requestPEImage && emitSuccess);

                if (!peImage.IsDefault)
                {
                    // TODO(tomat):
                    // We assume that we CompileAndVerify(verify: true) is called if we are going to use VerifyIL.
                    // Ideally expected IL would be a parameter of CompileAndVerify so we know that we need to get the pe image.
                    verifier.EmittedAssemblyData = peImage;
                }

                return emitSuccess;
            };

            if (emitOptions != TestEmitters.RefEmitBug)
            {
                verifier = new CommonTestBase.CompilationVerifier(test, compilation, dependencies);

                if (emitOptions == TestEmitters.RefEmitUnsupported)
                {
                    // Test that Ref.Emit fails.
                    // The code contains features not supported by Ref.Emit, don't fall back to CCI to test the failure.
                    Assert.False(
                        emit(refEmitSupported: false, fallback: false));

                    // test that Emit falls back to CCI:
                    Assert.True(
                        emit(refEmitSupported: false, fallback: true));
                }
                else
                {
                    // The code should only contain features supported by Ref.Emit.
                    Assert.True(
                        emit(refEmitSupported: true, fallback: false));
                }

                // We're dual-purposing EmitOptions here.  In this context, it
                // tells the validator the version of Emit that is calling it.
                CommonTestBase.RunValidators(verifier, TestEmitters.RefEmit, assemblyValidator, symbolValidator);
            }

            return (emitOptions == TestEmitters.RefEmit) ? verifier : null;
        }

        private static bool ReflectionEmitInternal(
            Compilation compilation,
            string expectedOutput,
            bool peImageRequested,
            out ImmutableArray<byte> peImage,
            bool refEmitSupported,
            bool fallback,
            bool collectEmittedAssembly,
            bool peVerify,
            TempRoot tempRoot)
        {
            peImage = default(ImmutableArray<byte>);

            var success = false;
            var compilationDependencies = new List<ModuleData>();
            var diagnostics = new DiagnosticBag();
            HostedRuntimeEnvironment.EmitReferences(compilation, compilationDependencies, diagnostics);

            // allow warnings
            if (diagnostics.HasAnyErrors())
            {
                // this will throw if there are errors
                diagnostics.Verify();
            }

            bool doExecute = expectedOutput != null;

            string fileName;
            string fileNameExt;
            string fileDir;

            TempFile outputFile;
            AssemblyBuilderAccess access;
            if (peImageRequested)
            {
                access = doExecute ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Save;

                // Until HostedExecutionEnvironment supports ref emit, we need to generate a unique temp file.
                // Otherwise, the file will be held open the next time we Emit this same compilation (done in CompileAndVerify).
                outputFile = tempRoot.CreateFile("RefEmit_", ".dll");

                fileNameExt = Path.GetFileName(outputFile.Path);
                fileName = Path.GetFileNameWithoutExtension(fileNameExt);
                fileDir = Path.GetDirectoryName(outputFile.Path);
            }
            else
            {
                outputFile = null;

                fileName = CommonTestBase.GetUniqueName();
                fileNameExt = fileName + ".dll";
                fileDir = null;

                access = collectEmittedAssembly ? AssemblyBuilderAccess.RunAndCollect : AssemblyBuilderAccess.Run;
            }

            var assemblyName = new AssemblyIdentity(fileName).ToAssemblyName();
            AssemblyBuilder abuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, access, fileDir);
            ModuleBuilder mbuilder = abuilder.DefineDynamicModule("Module", fileNameExt, emitSymbolInfo: false);

            using (var assemblyManager = new RuntimeAssemblyManager())
            {
                assemblyManager.AddModuleData(compilationDependencies);

                DiagnosticBag emitDiagnostics = DiagnosticBag.GetInstance();
                MethodInfo entryPoint;

                byte[] compiledAssemblyImage;

                success = compilation.Emit(
                    moduleBuilder: mbuilder,
                    assemblyLoader: new AssemblyLoader(assemblyManager),
                    assemblySymbolMapper: null,
                    recoverOnError: !refEmitSupported && fallback,
                    diagnostics: emitDiagnostics,
                    cancellationToken: default(CancellationToken),
                    entryPoint: out entryPoint,
                    compiledAssemblyImage: out compiledAssemblyImage
                );

                emitDiagnostics.Free();

                if (success && peImageRequested)
                {
                    if (fallback)
                    {
                        peImage = compiledAssemblyImage.AsImmutableOrNull();
                    }
                    else
                    {
                        abuilder.Save(fileNameExt);
                        peImage = CommonTestBase.ReadFromFile(outputFile.Path);
                    }
                }

                if (refEmitSupported)
                {
                    Assert.True(success, "Expected Ref.Emit success");
                    Assert.Null(compiledAssemblyImage);
                }
                else if (fallback)
                {
                    Assert.True(success, "Expected fallback to CCI");
                    Assert.NotNull(compiledAssemblyImage);
                }
                else
                {
                    Assert.False(success, "Expected emit failure but it succeeded");
                    Assert.Null(compiledAssemblyImage);
                }

                if (success)
                {
                    if (peVerify)
                    {
                        Assert.False(peImage.IsDefault);

                        // Saving AssemblyBuilder to disk changes its manifest module MVID.
                        Guid mvid;
                        using (var metadata = ModuleMetadata.CreateFromImage(peImage))
                        {
                            mvid = metadata.GetModuleVersionId();
                        }

                        assemblyManager.AddMainModuleMvid(mvid);
#if !(ARM)
                        Assert.Equal(String.Empty, CLRHelpers.PeVerify(peImage).Concat());
#endif
                    }

                    if (doExecute)
                    {
                        Assert.NotNull(entryPoint);
                        assemblyManager.AddMainModuleMvid(entryPoint.Module.ModuleVersionId);

                        Action main = GetEntryPointAction(entryPoint);
                        ConsoleOutput.AssertEqual(main, expectedOutput, "");
                    }
                }

                return success;
            }
        }

        private static Action GetEntryPointAction(MethodInfo entryPoint)
        {
            if (entryPoint.GetParameters().Length == 0)
            {
                if (entryPoint.ReturnType == typeof(void))
                {
                    return (Action)Delegate.CreateDelegate(typeof(Action), entryPoint);
                }
                else
                {
                    return () => ((Func<int>)Delegate.CreateDelegate(typeof(Func<int>), entryPoint))();
                }
            }
            else
            {
                if (entryPoint.ReturnType == typeof(void))
                {
                    return () => ((Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), entryPoint))(new string[0]);
                }
                else
                {
                    return () => ((Func<string[], int>)Delegate.CreateDelegate(typeof(Func<string[], int>), entryPoint))(new string[0]);
                }
            }
        }

        #region Interactive Tests

        internal static void AssertCompilationError(ScriptEngine engine, string code, params DiagnosticDescription[] expectedDiagnostics)
        {
            AssertCompilationError(engine.CreateSession(), code, expectedDiagnostics);
        }

        internal static void AssertCompilationError(Session session, string code, params DiagnosticDescription[] expectedDiagnostics)
        {
            bool noException = false;
            try
            {
                session.Execute(code);
                noException = true;
            }
            catch (CompilationErrorException e)
            {
                e.Diagnostics.Verify(expectedDiagnostics);
                e.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && e.Message == d.ToString());
            }

            Assert.False(noException);
        }

        #endregion
    }
}
