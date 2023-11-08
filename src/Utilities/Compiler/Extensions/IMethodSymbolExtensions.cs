// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#nullable disable warnings

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

#if HAS_IOPERATION
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
#endif

namespace Analyzer.Utilities.Extensions
{
    internal static class IMethodSymbolExtensions
    {
        /// <summary>
        /// Checks if the given method overrides <see cref="object.Equals(object)"/>.
        /// </summary>
        public static bool IsObjectEqualsOverride(this IMethodSymbol method)
        {
            return method != null &&
                method.IsOverride &&
                method.Name == WellKnownMemberNames.ObjectEquals &&
                method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                method.Parameters.Length == 1 &&
                method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
                IsObjectMethodOverride(method);
        }

        /// <summary>
        /// Checks if the given method is <see cref="object.Equals(object)"/>.
        /// </summary>
        public static bool IsObjectEquals(this IMethodSymbol method)
        {
            return method != null &&
                method.ContainingType.SpecialType == SpecialType.System_Object &&
                method.IsVirtual &&
                method.Name == WellKnownMemberNames.ObjectEquals &&
                method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                method.Parameters.Length == 1 &&
                method.Parameters[0].Type.SpecialType == SpecialType.System_Object;
        }

        /// <summary>
        /// Checks if the given <paramref name="method"/> is <see cref="object.Equals(object, object)"/> or <see cref="object.ReferenceEquals(object, object)"/>.
        /// </summary>
        public static bool IsStaticObjectEqualsOrReferenceEquals(this IMethodSymbol method)
        {
            return method != null &&
                method.IsStatic &&
                method.ContainingType.SpecialType == SpecialType.System_Object &&
                method.Parameters.Length == 2 &&
                method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
                method.Parameters[1].Type.SpecialType == SpecialType.System_Object &&
                (method.Name == WellKnownMemberNames.ObjectEquals || method.Name == "ReferenceEquals");
        }

        /// <summary>
        /// Checks if the given method overrides Object.GetHashCode.
        /// </summary>
        public static bool IsGetHashCodeOverride(this IMethodSymbol method)
        {
            return method != null &&
                   method.IsOverride &&
                   method.Name == WellKnownMemberNames.ObjectGetHashCode &&
                   method.ReturnType.SpecialType == SpecialType.System_Int32 &&
                   method.Parameters.IsEmpty &&
                   IsObjectMethodOverride(method);
        }

        /// <summary>
        /// Checks if the given method overrides Object.ToString.
        /// </summary>
        public static bool IsToStringOverride(this IMethodSymbol method)
        {
            return method != null &&
                   method.IsOverride &&
                   method.ReturnType.SpecialType == SpecialType.System_String &&
                   method.Name == WellKnownMemberNames.ObjectToString &&
                   method.Parameters.IsEmpty &&
                   IsObjectMethodOverride(method);
        }

        /// <summary>
        /// Checks if the given method overrides a method from System.Object
        /// </summary>
        private static bool IsObjectMethodOverride(IMethodSymbol method)
        {
            IMethodSymbol overriddenMethod = method.OverriddenMethod;
            while (overriddenMethod != null)
            {
                if (overriddenMethod.ContainingType.SpecialType == SpecialType.System_Object)
                {
                    return true;
                }

                overriddenMethod = overriddenMethod.OverriddenMethod;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given method is a Finalizer implementation.
        /// </summary>
        public static bool IsFinalizer(this IMethodSymbol method)
        {
            if (method.MethodKind == MethodKind.Destructor)
            {
                return true; // for C#
            }

            if (method.Name != WellKnownMemberNames.DestructorName || !method.Parameters.IsEmpty || !method.ReturnsVoid)
            {
                return false;
            }

            IMethodSymbol overridden = method.OverriddenMethod;

            if (method.ContainingType.SpecialType == SpecialType.System_Object)
            {
                // This is object.Finalize
                return true;
            }

            if (overridden == null)
            {
                return false;
            }

            for (IMethodSymbol o = overridden.OverriddenMethod; o != null; o = o.OverriddenMethod)
            {
                overridden = o;
            }

            return overridden.ContainingType.SpecialType == SpecialType.System_Object; // it is object.Finalize
        }

        /// <summary>
        /// Checks if the given method is an implementation of the given interface method
        /// Substituted with the given typeargument.
        /// </summary>
        public static bool IsImplementationOfInterfaceMethod(this IMethodSymbol method, ITypeSymbol? typeArgument, [NotNullWhen(returnValue: true)] INamedTypeSymbol? interfaceType, string interfaceMethodName)
        {
            INamedTypeSymbol? constructedInterface = typeArgument != null ? interfaceType?.Construct(typeArgument) : interfaceType;

            return constructedInterface?.GetMembers(interfaceMethodName).FirstOrDefault() is IMethodSymbol interfaceMethod &&
                SymbolEqualityComparer.Default.Equals(method, method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod));
        }

        /// <summary>
        /// Checks if the given method implements IDisposable.Dispose()
        /// </summary>
        public static bool IsDisposeImplementation(this IMethodSymbol method, Compilation compilation)
        {
            INamedTypeSymbol? iDisposable = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIDisposable);
            return method.IsDisposeImplementation(iDisposable);
        }

