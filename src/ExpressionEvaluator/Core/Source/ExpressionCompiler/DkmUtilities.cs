// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Clr.NativeCompilation;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class DkmUtilities
    {
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

        internal static ImmutableArray<MetadataBlock> GetMetadataBlocks(
            this DkmClrRuntimeInstance runtime,
            DkmClrAppDomain appDomain,
            ImmutableArray<MetadataBlock> previousMetadataBlocks)
        {
            // Add a dummy data item to the appdomain to add it to the disposal queue when the debugged process is shutting down.
            // This should prevent from attempts to use the Metadata pointer for dead debugged processes.
            if (appDomain.GetDataItem<AppDomainLifetimeDataItem>() == null)
            {
                appDomain.SetDataItem(DkmDataCreationDisposition.CreateNew, new AppDomainLifetimeDataItem());
            }

            var builder = ArrayBuilder<MetadataBlock>.GetInstance();
            IntPtr ptr;
            uint size;
            int index = 0;
            foreach (DkmClrModuleInstance module in runtime.GetModulesInAppDomain(appDomain))
            {
                try
                {
                    ptr = module.GetMetaDataBytesPtr(out size);
                    Debug.Assert(size > 0);
                }
                catch (NotImplementedException e) when (module is DkmClrNcModuleInstance)
                {
                    // DkmClrNcModuleInstance.GetMetaDataBytesPtr not implemented in Dev14.
                    throw new NotImplementedMetadataException(e);
                }
                catch (Exception e) when (DkmExceptionUtilities.IsBadOrMissingMetadataException(e))
                {
                    continue;
                }

                if (!TryGetMetadataBlock(previousMetadataBlocks, index, ptr, size, out var block))
                {
                    // ignore modules with bad metadata headers
                    continue;
                }

                Debug.Assert(block.ModuleVersionId == module.Mvid);
                builder.Add(block);
                index++;
            }

            // Include "intrinsic method" assembly.
            ptr = runtime.GetIntrinsicAssemblyMetaDataBytesPtr(out size);
            if (!TryGetMetadataBlock(previousMetadataBlocks, index, ptr, size, out var intrinsicsBlock))
            {
                throw ExceptionUtilities.Unreachable();
            }

            builder.Add(intrinsicsBlock);
            return builder.ToImmutableAndFree();
        }

        internal static ImmutableArray<MetadataBlock> GetMetadataBlocks(GetMetadataBytesPtrFunction getMetaDataBytesPtrFunction, ImmutableArray<AssemblyIdentity> missingAssemblyIdentities)
        {
            ArrayBuilder<MetadataBlock>? builder = null;
            foreach (var missingAssemblyIdentity in missingAssemblyIdentities)
            {
                uint size;
                IntPtr ptr;
                try
                {
                    ptr = getMetaDataBytesPtrFunction(missingAssemblyIdentity, out size);
                    Debug.Assert(size > 0);
                }
                catch (Exception e) when (DkmExceptionUtilities.IsBadOrMissingMetadataException(e))
                {
                    continue;
                }

                if (!TryGetMetadataBlock(ptr, size, out var block))
                {
                    // ignore modules with bad metadata headers
                    continue;
                }

                builder ??= ArrayBuilder<MetadataBlock>.GetInstance();
                builder.Add(block);
            }

            return builder == null ? ImmutableArray<MetadataBlock>.Empty : builder.ToImmutableAndFree();
        }

        internal static unsafe ImmutableArray<AssemblyReaders> MakeAssemblyReaders(this DkmClrInstructionAddress instructionAddress)
        {
            var builder = ArrayBuilder<AssemblyReaders>.GetInstance();
            foreach (DkmClrModuleInstance module in instructionAddress.RuntimeInstance.GetModulesInAppDomain(instructionAddress.ModuleInstance.AppDomain))
            {
                var symReader = module.GetSymReader();
                if (symReader == null)
                {
                    continue;
                }

                uint size;
                IntPtr ptr;
                try
                {
                    ptr = module.GetMetaDataBytesPtr(out size);
                    Debug.Assert(size > 0);
                }
                catch (Exception e) when (DkmExceptionUtilities.IsBadOrMissingMetadataException(e))
                {
                    continue;
                }

                MetadataReader reader;
                try
                {
                    reader = new MetadataReader((byte*)ptr, (int)size);
                }
                catch (BadImageFormatException)
                {
                    // ignore modules with bad metadata headers
                    continue;
                }

                builder.Add(new AssemblyReaders(reader, symReader));
            }
            return builder.ToImmutableAndFree();
        }

        private static unsafe bool TryGetMetadataBlock(IntPtr ptr, uint size, out MetadataBlock block)
        {
            try
            {
                var reader = new MetadataReader((byte*)ptr, (int)size);
                var moduleDef = reader.GetModuleDefinition();
                var moduleVersionId = reader.GetGuid(moduleDef.Mvid);
                var generationId = reader.GetGuid(moduleDef.GenerationId);
                block = new MetadataBlock(moduleVersionId, generationId, ptr, (int)size);
                return true;
            }
            catch (BadImageFormatException)
            {
                block = default;
                return false;
            }
        }

        private static bool TryGetMetadataBlock(ImmutableArray<MetadataBlock> previousMetadataBlocks, int index, IntPtr ptr, uint size, out MetadataBlock block)
        {
            if (!previousMetadataBlocks.IsDefault && index < previousMetadataBlocks.Length)
            {
                var previousBlock = previousMetadataBlocks[index];
                if (previousBlock.Pointer == ptr && previousBlock.Size == size)
                {
                    block = previousBlock;
                    return true;
                }
            }

            return TryGetMetadataBlock(ptr, size, out block);
        }

        internal static object? GetSymReader(this DkmClrModuleInstance clrModule)
        {
            var module = clrModule.Module; // Null if there are no symbols.
            if (module == null)
            {
                return null;
            }
            // Use DkmClrModuleInstance.GetSymUnmanagedReader()
            // rather than DkmModule.GetSymbolInterface() since the
            // latter does not handle .NET Native modules.
            return clrModule.GetSymUnmanagedReader();
        }

        internal static DkmCompiledClrInspectionQuery? ToQueryResult(
            this CompileResult? compResult,
            DkmCompilerId languageId,
            ResultProperties resultProperties,
            DkmClrRuntimeInstance runtimeInstance)
        {
            if (compResult == null)
            {
                return null;
            }

            Debug.Assert(compResult.Assembly != null);

            ReadOnlyCollection<byte>? customTypeInfo;
            Guid customTypeInfoId = compResult.GetCustomTypeInfo(out customTypeInfo);

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
                CustomTypeInfo: customTypeInfo.ToCustomTypeInfo(customTypeInfoId));
        }

        internal static DkmClrCustomTypeInfo? ToCustomTypeInfo(this ReadOnlyCollection<byte>? payload, Guid payloadTypeId)
        {
            return (payload == null) ? null : DkmClrCustomTypeInfo.Create(payloadTypeId, payload);
        }

        internal static ResultProperties GetResultProperties<TSymbol>(this TSymbol? symbol, DkmClrCompilationResultFlags flags, bool isConstant)
            where TSymbol : class, ISymbolInternal
        {
            var category = (symbol != null) ? GetResultCategory(symbol.Kind)
                : DkmEvaluationResultCategory.Data;

            var accessType = (symbol != null) ? GetResultAccessType(symbol.DeclaredAccessibility)
                : DkmEvaluationResultAccessType.None;

            var storageType = (symbol != null) && symbol.IsStatic
                ? DkmEvaluationResultStorageType.Static
                : DkmEvaluationResultStorageType.None;

            var modifierFlags = DkmEvaluationResultTypeModifierFlags.None;
            if (isConstant)
            {
                modifierFlags = DkmEvaluationResultTypeModifierFlags.Constant;
            }
            else if (symbol is null)
            {
                // No change.
            }
            else if (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride)
            {
                modifierFlags = DkmEvaluationResultTypeModifierFlags.Virtual;
            }
            else if (symbol.Kind == SymbolKind.Field && ((IFieldSymbolInternal)symbol).IsVolatile)
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

        internal static MetadataContext<TAssemblyContext> GetMetadataContext<TAssemblyContext>(this DkmClrAppDomain appDomain)
            where TAssemblyContext : struct
        {
            var dataItem = appDomain.GetDataItem<MetadataContextItem<MetadataContext<TAssemblyContext>>>();
            return (dataItem == null) ? default : dataItem.MetadataContext;
        }

        internal static void SetMetadataContext<TAssemblyContext>(this DkmClrAppDomain appDomain, MetadataContext<TAssemblyContext> context, bool report)
            where TAssemblyContext : struct
        {
            if (report)
            {
                var process = appDomain.Process;
                var message = DkmUserMessage.Create(
                    process.Connection,
                    process,
                    DkmUserMessageOutputKind.UnfilteredOutputWindowMessage,
                    $"EE: AppDomain {appDomain.Id}, blocks {context.MetadataBlocks.Length}, contexts {context.AssemblyContexts.Count}" + Environment.NewLine,
                    MessageBoxFlags.MB_OK,
                    0);
                message.Post();
            }
            appDomain.SetDataItem(DkmDataCreationDisposition.CreateAlways, new MetadataContextItem<MetadataContext<TAssemblyContext>>(context));
        }

        internal static void RemoveMetadataContext<TAssemblyContext>(this DkmClrAppDomain appDomain)
            where TAssemblyContext : struct
        {
            appDomain.RemoveDataItem<MetadataContextItem<TAssemblyContext>>();
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

        private sealed class AppDomainLifetimeDataItem : DkmDataItem { }
    }
}
