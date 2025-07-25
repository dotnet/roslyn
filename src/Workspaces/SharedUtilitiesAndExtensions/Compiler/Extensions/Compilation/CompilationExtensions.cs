// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis;

internal static class CompilationExtensions
{
    extension(Compilation compilation)
    {
        public ImmutableArray<Compilation> GetReferencedCompilations()
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

        public ImmutableArray<IAssemblySymbol> GetReferencedAssemblySymbols(bool excludePreviousSubmissions = false)
        {
            // The first module of every assembly is its source module and the source
            // module always has the list of all referenced assemblies.
            var referencedAssemblySymbols = compilation.Assembly.Modules.First().ReferencedAssemblySymbols;

            if (excludePreviousSubmissions)
            {
                return referencedAssemblySymbols;
            }

            // Do a quick check first to avoid unnecessary allocations in case this is not a script compilation.
            var previous = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;
            if (previous is null)
            {
                return referencedAssemblySymbols;
            }

            var builder = ArrayBuilder<IAssemblySymbol>.GetInstance();
            builder.AddRange(referencedAssemblySymbols);

            while (previous != null)
            {
                builder.Add(previous.Assembly);
                previous = previous.ScriptCompilationInfo?.PreviousScriptCompilation;
            }

            return builder.ToImmutableAndFree();
        }

        public INamedTypeSymbol? ArgumentExceptionType()
            => compilation.GetTypeByMetadataName(typeof(ArgumentException).FullName!);

        public INamedTypeSymbol? ArgumentNullExceptionType()
            => compilation.GetTypeByMetadataName(typeof(ArgumentNullException).FullName!);

        public INamedTypeSymbol? ArgumentOutOfRangeExceptionType()
            => compilation.GetTypeByMetadataName(typeof(ArgumentOutOfRangeException).FullName!);

        public INamedTypeSymbol? ArrayType()
            => compilation.GetTypeByMetadataName(typeof(Array).FullName!);

        public INamedTypeSymbol? AttributeType()
            => compilation.GetTypeByMetadataName(typeof(Attribute).FullName!);

        public INamedTypeSymbol? BlockingCollectionOfTType()
            => compilation.GetTypeByMetadataName(typeof(BlockingCollection<>).FullName!);

        public INamedTypeSymbol? CollectionOfTType()
            => compilation.GetTypeByMetadataName(typeof(Collection<>).FullName!);

        public INamedTypeSymbol? ExceptionType()
            => compilation.GetTypeByMetadataName(typeof(Exception).FullName!);

        public INamedTypeSymbol? DebuggerDisplayAttributeType()
            => compilation.GetTypeByMetadataName(typeof(System.Diagnostics.DebuggerDisplayAttribute).FullName!);

        public INamedTypeSymbol? StructLayoutAttributeType()
            => compilation.GetTypeByMetadataName(typeof(StructLayoutAttribute).FullName!);

        public INamedTypeSymbol? InlineArrayAttributeType()
            => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.InlineArrayAttribute");

        public INamedTypeSymbol? DesignerCategoryAttributeType()
            => compilation.GetTypeByMetadataName("System.ComponentModel.DesignerCategoryAttribute");

        public INamedTypeSymbol? DesignerGeneratedAttributeType()
            => compilation.GetTypeByMetadataName("Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute");

        public INamedTypeSymbol? HideModuleNameAttribute()
            => compilation.GetTypeByMetadataName("Microsoft.VisualBasic.HideModuleNameAttribute");

        public INamedTypeSymbol? ThreadStaticAttributeType()
            => compilation.GetTypeByMetadataName(typeof(ThreadStaticAttribute).FullName!);

        public INamedTypeSymbol? FormattableStringType()
            => compilation.GetTypeByMetadataName(typeof(FormattableString).FullName!);

        public INamedTypeSymbol? EventArgsType()
            => compilation.GetTypeByMetadataName(typeof(EventArgs).FullName!);

        public INamedTypeSymbol? NotImplementedExceptionType()
            => compilation.GetTypeByMetadataName(typeof(NotImplementedException).FullName!);

        public INamedTypeSymbol? EqualityComparerOfTType()
            => compilation.GetTypeByMetadataName(typeof(EqualityComparer<>).FullName!);

        public INamedTypeSymbol? ActionType()
            => compilation.GetTypeByMetadataName(typeof(Action).FullName!);

        public INamedTypeSymbol? ExpressionOfTType()
            => compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");

        public INamedTypeSymbol? EditorBrowsableAttributeType()
            => compilation.GetTypeByMetadataName(typeof(EditorBrowsableAttribute).FullName!);

        public INamedTypeSymbol? EditorBrowsableStateType()
            => compilation.GetTypeByMetadataName(typeof(EditorBrowsableState).FullName!);

        public INamedTypeSymbol? TaskType()
            => compilation.GetTypeByMetadataName(typeof(Task).FullName!);