        /// <summary>
        /// Checks if the given method implements IAsyncDisposable.Dispose()
        /// </summary>
        public static bool IsAsyncDisposeImplementation(this IMethodSymbol method, Compilation compilation)
        {
            INamedTypeSymbol? iAsyncDisposable = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIAsyncDisposable);
            INamedTypeSymbol? valueTaskType = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksValueTask);
            return method.IsAsyncDisposeImplementation(iAsyncDisposable, valueTaskType);
        }

        /// <summary>
        /// Checks if the given method implements <see cref="IDisposable.Dispose"/> or overrides an implementation of <see cref="IDisposable.Dispose"/>.
        /// </summary>
        public static bool IsDisposeImplementation([NotNullWhen(returnValue: true)] this IMethodSymbol? method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? iDisposable)
        {
            if (method == null)
            {
                return false;
            }

            if (method.IsOverride)
            {
                return method.OverriddenMethod.IsDisposeImplementation(iDisposable);
            }

            // Identify the implementor of IDisposable.Dispose in the given method's containing type and check
            // if it is the given method.
            return method.ReturnsVoid &&
                method.Parameters.IsEmpty &&
                method.IsImplementationOfInterfaceMethod(null, iDisposable, "Dispose");
        }

        /// <summary>
        /// Checks if the given method implements "IAsyncDisposable.Dispose" or overrides an implementation of "IAsyncDisposable.Dispose".
        /// </summary>
        public static bool IsAsyncDisposeImplementation([NotNullWhen(returnValue: true)] this IMethodSymbol? method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? iAsyncDisposable, [NotNullWhen(returnValue: true)] INamedTypeSymbol? valueTaskType)
        {
            if (method == null)
            {
                return false;
            }

            if (method.IsOverride)
            {
                return method.OverriddenMethod.IsAsyncDisposeImplementation(iAsyncDisposable, valueTaskType);
            }

            // Identify the implementor of IAsyncDisposable.Dispose in the given method's containing type and check
            // if it is the given method.
            return SymbolEqualityComparer.Default.Equals(method.ReturnType, valueTaskType) &&
                method.Parameters.IsEmpty &&
                method.IsImplementationOfInterfaceMethod(null, iAsyncDisposable, "DisposeAsync");
        }

        /// <summary>
        /// Checks if the given method has the signature "void Dispose()".
        /// </summary>
        private static bool HasDisposeMethodSignature(this IMethodSymbol method)
        {
            return method.Name == "Dispose" && method.MethodKind == MethodKind.Ordinary &&
                method.ReturnsVoid && method.Parameters.IsEmpty;
        }

        /// <summary>
        /// Checks if the given method matches Dispose method convention and can be recognized by "using".
        /// </summary>
        public static bool HasDisposeSignatureByConvention(this IMethodSymbol method)
        {
            return method.HasDisposeMethodSignature()
                && !method.IsStatic
                && !method.IsPrivate();
        }

        /// <summary>
        /// Checks if the given method has the signature "void Dispose(bool)".
        /// </summary>
        public static bool HasDisposeBoolMethodSignature(this IMethodSymbol method)
        {
            if (method.Name == "Dispose" && method.MethodKind == MethodKind.Ordinary &&
                method.ReturnsVoid && method.Parameters.Length == 1)
            {
                IParameterSymbol parameter = method.Parameters[0];
                return parameter.Type != null &&
                    parameter.Type.SpecialType == SpecialType.System_Boolean &&
                    parameter.RefKind == RefKind.None;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given method has the signature "void Close()".
        /// </summary>
        private static bool HasDisposeCloseMethodSignature(this IMethodSymbol method)
        {
            return method.Name == "Close" && method.MethodKind == MethodKind.Ordinary &&
                method.ReturnsVoid && method.Parameters.IsEmpty;
        }

        /// <summary>
        /// Checks if the given method has the signature "Task CloseAsync()".
        /// </summary>
        private static bool HasDisposeCloseAsyncMethodSignature(this IMethodSymbol method, INamedTypeSymbol? taskType)
            => taskType != null && method.Parameters.IsEmpty && method.Name == "CloseAsync" &&
                SymbolEqualityComparer.Default.Equals(method.ReturnType, taskType);

        /// <summary>
        /// Checks if the given method has the signature "Task DisposeAsync()" or "ValueTask DisposeAsync()" or "ConfiguredValueTaskAwaitable DisposeAsync()".
        /// </summary>
        private static bool HasDisposeAsyncMethodSignature(this IMethodSymbol method,
            INamedTypeSymbol? task,
            INamedTypeSymbol? valueTask,
            INamedTypeSymbol? configuredValueTaskAwaitable)
        {
            return method.Name == "DisposeAsync" &&
                method.MethodKind == MethodKind.Ordinary &&
                method.Parameters.IsEmpty &&
                (SymbolEqualityComparer.Default.Equals(method.ReturnType, task) ||
                 SymbolEqualityComparer.Default.Equals(method.ReturnType, valueTask) ||
                 SymbolEqualityComparer.Default.Equals(method.ReturnType, configuredValueTaskAwaitable));
        }

        /// <summary>
        /// Checks if the given method has the signature "override Task DisposeCoreAsync(bool)" or "override Task DisposeAsyncCore(bool)".
        /// </summary>
        private static bool HasOverriddenDisposeCoreAsyncMethodSignature(this IMethodSymbol method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? task)
        {
            return (method.Name == "DisposeAsyncCore" || method.Name == "DisposeCoreAsync") &&
                method.MethodKind == MethodKind.Ordinary &&
                method.IsOverride &&
                SymbolEqualityComparer.Default.Equals(method.ReturnType, task) &&
                method.Parameters.Length == 1 &&
                method.Parameters[0].Type.SpecialType == SpecialType.System_Boolean;
        }

        /// <summary>
        /// Checks if the given method has the signature "virtual ValueTask DisposeCoreAsync()" or "virtual ValueTask DisposeAsyncCore()".
        /// </summary>
        private static bool HasVirtualDisposeCoreAsyncMethodSignature(this IMethodSymbol method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? valueTask)
        {
            return (method.Name == "DisposeAsyncCore" || method.Name == "DisposeCoreAsync") &&
                method.MethodKind == MethodKind.Ordinary &&
                method.IsVirtual &&
                SymbolEqualityComparer.Default.Equals(method.ReturnType, valueTask) &&
                method.Parameters.Length == 0;
        }

        /// <summary>
        /// Gets the <see cref="DisposeMethodKind"/> for the given method.
        /// </summary>
        public static DisposeMethodKind GetDisposeMethodKind(this IMethodSymbol method, Compilation compilation)
        {
            INamedTypeSymbol? iDisposable = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIDisposable);
            INamedTypeSymbol? iAsyncDisposable = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIAsyncDisposable);
            INamedTypeSymbol? configuredAsyncDisposable = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesConfiguredAsyncDisposable);
            INamedTypeSymbol? task = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask);
            INamedTypeSymbol? valueTask = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksValueTask);
            INamedTypeSymbol? configuredValueTaskAwaitable = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesConfiguredValueTaskAwaitable);
            return method.GetDisposeMethodKind(iDisposable, iAsyncDisposable, configuredAsyncDisposable, task, valueTask, configuredValueTaskAwaitable);
        }

        /// <summary>
        /// Gets the <see cref="DisposeMethodKind"/> for the given method.
        /// </summary>
        public static DisposeMethodKind GetDisposeMethodKind(
            this IMethodSymbol method,
            INamedTypeSymbol? iDisposable,
            INamedTypeSymbol? iAsyncDisposable,
            INamedTypeSymbol? configuredAsyncDisposable,
            INamedTypeSymbol? task,
            INamedTypeSymbol? valueTask,
            INamedTypeSymbol? configuredValueTaskAwaitable)
        {
            if (method.ContainingType.IsDisposable(iDisposable, iAsyncDisposable, configuredAsyncDisposable))
            {
                if (IsDisposeImplementation(method, iDisposable) ||
                    (SymbolEqualityComparer.Default.Equals(method.ContainingType, iDisposable) &&
                     method.HasDisposeMethodSignature())
#if CODEANALYSIS_V3_OR_BETTER
                    || (method.ContainingType.IsRefLikeType &&
                     method.HasDisposeSignatureByConvention())
#endif
                )
                {
                    return DisposeMethodKind.Dispose;
                }
                else if (method.HasDisposeBoolMethodSignature())
                {
                    return DisposeMethodKind.DisposeBool;
                }
                else if (method.IsAsyncDisposeImplementation(iAsyncDisposable, valueTask) || method.HasDisposeAsyncMethodSignature(task, valueTask, configuredValueTaskAwaitable))
                {
                    return DisposeMethodKind.DisposeAsync;
                }
                else if (method.HasOverriddenDisposeCoreAsyncMethodSignature(task))
                {
                    return DisposeMethodKind.DisposeCoreAsync;
                }
                else if (method.HasVirtualDisposeCoreAsyncMethodSignature(valueTask))
                {
                    return DisposeMethodKind.DisposeCoreAsync;
                }
                else if (method.HasDisposeCloseMethodSignature())
                {
                    return DisposeMethodKind.Close;
                }
                else if (method.HasDisposeCloseAsyncMethodSignature(task))
                {
                    return DisposeMethodKind.CloseAsync;
                }
            }

            return DisposeMethodKind.None;
        }

        /// <summary>
        /// Checks if the given method implements 'System.Runtime.Serialization.IDeserializationCallback.OnDeserialization' or overrides an implementation of 'System.Runtime.Serialization.IDeserializationCallback.OnDeserialization'/>.
        /// </summary>
        public static bool IsOnDeserializationImplementation([NotNullWhen(returnValue: true)] this IMethodSymbol? method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? iDeserializationCallback)
        {
            if (method == null)
            {
                return false;
            }

            if (method.IsOverride)
            {
                return method.OverriddenMethod.IsOnDeserializationImplementation(iDeserializationCallback);
            }

            // Identify the implementor of IDisposable.Dispose in the given method's containing type and check
            // if it is the given method.
            return method.ReturnsVoid &&
                method.Parameters.Length == 1 &&
                method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
                method.IsImplementationOfInterfaceMethod(null, iDeserializationCallback, "OnDeserialization");
        }

        public static bool IsSerializationConstructor([NotNullWhen(returnValue: true)] this IMethodSymbol? method, INamedTypeSymbol? serializationInfoType, INamedTypeSymbol? streamingContextType)
            => method.IsConstructor() &&
                method.Parameters.Length == 2 &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, serializationInfoType) &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, streamingContextType);

        public static bool IsGetObjectData([NotNullWhen(returnValue: true)] this IMethodSymbol? method, INamedTypeSymbol? serializationInfoType, INamedTypeSymbol? streamingContextType)
            => method?.Name == "GetObjectData" &&
                method.ReturnsVoid &&
                method.Parameters.Length == 2 &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, serializationInfoType) &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, streamingContextType);

        /// <summary>
        /// Checks if the method is a property getter.
        /// </summary>
        public static bool IsPropertyGetter(this IMethodSymbol method)
        {
            return method.MethodKind == MethodKind.PropertyGet &&
                   method.AssociatedSymbol?.GetParameters().Length == 0;
        }

        /// <summary>
        /// Checks if the method is the getter for an indexer.
        /// </summary>
        public static bool IsIndexerGetter(this IMethodSymbol method)
        {
            return method.MethodKind == MethodKind.PropertyGet &&
                   method.AssociatedSymbol.IsIndexer();
        }

        /// <summary>
        /// Checks if the method is an accessor for a property.
        /// </summary>
        public static bool IsPropertyAccessor(this IMethodSymbol method)
        {
            return method.MethodKind is MethodKind.PropertyGet or
                   MethodKind.PropertySet;
        }

        /// <summary>
        /// Checks if the method is an accessor for an event.
        /// </summary>
        public static bool IsEventAccessor(this IMethodSymbol method)
        {
            return method.MethodKind is MethodKind.EventAdd or
                   MethodKind.EventRaise or
                   MethodKind.EventRemove;
        }

        public static bool IsOperator(this IMethodSymbol methodSymbol)
        {
            return methodSymbol.MethodKind is MethodKind.UserDefinedOperator or MethodKind.BuiltinOperator;
        }

        public static bool HasOptionalParameters(this IMethodSymbol methodSymbol)
        {
            return methodSymbol.Parameters.Any(p => p.IsOptional);
        }

        public static IEnumerable<IMethodSymbol> GetOverloads(this IMethodSymbol? method)
        {
            var methods = method?.ContainingType?.GetMembers(method.Name).OfType<IMethodSymbol>();
            if (methods != null)
            {
                foreach (var member in methods)
                {
                    if (!SymbolEqualityComparer.Default.Equals(member, method))
                    {
                        yield return member;
                    }
                }
            }
        }

        /// <summary>
        /// Set of well-known collection add method names.
        /// Used in <see cref="IsCollectionAddMethod"/> heuristic.
        /// </summary>
        private static readonly ImmutableHashSet<string> s_collectionAddMethodNameVariants =
            ImmutableHashSet.Create(StringComparer.Ordinal, "Add", "AddOrUpdate", "GetOrAdd", "TryAdd", "TryUpdate");

        /// <summary>
        /// Determine if the specific method is an Add method that adds to a collection.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <param name="iCollectionTypes">Collection types.</param>
        /// <returns>'true' if <paramref name="method"/> is believed to be the add method of a collection.</returns>
        /// <remarks>
        /// We use the following heuristic to determine if a method is a collection add method:
        /// 1. Method's enclosing type implements any of the given <paramref name="iCollectionTypes"/>.
        /// 2. Any of the following name heuristics are met:
        ///     a. Method's name is from one of the well-known add method names from <see cref="s_collectionAddMethodNameVariants"/> ("Add", "AddOrUpdate", "GetOrAdd", "TryAdd", or "TryUpdate")
        ///     b. Method's name begins with "Add" (FxCop compat)
        /// </remarks>
        public static bool IsCollectionAddMethod(this IMethodSymbol method, ImmutableHashSet<INamedTypeSymbol> iCollectionTypes)
        {
            if (iCollectionTypes.IsEmpty)
            {
                return false;
            }

            if (!s_collectionAddMethodNameVariants.Contains(method.Name) &&
                !method.Name.StartsWith("Add", StringComparison.Ordinal))
            {
                return false;
            }

            return method.ContainingType.AllInterfaces.Any(i => iCollectionTypes.Contains(i.OriginalDefinition));
        }

        /// <summary>
        /// Determine if the specific method is a Task.FromResult method that wraps a result in a task.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <param name="taskType">Task type.</param>
        public static bool IsTaskFromResultMethod(this IMethodSymbol method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? taskType)
            => method.Name.Equals("FromResult", StringComparison.Ordinal) &&
               SymbolEqualityComparer.Default.Equals(method.ContainingType, taskType);

        /// <summary>
        /// Determine if the specific method is a Task.ConfigureAwait(bool) method.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <param name="genericTaskType">Generic task type.</param>
        public static bool IsTaskConfigureAwaitMethod(this IMethodSymbol method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? genericTaskType)
            => method.Name.Equals("ConfigureAwait", StringComparison.Ordinal) &&
               method.Parameters.Length == 1 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_Boolean &&
               SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, genericTaskType);

        /// <summary>
        /// Determine if the specific method is a TaskAsyncEnumerableExtensions.ConfigureAwait(this IAsyncDisposable, bool) extension method.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <param name="asyncDisposableType">IAsyncDisposable named type.</param>
        /// <param name="taskAsyncEnumerableExtensions">System.Threading.Tasks.TaskAsyncEnumerableExtensions named type.</param>
        public static bool IsAsyncDisposableConfigureAwaitMethod(this IMethodSymbol method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? asyncDisposableType, [NotNullWhen(returnValue: true)] INamedTypeSymbol? taskAsyncEnumerableExtensions)
            => method.IsExtensionMethod &&
               method.Name.Equals("ConfigureAwait", StringComparison.Ordinal) &&
               method.Parameters.Length == 2 &&
               SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, asyncDisposableType) &&
               method.Parameters[1].Type.SpecialType == SpecialType.System_Boolean &&
               SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, taskAsyncEnumerableExtensions) &&
               taskAsyncEnumerableExtensions.IsStatic;

