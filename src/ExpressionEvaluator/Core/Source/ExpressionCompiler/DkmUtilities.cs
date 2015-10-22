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

        // Return the set of managed module instances from the AppDomain.
        private static IEnumerable<DkmClrModuleInstance> GetModulesInAppDomain(this DkmClrRuntimeInstance runtime, DkmClrAppDomain appDomain)
        {
            if (appDomain.IsUnloaded)
            {
                return SpecializedCollections.EmptyEnumerable<DkmClrModuleInstance>();
            }

            var appDomainId = appDomain.Id;
            // GetModuleInstances() may include instances of DkmClrNcContainerModuleInstance
            // which are containers of managed module instances (see GetEmbeddedModules())
            // but not managed modules themselves. Since GetModuleInstances() will include the
            // embedded modules, we can simply ignore DkmClrNcContainerModuleInstances.
            return runtime.GetModuleInstances().
                OfType<DkmClrModuleInstance>().
                Where(module =>
                {
                    var moduleAppDomain = module.AppDomain;
                    return !moduleAppDomain.IsUnloaded && (moduleAppDomain.Id == appDomainId);
                });
        }

        internal unsafe static ImmutableArray<MetadataBlock> GetMetadataBlocks(this DkmClrRuntimeInstance runtime, DkmClrAppDomain appDomain)
        {
            var builder = ArrayBuilder<MetadataBlock>.GetInstance();
            IntPtr ptr;
            uint size;
            foreach (DkmClrModuleInstance module in runtime.GetModulesInAppDomain(appDomain))
            {
                MetadataBlock block;
                try
                {
                    ptr = module.GetMetaDataBytesPtr(out size);
                    Debug.Assert(size > 0);
                    block = GetMetadataBlock(ptr, size);
                }
                catch (Exception e) when (MetadataUtilities.IsBadOrMissingMetadataException(e, module.FullName))
                {
                    continue;
                }
                Debug.Assert(block.ModuleVersionId == module.Mvid);
                builder.Add(block);
            }
            // Include "intrinsic method" assembly.
            ptr = runtime.GetIntrinsicAssemblyMetaDataBytesPtr(out size);
            builder.Add(GetMetadataBlock(ptr, size));
            return builder.ToImmutableAndFree();
        }

        internal static ImmutableArray<MetadataBlock> GetMetadataBlocks(GetMetadataBytesPtrFunction getMetaDataBytesPtrFunction, ImmutableArray<AssemblyIdentity> missingAssemblyIdentities)
        {
            ArrayBuilder<MetadataBlock> builder = null;
            foreach (AssemblyIdentity missingAssemblyIdentity in missingAssemblyIdentities)
            {
                MetadataBlock block;
                try
                {
                    uint size;
                    IntPtr ptr;
                    ptr = getMetaDataBytesPtrFunction(missingAssemblyIdentity, out size);
                    Debug.Assert(size > 0);
                    block = GetMetadataBlock(ptr, size);
                }
                catch (Exception e) when (MetadataUtilities.IsBadOrMissingMetadataException(e, missingAssemblyIdentity.GetDisplayName()))
                {
                    continue;
                }
                if (builder == null)
                {
                    builder = ArrayBuilder<MetadataBlock>.GetInstance();
                }
                builder.Add(block);
            }
            return builder == null ? ImmutableArray<MetadataBlock>.Empty : builder.ToImmutableAndFree();
        }

        internal static ImmutableArray<AssemblyReaders> MakeAssemblyReaders(this DkmClrInstructionAddress instructionAddress)
        {
            var builder = ArrayBuilder<AssemblyReaders>.GetInstance();
            foreach (DkmClrModuleInstance module in instructionAddress.RuntimeInstance.GetModulesInAppDomain(instructionAddress.ModuleInstance.AppDomain))
            {
                var symReader = module.GetSymReader();
                if (symReader == null)
                {
                    continue;
                }
                MetadataReader reader;
                unsafe
                {
                    try
                    {
                        uint size;
                        IntPtr ptr;
                        ptr = module.GetMetaDataBytesPtr(out size);
                        Debug.Assert(size > 0);
                        reader = new MetadataReader((byte*)ptr, (int)size);
                    }
                    catch (Exception e) when (MetadataUtilities.IsBadOrMissingMetadataException(e, module.FullName))
                    {
                        continue;
                    }
                }
                builder.Add(new AssemblyReaders(reader, symReader));
            }
            return builder.ToImmutableAndFree();
        }

        private unsafe static MetadataBlock GetMetadataBlock(IntPtr ptr, uint size)
        {
            var reader = new MetadataReader((byte*)ptr, (int)size);
            var moduleDef = reader.GetModuleDefinition();
            var moduleVersionId = reader.GetGuid(moduleDef.Mvid);
            var generationId = reader.GetGuid(moduleDef.GenerationId);
            return new MetadataBlock(moduleVersionId, generationId, ptr, (int)size);
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
            if (compResult == null)
            {
                return null;
            }

            Debug.Assert(compResult.Assembly != null);
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
                TypeModifierFlags: resultProperties.ModifierFlags,
                CustomTypeInfo: compResult.GetCustomTypeInfo().ToDkmClrCustomTypeInfo());
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

        internal static TMetadataContext GetMetadataContext<TMetadataContext>(this DkmClrAppDomain appDomain)
            where TMetadataContext : struct
        {
            var dataItem = appDomain.GetDataItem<MetadataContextItem<TMetadataContext>>();
            return (dataItem == null) ? default(TMetadataContext) : dataItem.MetadataContext;
        }

        internal static void SetMetadataContext<TMetadataContext>(this DkmClrAppDomain appDomain, TMetadataContext context)
            where TMetadataContext : struct
        {
            appDomain.SetDataItem(DkmDataCreationDisposition.CreateAlways, new MetadataContextItem<TMetadataContext>(context));
        }

        internal static void RemoveMetadataContext<TMetadataContext>(this DkmClrAppDomain appDomain)
            where TMetadataContext : struct
        {
            appDomain.RemoveDataItem<MetadataContextItem<TMetadataContext>>();
        }

        private sealed class MetadataContextItem<TMetadataContext> : DkmDataItem
            where TMetadataContext : struct
        {
            internal readonly TMetadataContext MetadataContext;

            internal MetadataContextItem(TMetadataContext metadataContext)
            {
                this.MetadataContext = metadataContext;
            }
        }
    }
}