        public INamedTypeSymbol? TaskOfTType()
            => compilation.GetTypeByMetadataName(typeof(Task<>).FullName!);

        public INamedTypeSymbol? ValueTaskType()
            => compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");

        public INamedTypeSymbol? ValueTaskOfTType()
            => compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        public INamedTypeSymbol? IEnumerableType()
            => compilation.GetTypeByMetadataName(typeof(System.Collections.IEnumerable).FullName!);

        public INamedTypeSymbol? IEnumerableOfTType()
            => compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName!);

        public INamedTypeSymbol? IEnumeratorOfTType()
            => compilation.GetTypeByMetadataName(typeof(IEnumerator<>).FullName!);

        public INamedTypeSymbol? IListOfTType()
            => compilation.GetTypeByMetadataName(typeof(IList<>).FullName!);

        public INamedTypeSymbol? IReadOnlyListOfTType()
            => compilation.GetTypeByMetadataName(typeof(IReadOnlyList<>).FullName!);

        public INamedTypeSymbol? ISetOfTType()
            => compilation.GetTypeByMetadataName(typeof(ISet<>).FullName!);

        public INamedTypeSymbol? IReadOnlySetOfTType()
            => compilation.GetTypeByMetadataName(typeof(IReadOnlySet<>).FullName!);

        public INamedTypeSymbol? IAsyncEnumerableOfTType()
            => compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");

        public INamedTypeSymbol? IAsyncEnumeratorOfTType()
            => compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1");

        public INamedTypeSymbol? ImmutableArrayOfTType()
            => compilation.GetTypeByMetadataName(typeof(ImmutableArray<>).FullName!);

        public INamedTypeSymbol? SerializableAttributeType()
            => compilation.GetTypeByMetadataName(typeof(SerializableAttribute).FullName!);

        public INamedTypeSymbol? CoClassType()
            => compilation.GetTypeByMetadataName(typeof(CoClassAttribute).FullName!);

        public INamedTypeSymbol? ComAliasNameAttributeType()
            => compilation.GetTypeByMetadataName(typeof(ComAliasNameAttribute).FullName!);

        public INamedTypeSymbol? SuppressMessageAttributeType()
            => compilation.GetTypeByMetadataName(typeof(SuppressMessageAttribute).FullName!);

        public INamedTypeSymbol? TupleElementNamesAttributeType()
            => compilation.GetTypeByMetadataName(typeof(TupleElementNamesAttribute).FullName!);

        public INamedTypeSymbol? NativeIntegerAttributeType()
            => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.NativeIntegerAttribute");

        public INamedTypeSymbol? DynamicAttributeType()
            => compilation.GetTypeByMetadataName(typeof(DynamicAttribute).FullName!);

        public INamedTypeSymbol? LazyOfTType()
            => compilation.GetTypeByMetadataName(typeof(Lazy<>).FullName!);

        public INamedTypeSymbol? ISerializableType()
            => compilation.GetTypeByMetadataName(typeof(ISerializable).FullName!);

        public INamedTypeSymbol? SerializationInfoType()
            => compilation.GetTypeByMetadataName(typeof(SerializationInfo).FullName!);

        public INamedTypeSymbol? StreamingContextType()
            => compilation.GetTypeByMetadataName(typeof(StreamingContext).FullName!);

        public INamedTypeSymbol? OnDeserializingAttribute()
            => compilation.GetTypeByMetadataName(typeof(OnDeserializingAttribute).FullName!);

        public INamedTypeSymbol? OnDeserializedAttribute()
            => compilation.GetTypeByMetadataName(typeof(OnDeserializedAttribute).FullName!);

        public INamedTypeSymbol? OnSerializingAttribute()
            => compilation.GetTypeByMetadataName(typeof(OnSerializingAttribute).FullName!);

        public INamedTypeSymbol? OnSerializedAttribute()
            => compilation.GetTypeByMetadataName(typeof(OnSerializedAttribute).FullName!);

        public INamedTypeSymbol? ComRegisterFunctionAttribute()
            => compilation.GetTypeByMetadataName(typeof(ComRegisterFunctionAttribute).FullName!);

        public INamedTypeSymbol? ComUnregisterFunctionAttribute()
            => compilation.GetTypeByMetadataName(typeof(ComUnregisterFunctionAttribute).FullName!);

        public INamedTypeSymbol? ConditionalAttribute()
            => compilation.GetTypeByMetadataName(typeof(ConditionalAttribute).FullName!);

        public INamedTypeSymbol? ObsoleteAttribute()
            => compilation.GetTypeByMetadataName(typeof(ObsoleteAttribute).FullName!);

        public INamedTypeSymbol? SystemCompositionImportingConstructorAttribute()
            => compilation.GetTypeByMetadataName("System.Composition.ImportingConstructorAttribute");

        public INamedTypeSymbol? SystemComponentModelCompositionImportingConstructorAttribute()
            => compilation.GetTypeByMetadataName("System.ComponentModel.Composition.ImportingConstructorAttribute");

