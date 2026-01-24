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

    public static INamedTypeSymbol? ArgumentExceptionType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(ArgumentException).FullName!);

    public static INamedTypeSymbol? ArgumentNullExceptionType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(ArgumentNullException).FullName!);

    public static INamedTypeSymbol? ArgumentOutOfRangeExceptionType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(ArgumentOutOfRangeException).FullName!);

    public static INamedTypeSymbol? ArrayType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(Array).FullName!);

    public static INamedTypeSymbol? AttributeType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(Attribute).FullName!);

    public static INamedTypeSymbol? BlockingCollectionOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(BlockingCollection<>).FullName!);

    public static INamedTypeSymbol? CollectionOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(Collection<>).FullName!);

    public static INamedTypeSymbol? ExceptionType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(Exception).FullName!);

    public static INamedTypeSymbol? DebuggerDisplayAttributeType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(System.Diagnostics.DebuggerDisplayAttribute).FullName!);

    public static INamedTypeSymbol? StructLayoutAttributeType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(StructLayoutAttribute).FullName!);

    public static INamedTypeSymbol? InlineArrayAttributeType(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.InlineArrayAttribute");

    public static INamedTypeSymbol? DesignerCategoryAttributeType(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.ComponentModel.DesignerCategoryAttribute");

    public static INamedTypeSymbol? DesignerGeneratedAttributeType(this Compilation compilation)
        => compilation.GetTypeByMetadataName("Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute");

    public static INamedTypeSymbol? HideModuleNameAttribute(this Compilation compilation)
        => compilation.GetTypeByMetadataName("Microsoft.VisualBasic.HideModuleNameAttribute");

    public static INamedTypeSymbol? ThreadStaticAttributeType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(ThreadStaticAttribute).FullName!);

    public static INamedTypeSymbol? FormattableStringType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(FormattableString).FullName!);

    public static INamedTypeSymbol? IFormattableType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(IFormattable).FullName!);

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

    public static INamedTypeSymbol? ICollectionOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(ICollection<>).FullName!);

    public static INamedTypeSymbol? IEnumerableType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(System.Collections.IEnumerable).FullName!);

    public static INamedTypeSymbol? IEnumerableOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName!);

    public static INamedTypeSymbol? IEnumeratorOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(IEnumerator<>).FullName!);

    public static INamedTypeSymbol? IListOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(IList<>).FullName!);

    public static INamedTypeSymbol? IReadOnlyListOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(IReadOnlyList<>).FullName!);

    public static INamedTypeSymbol? ISetOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(ISet<>).FullName!);

    public static INamedTypeSymbol? IReadOnlySetOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(IReadOnlySet<>).FullName!);

    public static INamedTypeSymbol? IAsyncEnumerableOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");

    public static INamedTypeSymbol? IAsyncEnumeratorOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1");

    public static INamedTypeSymbol? ImmutableArrayOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(ImmutableArray<>).FullName!);

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

    public static INamedTypeSymbol? AsyncMethodBuilderAttribute(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");

    public static INamedTypeSymbol? CancellationTokenType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!);

    public static INamedTypeSymbol? ValueTupleType(this Compilation compilation, int arity)
        => compilation.GetTypeByMetadataName($"System.ValueTuple`{arity}");

    public static INamedTypeSymbol? ListOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(List<>).FullName!);

    public static INamedTypeSymbol? ReadOnlySpanOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(ReadOnlySpan<>).FullName!);

    public static INamedTypeSymbol? SpanOfTType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(Span<>).FullName!);

    public static INamedTypeSymbol? InterpolatedStringHandlerAttributeType(this Compilation compilation)
        => compilation.GetTypeByMetadataName(typeof(InterpolatedStringHandlerAttribute).FullName!);

    public static INamedTypeSymbol? DateOnlyType(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.DateOnly");

    public static INamedTypeSymbol? TimeOnlyType(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.TimeOnly");

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
    public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
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
    public static IMethodSymbol? GetTopLevelStatementsMethod(this Compilation compilation)
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

    public static INamedTypeSymbol? TryGetCallingConventionSymbol(this Compilation compilation, string callingConvention)
        => compilation.GetBestTypeByMetadataName($"System.Runtime.CompilerServices.CallConv{callingConvention}");
}