#if HAS_IOPERATION
        /// <summary>
        /// PERF: Cache from method symbols to their topmost block operations to enable interprocedural flow analysis
        /// across analyzers and analyzer callbacks to re-use the operations, semanticModel and control flow graph.
        /// </summary>
        /// <remarks>Also see <see cref="IOperationExtensions.s_operationToCfgCache"/></remarks>
        private static readonly BoundedCache<Compilation, ConcurrentDictionary<IMethodSymbol, IBlockOperation?>> s_methodToTopmostOperationBlockCache
            = new();

        /// <summary>
        /// Returns the topmost <see cref="IBlockOperation"/> for given <paramref name="method"/>.
        /// </summary>
        public static IBlockOperation? GetTopmostOperationBlock(this IMethodSymbol method, Compilation compilation, CancellationToken cancellationToken = default)
        {
            var methodToBlockMap = s_methodToTopmostOperationBlockCache.GetOrCreateValue(compilation);
            return methodToBlockMap.GetOrAdd(method, ComputeTopmostOperationBlock);

            // Local functions.
            IBlockOperation? ComputeTopmostOperationBlock(IMethodSymbol unused)
            {
                if (!SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, compilation.Assembly))
                {
                    return null;
                }

                foreach (var decl in method.DeclaringSyntaxReferences)
                {
                    var syntax = decl.GetSyntax(cancellationToken);

                    // VB Workaround: declaration.GetSyntax returns StatementSyntax nodes instead of BlockSyntax nodes
                    //                GetOperation returns null for StatementSyntax, and the method's operation block for BlockSyntax.
                    if (compilation.Language == LanguageNames.VisualBasic)
                    {
                        syntax = syntax.Parent;
                    }

                    var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                    foreach (var descendant in syntax.DescendantNodesAndSelf())
                    {
                        var operation = semanticModel.GetOperation(descendant, cancellationToken);
                        if (operation is IBlockOperation blockOperation)
                        {
                            return blockOperation;
                        }
                    }
                }

                return null;
            }
        }
