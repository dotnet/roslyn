// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ICompilationExtensions
    {
        public static ImmutableArray<Compilation> GetReferencedCompilations(this Compilation compilation)
        {
            var builder = ArrayBuilder<Compilation>.GetInstance();

            foreach (var reference in compilation.References.OfType<CompilationReference>())
            {
                builder.Add(reference.Compilation);
            }

            var previous = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;
            while (previous != null)
            {
                builder.Add(previous);
                previous = previous.ScriptCompilationInfo?.PreviousScriptCompilation;
            }

            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<IAssemblySymbol> GetReferencedAssemblySymbols(this Compilation compilation, bool excludePreviousSubmissions = false)
        {
            // The first module of every assembly is its source module and the source
            // module always has the list of all referenced assemblies.
            var referencedAssemblySymbols = compilation.Assembly.Modules.First().ReferencedAssemblySymbols;

            if (excludePreviousSubmissions)
            {
                return referencedAssemblySymbols;
            }

            var builder = ArrayBuilder<IAssemblySymbol>.GetInstance();
            builder.AddRange(referencedAssemblySymbols);

            var previous = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;
            while (previous != null)
            {
                builder.Add(previous.Assembly);
                previous = previous.ScriptCompilationInfo?.PreviousScriptCompilation;
            }

            return builder.ToImmutableAndFree();
        }

        public static INamedTypeSymbol? AttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Attribute).FullName!);

        public static INamedTypeSymbol? ExceptionType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Exception).FullName!);

        public static INamedTypeSymbol? DebuggerDisplayAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(System.Diagnostics.DebuggerDisplayAttribute).FullName!);

        public static INamedTypeSymbol? StructLayoutAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.StructLayoutAttribute).FullName!);

        public static INamedTypeSymbol? DesignerCategoryAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.ComponentModel.DesignerCategoryAttribute");

        public static INamedTypeSymbol? DesignerGeneratedAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute");

        public static INamedTypeSymbol? HideModuleNameAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName("Microsoft.VisualBasic.HideModuleNameAttribute");

        public static INamedTypeSymbol? ThreadStaticAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(ThreadStaticAttribute).FullName!);

        public static INamedTypeSymbol? EventArgsType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(EventArgs).FullName!);

        public static INamedTypeSymbol? NotImplementedExceptionType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(NotImplementedException).FullName!);

        public static INamedTypeSymbol? EqualityComparerOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(EqualityComparer<>).FullName!);

        public static INamedTypeSymbol? ActionType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Action).FullName!);

        public static INamedTypeSymbol? ExpressionOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");

        public static INamedTypeSymbol? EditorBrowsableAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(EditorBrowsableAttribute).FullName!);

        public static INamedTypeSymbol? EditorBrowsableStateType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(EditorBrowsableState).FullName!);

        public static INamedTypeSymbol? TaskType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Task).FullName!);

        public static INamedTypeSymbol? TaskOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Task<>).FullName!);

        public static INamedTypeSymbol? ValueTaskType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");

        public static INamedTypeSymbol? ValueTaskOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        public static INamedTypeSymbol? IEnumerableOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName!);

        public static INamedTypeSymbol? IEnumeratorOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(IEnumerator<>).FullName!);

        public static INamedTypeSymbol? IAsyncEnumerableOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");

        public static INamedTypeSymbol? IAsyncEnumeratorOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1");

        public static INamedTypeSymbol? SerializableAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(SerializableAttribute).FullName!);

        public static INamedTypeSymbol? CoClassType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(CoClassAttribute).FullName!);

        public static INamedTypeSymbol? ComAliasNameAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(ComAliasNameAttribute).FullName!);

        public static INamedTypeSymbol? SuppressMessageAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(SuppressMessageAttribute).FullName!);

        public static INamedTypeSymbol? TupleElementNamesAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(TupleElementNamesAttribute).FullName!);

        public static INamedTypeSymbol? NativeIntegerAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.NativeIntegerAttribute");

        public static INamedTypeSymbol? DynamicAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(DynamicAttribute).FullName!);

        public static INamedTypeSymbol? LazyOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(Lazy<>).FullName!);

        public static INamedTypeSymbol? ISerializableType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(ISerializable).FullName!);

        public static INamedTypeSymbol? SerializationInfoType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(SerializationInfo).FullName!);

        public static INamedTypeSymbol? StreamingContextType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(StreamingContext).FullName!);

        public static INamedTypeSymbol? OnDeserializingAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(OnDeserializingAttribute).FullName!);

        public static INamedTypeSymbol? OnDeserializedAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(OnDeserializedAttribute).FullName!);

        public static INamedTypeSymbol? OnSerializingAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(OnSerializingAttribute).FullName!);

        public static INamedTypeSymbol? OnSerializedAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(OnSerializedAttribute).FullName!);

        public static INamedTypeSymbol? ComRegisterFunctionAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(ComRegisterFunctionAttribute).FullName!);

        public static INamedTypeSymbol? ComUnregisterFunctionAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(ComUnregisterFunctionAttribute).FullName!);

        public static INamedTypeSymbol? ConditionalAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(ConditionalAttribute).FullName!);

        public static INamedTypeSymbol? ObsoleteAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(ObsoleteAttribute).FullName!);

        public static INamedTypeSymbol? SystemCompositionImportingConstructorAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Composition.ImportingConstructorAttribute");

        public static INamedTypeSymbol? SystemComponentModelCompositionImportingConstructorAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.ComponentModel.Composition.ImportingConstructorAttribute");

        public static INamedTypeSymbol? SystemIDisposableType(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(IDisposable).FullName!);

        public static INamedTypeSymbol? NotNullAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(NotNullAttribute).FullName!);

        public static INamedTypeSymbol? MaybeNullAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(MaybeNullAttribute).FullName!);

        public static INamedTypeSymbol? MaybeNullWhenAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(MaybeNullWhenAttribute).FullName!);

        public static INamedTypeSymbol? AllowNullAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(AllowNullAttribute).FullName!);

        public static INamedTypeSymbol? DisallowNullAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(DisallowNullAttribute).FullName!);

        public static INamedTypeSymbol? DataMemberAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(DataMemberAttribute).FullName!);

        public static INamedTypeSymbol? DataContractAttribute(this Compilation compilation)
            => compilation.GetTypeByMetadataName(typeof(DataContractAttribute).FullName!);
    }
}
