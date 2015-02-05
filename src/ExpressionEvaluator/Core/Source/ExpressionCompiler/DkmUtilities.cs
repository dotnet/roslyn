// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class DkmUtilities
    {
        private static readonly Guid s_symUnmanagedReaderClassId = Guid.Parse("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5");

        internal unsafe delegate IntPtr GetMetadataBytesPtrFunction(AssemblyIdentity assemblyIdentity, out uint uSize);

        private static IEnumerable<DkmClrModuleInstance> GetModulesInAppDomain(this DkmProcess process, DkmClrAppDomain appDomain)
        {
            var appDomainId = appDomain.Id;
            return process.GetRuntimeInstances().
                OfType<DkmClrRuntimeInstance>().
                SelectMany(runtime => runtime.GetModuleInstances()).
                Cast<DkmClrModuleInstance>().
                Where(module => module.AppDomain.Id == appDomainId);
        }

        internal unsafe static ImmutableArray<MetadataBlock> GetMetadataBlocks(this DkmProcess process, DkmClrAppDomain appDomain)
        {
            var builder = ArrayBuilder<MetadataBlock>.GetInstance();
            foreach (DkmClrModuleInstance module in process.GetModulesInAppDomain(appDomain))
            {
                int size;
                IntPtr ptr;
                MetadataReader reader;
                if (module.TryGetMetadataReader(out ptr, out size, out reader))
                {
                    var moduleDef = reader.GetModuleDefinition();
                    var moduleVersionId = reader.GetGuid(moduleDef.Mvid);
                    var generationId = reader.GetGuid(moduleDef.GenerationId);
                    Debug.Assert(moduleVersionId == module.Mvid);
                    builder.Add(new MetadataBlock(moduleVersionId, generationId, ptr, size));
                }
            }
            return builder.ToImmutableAndFree();
        }

        internal static ImmutableArray<MetadataBlock> GetMetadataBlocks(GetMetadataBytesPtrFunction getMetaDataBytesPtrFunction, ImmutableArray<AssemblyIdentity> missingAssemblyIdentities)
        {
            ArrayBuilder<MetadataBlock> builder = null;
            foreach (AssemblyIdentity missingAssemblyIdentity in missingAssemblyIdentities)
            {
                int size;
                IntPtr ptr;
                MetadataReader reader;
                if (TryGetMetadataReader(getMetaDataBytesPtrFunction, missingAssemblyIdentity, out ptr, out size, out reader))
                {
                    var moduleDef = reader.GetModuleDefinition();
                    var moduleVersionId = reader.GetGuid(moduleDef.Mvid);
                    var generationId = reader.GetGuid(moduleDef.GenerationId);

                    if (builder == null)
                    {
                        builder = ArrayBuilder<MetadataBlock>.GetInstance();
                    }

                    builder.Add(new MetadataBlock(moduleVersionId, generationId, ptr, size));
                }
            }
            return builder == null ? ImmutableArray<MetadataBlock>.Empty : builder.ToImmutableAndFree();
        }

        internal static ImmutableArray<AssemblyReaders> MakeAssemblyReaders(this DkmClrInstructionAddress instructionAddress)
        {
            var builder = ArrayBuilder<AssemblyReaders>.GetInstance();
            foreach (DkmClrModuleInstance module in instructionAddress.Process.GetModulesInAppDomain(instructionAddress.ModuleInstance.AppDomain))
            {
                MetadataReader metadataReader;
                if (module.TryGetMetadataReader(out metadataReader))
                {
                    var symReader = module.GetSymReader();
                    if (symReader != null)
                    {
                        builder.Add(new AssemblyReaders(metadataReader, symReader));
                    }
                }
            }
            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Attempt to construct a <see cref="MetadataReader"/> instance for this module.
        /// </summary>
        /// <returns>Returns 'false' for modules with "bad" or missing metadata.</returns>
        private static bool TryGetMetadataReader(this DkmClrModuleInstance module, out MetadataReader reader)
        {
            int size;
            IntPtr ptr;
            return module.TryGetMetadataReader(out ptr, out size, out reader);
        }

        /// <summary>
        /// Attempt to construct a <see cref="MetadataReader"/> instance for this module.
        /// </summary>
        /// <returns>Returns 'false' for modules with "bad" or missing metadata.</returns>
        private unsafe static bool TryGetMetadataReader(this DkmClrModuleInstance module, out IntPtr ptr, out int size, out MetadataReader reader)
        {
            try
            {
                uint uSize;
                ptr = module.GetMetaDataBytesPtr(out uSize);
                size = (int)uSize;
                reader = new MetadataReader((byte*)ptr, size);
                return true;
            }
            catch (Exception e) when (MetadataUtilities.IsBadOrMissingMetadataException(e, module.FullName))
            {
                ptr = IntPtr.Zero;
                size = 0;
                reader = null;
                return false;
            }
        }

        /// <summary>
        /// Attempt to construct a <see cref="MetadataReader"/> instance for this module.
        /// </summary>
        /// <returns>Returns 'false' for modules with "bad" or missing metadata.</returns>
        private unsafe static bool TryGetMetadataReader(GetMetadataBytesPtrFunction getMetaDataBytesPtrFunction, AssemblyIdentity assemblyIdentity, out IntPtr ptr, out int size, out MetadataReader reader)
        {
            var assemblyName = assemblyIdentity.GetDisplayName();
            try
            {
                uint uSize;
                ptr = getMetaDataBytesPtrFunction(assemblyIdentity, out uSize);
                size = (int)uSize;
                reader = new MetadataReader((byte*)ptr, size);
                return true;
            }
            catch (Exception e) when (MetadataUtilities.IsBadOrMissingMetadataException(e, assemblyName))
            {
                ptr = IntPtr.Zero;
                size = 0;
                reader = null;
                return false;
            }
        }

        internal static object GetSymReader(this DkmClrModuleInstance clrModule)
        {
            var module = clrModule.Module; // Null if there are no symbols.
            return (module == null) ? null : module.GetSymbolInterface(s_symUnmanagedReaderClassId);
        }

        internal static DkmCompiledClrInspectionQuery ToQueryResult(
            this CompileResult compResult,
            DkmCompilerId languageId,
            ResultProperties resultProperties,
            DkmClrRuntimeInstance runtimeInstance)
        {
            if (compResult.Assembly == null)
            {
                Debug.Assert(compResult.TypeName == null);
                Debug.Assert(compResult.MethodName == null);
                return null;
            }

            Debug.Assert(compResult.TypeName != null);
            Debug.Assert(compResult.MethodName != null);

            return DkmCompiledClrInspectionQuery.Create(
                runtimeInstance,
                Binary: new ReadOnlyCollection<byte>(compResult.Assembly),
                DataContainer: null,
                LanguageId: languageId,
                TypeName: compResult.TypeName,
                MethodName: compResult.MethodName,
                FormatSpecifiers: compResult.FormatSpecifiers,
                CompilationFlags: resultProperties.Flags,
                ResultCategory: resultProperties.Category,
                Access: resultProperties.AccessType,
                StorageType: resultProperties.StorageType,
                TypeModifierFlags: resultProperties.ModifierFlags);
        }

        internal static ResultProperties GetResultProperties<TSymbol>(this TSymbol symbol, DkmClrCompilationResultFlags flags, bool isConstant)
            where TSymbol : ISymbol
        {
            var haveSymbol = symbol != null;

            var category = haveSymbol
                ? GetResultCategory(symbol.Kind)
                : DkmEvaluationResultCategory.Data;

            var accessType = haveSymbol
                ? GetResultAccessType(symbol.DeclaredAccessibility)
                : DkmEvaluationResultAccessType.None;

            var storageType = haveSymbol && symbol.IsStatic
                ? DkmEvaluationResultStorageType.Static
                : DkmEvaluationResultStorageType.None;

            var modifierFlags = DkmEvaluationResultTypeModifierFlags.None;
            if (isConstant)
            {
                modifierFlags = DkmEvaluationResultTypeModifierFlags.Constant;
            }
            else if (!haveSymbol)
            {
                // No change.
            }
            else if (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride)
            {
                modifierFlags = DkmEvaluationResultTypeModifierFlags.Virtual;
            }
            else if (symbol.Kind == SymbolKind.Field && ((IFieldSymbol)symbol).IsVolatile)
            {
                modifierFlags = DkmEvaluationResultTypeModifierFlags.Volatile;
            }

            // CONSIDER: for completeness, we could check for [MethodImpl(MethodImplOptions.Synchronized)]
            // and set DkmEvaluationResultTypeModifierFlags.Synchronized, but it doesn't seem to have any
            // impact on the UI.  It is exposed through the DTE, but cscompee didn't set the flag either.

            return new ResultProperties(flags, category, accessType, storageType, modifierFlags);
        }

        private static DkmEvaluationResultCategory GetResultCategory(SymbolKind kind)
        {
            switch (kind)
            {
                case SymbolKind.Method:
                    return DkmEvaluationResultCategory.Method;
                case SymbolKind.Property:
                    return DkmEvaluationResultCategory.Property;
                default:
                    return DkmEvaluationResultCategory.Data;
            }
        }

        private static DkmEvaluationResultAccessType GetResultAccessType(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Public:
                    return DkmEvaluationResultAccessType.Public;
                case Accessibility.Protected:
                    return DkmEvaluationResultAccessType.Protected;
                case Accessibility.Private:
                    return DkmEvaluationResultAccessType.Private;
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal: // Dev12 treats this as "internal"
                case Accessibility.ProtectedAndInternal: // Dev12 treats this as "internal"
                    return DkmEvaluationResultAccessType.Internal;
                case Accessibility.NotApplicable:
                    return DkmEvaluationResultAccessType.None;
                default:
                    throw ExceptionUtilities.UnexpectedValue(accessibility);
            }
        }

        internal static bool Includes(this DkmVariableInfoFlags flags, DkmVariableInfoFlags desired)
        {
            return (flags & desired) == desired;
        }
    }
}