#endif

        public static bool IsLambdaOrLocalFunctionOrDelegate(this IMethodSymbol method)
        {
            return method.MethodKind switch
            {
                MethodKind.LambdaMethod or MethodKindEx.LocalFunction or MethodKind.DelegateInvoke => true,
                _ => false,
            };
        }

        public static bool IsLambdaOrLocalFunction(this IMethodSymbol method)
        {
            return method.MethodKind switch
            {
                MethodKind.LambdaMethod or MethodKindEx.LocalFunction => true,
                _ => false,
            };
        }

        public static int GetParameterIndex(this IMethodSymbol methodSymbol, IParameterSymbol parameterSymbol)
        {
            for (var i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(parameterSymbol, methodSymbol.Parameters[i]))
                {
                    return i;
                }
            }

            throw new ArgumentException("Invalid parameter", nameof(parameterSymbol));
        }

        /// <summary>
        /// Returns true for void returning methods with two parameters, where
        /// the first parameter is of <see cref="object"/> type and the second
        /// parameter inherits from or equals <see cref="EventArgs"/> type or
        /// whose name ends with 'EventArgs'.
        /// </summary>
        public static bool HasEventHandlerSignature(this IMethodSymbol method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? eventArgsType)
            => eventArgsType != null &&
               method.ReturnsVoid &&
               method.Parameters.Length == 2 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
               // FxCop compat: Struct with name ending with "EventArgs" are allowed
               // + UWP has specific EventArgs not inheriting from 'System.EventArgs'.
               // See https://github.com/dotnet/roslyn-analyzers/issues/3106
               (method.Parameters[1].Type.DerivesFrom(eventArgsType, baseTypesOnly: true) || method.Parameters[1].Type.Name.EndsWith("EventArgs", StringComparison.Ordinal));

        public static bool IsLockMethod(this IMethodSymbol method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? systemThreadingMonitor)
        {
            // "System.Threading.Monitor.Enter(object)" OR "System.Threading.Monitor.Enter(object, bool)"
            return method.Name == "Enter" &&
                   SymbolEqualityComparer.Default.Equals(method.ContainingType, systemThreadingMonitor) &&
                   method.ReturnsVoid &&
                   !method.Parameters.IsEmpty &&
                   method.Parameters[0].Type.SpecialType == SpecialType.System_Object;
        }

        public static bool IsInterlockedExchangeMethod(this IMethodSymbol method, INamedTypeSymbol? systemThreadingInterlocked)
        {
            Debug.Assert(SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, systemThreadingInterlocked));

            // "System.Threading.Interlocked.Exchange(ref T, T)"
            return method.Name == "Exchange" &&
                   method.Parameters.Length == 2 &&
                   method.Parameters[0].RefKind == RefKind.Ref &&
                   method.Parameters[1].RefKind != RefKind.Ref &&
                   SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, method.Parameters[1].Type);
        }

        public static bool IsInterlockedCompareExchangeMethod(this IMethodSymbol method, INamedTypeSymbol? systemThreadingInterlocked)
        {
            Debug.Assert(SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, systemThreadingInterlocked));

            // "System.Threading.Interlocked.CompareExchange(ref T, T, T)"
            return method.Name == "CompareExchange" &&
                   method.Parameters.Length == 3 &&
                   method.Parameters[0].RefKind == RefKind.Ref &&
                   method.Parameters[1].RefKind != RefKind.Ref &&
                   method.Parameters[2].RefKind != RefKind.Ref &&
                   SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, method.Parameters[1].Type) &&
                   SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, method.Parameters[2].Type);
        }

        public static bool HasParameterWithDelegateType(this IMethodSymbol methodSymbol)
            => methodSymbol.Parameters.Any(p => p.Type.TypeKind == TypeKind.Delegate);

        /// <summary>
        /// Find out if the method overrides from target virtual method of a certain type
        /// or it is the virtual method itself.
        /// </summary>
        /// <param name="methodSymbol">The method</param>
        /// <param name="typeSymbol">The type has virtual method</param>
        public static bool IsOverrideOrVirtualMethodOf([NotNullWhen(returnValue: true)] this IMethodSymbol? methodSymbol, [NotNullWhen(returnValue: true)] INamedTypeSymbol? typeSymbol)
        {
            if (methodSymbol == null)
            {
                return false;
            }
            else
            {
                if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, typeSymbol))
                {
                    return true;
                }
                else
                {
                    return IsOverrideOrVirtualMethodOf(methodSymbol.OverriddenMethod, typeSymbol);
                }
            }
        }

        /// <summary>
        /// Returns true if this is a bool returning static method whose name starts with "IsNull"
        /// with a single parameter whose type is not a value type.
        /// For example, "static bool string.IsNullOrEmpty()"
        /// </summary>
        public static bool IsArgumentNullCheckMethod(this IMethodSymbol method)
        {
            return method.IsStatic &&
                method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                method.Name.StartsWith("IsNull", StringComparison.Ordinal) &&
                method.Parameters.Length == 1 &&
                !method.Parameters[0].Type.IsValueType;
        }

        public static bool IsBenchmarkOrXUnitTestMethod(this IMethodSymbol method, ConcurrentDictionary<INamedTypeSymbol, bool> knownTestAttributes, INamedTypeSymbol? benchmarkAttribute, INamedTypeSymbol? xunitFactAttribute)
        {
            foreach (var attribute in method.GetAttributes())
            {
                if (attribute.AttributeClass.IsBenchmarkOrXUnitTestAttribute(knownTestAttributes, benchmarkAttribute, xunitFactAttribute))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a method is an auto-property accessor.
        /// </summary>
        public static bool IsAutoPropertyAccessor(this IMethodSymbol methodSymbol)
            => methodSymbol.IsPropertyAccessor()
            && methodSymbol.AssociatedSymbol is IPropertySymbol propertySymbol
            && propertySymbol.IsAutoProperty();

        /// <summary>
        /// Check if the given <paramref name="methodSymbol"/> is an implicitly generated method for top level statements.
        /// </summary>
        public static bool IsTopLevelStatementsEntryPointMethod([NotNullWhen(true)] this IMethodSymbol? methodSymbol)
            => methodSymbol?.IsStatic == true && methodSymbol.Name switch
            {
                "$Main" => true,
                "<Main>$" => true,
                _ => false
            };

        public static bool IsGetAwaiterFromAwaitablePattern([NotNullWhen(true)] this IMethodSymbol? method,
            [NotNullWhen(true)] INamedTypeSymbol? inotifyCompletionType,
            [NotNullWhen(true)] INamedTypeSymbol? icriticalNotifyCompletionType)
        {
            if (method is null
                || !method.Name.Equals("GetAwaiter", StringComparison.Ordinal)
                || method.Parameters.Length != 0)
            {
                return false;
            }

            var returnType = method.ReturnType?.OriginalDefinition;
            if (returnType is null)
            {
                return false;
            }

            return returnType.DerivesFrom(inotifyCompletionType) ||
                returnType.DerivesFrom(icriticalNotifyCompletionType);
        }

        public static bool IsGetResultFromAwaiterPattern(
            [NotNullWhen(true)] this IMethodSymbol? method,
            [NotNullWhen(true)] INamedTypeSymbol? inotifyCompletionType,
            [NotNullWhen(true)] INamedTypeSymbol? icriticalNotifyCompletionType)
        {
            if (method is null
                || !method.Name.Equals("GetResult", StringComparison.Ordinal)
                || method.Parameters.Length != 0)
            {
                return false;
            }

            var containingType = method.ContainingType?.OriginalDefinition;
            if (containingType is null)
            {
                return false;
            }

            return containingType.DerivesFrom(inotifyCompletionType) ||
                containingType.DerivesFrom(icriticalNotifyCompletionType);
        }

        public static ImmutableArray<IMethodSymbol> GetOriginalDefinitions(this IMethodSymbol methodSymbol)
        {
            ImmutableArray<IMethodSymbol>.Builder originalDefinitionsBuilder = ImmutableArray.CreateBuilder<IMethodSymbol>();

            if (methodSymbol.IsOverride && (methodSymbol.OverriddenMethod != null))
            {
                originalDefinitionsBuilder.Add(methodSymbol.OverriddenMethod);
            }

            if (!methodSymbol.ExplicitInterfaceImplementations.IsEmpty)
            {
                originalDefinitionsBuilder.AddRange(methodSymbol.ExplicitInterfaceImplementations);
            }

            var typeSymbol = methodSymbol.ContainingType;
            var methodSymbolName = methodSymbol.Name;

            originalDefinitionsBuilder.AddRange(typeSymbol.AllInterfaces
                .SelectMany(m => m.GetMembers(methodSymbolName))
                .OfType<IMethodSymbol>()
                .Where(m => methodSymbol.Parameters.Length == m.Parameters.Length
                            && methodSymbol.Arity == m.Arity
                            && typeSymbol.FindImplementationForInterfaceMember(m) != null));

            return originalDefinitionsBuilder.ToImmutable();
        }
    }
}