        public INamedTypeSymbol? SystemIDisposableType()
            => compilation.GetTypeByMetadataName(typeof(IDisposable).FullName!);

        public INamedTypeSymbol? NotNullAttribute()
            => compilation.GetTypeByMetadataName(typeof(NotNullAttribute).FullName!);

        public INamedTypeSymbol? MaybeNullAttribute()
            => compilation.GetTypeByMetadataName(typeof(MaybeNullAttribute).FullName!);

        public INamedTypeSymbol? MaybeNullWhenAttribute()
            => compilation.GetTypeByMetadataName(typeof(MaybeNullWhenAttribute).FullName!);

        public INamedTypeSymbol? AllowNullAttribute()
            => compilation.GetTypeByMetadataName(typeof(AllowNullAttribute).FullName!);

        public INamedTypeSymbol? DisallowNullAttribute()
            => compilation.GetTypeByMetadataName(typeof(DisallowNullAttribute).FullName!);

        public INamedTypeSymbol? DataMemberAttribute()
            => compilation.GetTypeByMetadataName(typeof(DataMemberAttribute).FullName!);

        public INamedTypeSymbol? DataContractAttribute()
            => compilation.GetTypeByMetadataName(typeof(DataContractAttribute).FullName!);

        public INamedTypeSymbol? AsyncMethodBuilderAttribute()
            => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");

        public INamedTypeSymbol? CancellationTokenType()
            => compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!);

        public INamedTypeSymbol? ValueTupleType(int arity)
            => compilation.GetTypeByMetadataName($"System.ValueTuple`{arity}");

        public INamedTypeSymbol? ListOfTType()
            => compilation.GetTypeByMetadataName(typeof(List<>).FullName!);

        public INamedTypeSymbol? ReadOnlySpanOfTType()
            => compilation.GetTypeByMetadataName(typeof(ReadOnlySpan<>).FullName!);

        public INamedTypeSymbol? SpanOfTType()
            => compilation.GetTypeByMetadataName(typeof(Span<>).FullName!);

        public INamedTypeSymbol? InterpolatedStringHandlerAttributeType()
            => compilation.GetTypeByMetadataName(typeof(InterpolatedStringHandlerAttribute).FullName!);

        /// <summary>
        /// Gets a type by its metadata name to use for code analysis within a <see cref="Compilation"/>. This method
        /// attempts to find the "best" symbol to use for code analysis, which is the symbol matching the first of the
        /// following rules.
        ///
        /// <list type="number">
        ///   <item><description>
        ///     If only one type with the given name is found within the compilation and its referenced assemblies, that
        ///     type is returned regardless of accessibility.
        ///   </description></item>
        ///   <item><description>
        ///     If the current <paramref name="compilation"/> defines the symbol, that symbol is returned.
        ///   </description></item>
        ///   <item><description>
        ///     If exactly one referenced assembly defines the symbol in a manner that makes it visible to the current
        ///     <paramref name="compilation"/>, that symbol is returned.
        ///   </description></item>
        ///   <item><description>
        ///     Otherwise, this method returns <see langword="null"/>.
        ///   </description></item>
        /// </list>
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> to consider for analysis.</param>
        /// <param name="fullyQualifiedMetadataName">The fully-qualified metadata type name to find.</param>
        /// <returns>The symbol to use for code analysis; otherwise, <see langword="null"/>.</returns>
        public INamedTypeSymbol? GetBestTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            INamedTypeSymbol? type = null;

            foreach (var currentType in compilation.GetTypesByMetadataName(fullyQualifiedMetadataName))
            {
                if (ReferenceEquals(currentType.ContainingAssembly, compilation.Assembly))
                {
                    Debug.Assert(type is null);
                    return currentType;
                }

                switch (currentType.GetResultantVisibility())
                {
                    case SymbolVisibility.Public:
                    case SymbolVisibility.Internal when currentType.ContainingAssembly.GivesAccessTo(compilation.Assembly):
                        break;

                    default:
                        continue;
                }

                if (type is object)
                {
                    // Multiple visible types with the same metadata name are present
                    return null;
                }

                type = currentType;
            }

            return type;
        }

        /// <summary>
        /// Gets implicit method, that wraps top-level statements.
        /// </summary>
        public IMethodSymbol? GetTopLevelStatementsMethod()
        {
            foreach (var candidateTopLevelType in compilation.SourceModule.GlobalNamespace.GetTypeMembers(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName, arity: 0))
            {
                foreach (var candidateMember in candidateTopLevelType.GetMembers(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName))
                {
                    if (candidateMember is IMethodSymbol method)
                        return method;
                }
            }

            return null;
        }

        public INamedTypeSymbol? TryGetCallingConventionSymbol(string callingConvention)
            => compilation.GetBestTypeByMetadataName($"System.Runtime.CompilerServices.CallConv{callingConvention}");
    }
}
