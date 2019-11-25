// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

#if HAS_IOPERATION
using System.Collections.Concurrent;
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
                   method.Parameters.Length == 0 &&
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
                   method.Parameters.Length == 0 &&
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

            if (method.Name != WellKnownMemberNames.DestructorName || method.Parameters.Length != 0 || !method.ReturnsVoid)
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
        public static bool IsImplementationOfInterfaceMethod(this IMethodSymbol method, ITypeSymbol typeArgument, INamedTypeSymbol interfaceType, string interfaceMethodName)
        {
            INamedTypeSymbol constructedInterface = typeArgument != null ? interfaceType?.Construct(typeArgument) : interfaceType;

            return constructedInterface?.GetMembers(interfaceMethodName).FirstOrDefault() is IMethodSymbol interfaceMethod && method.Equals(method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod));
        }

        /// <summary>
        /// Checks if the given method implements IDisposable.Dispose()
        /// </summary>
        public static bool IsDisposeImplementation(this IMethodSymbol method, Compilation compilation)
        {
            INamedTypeSymbol iDisposable = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIDisposable);
            return method.IsDisposeImplementation(iDisposable);
        }

        /// <summary>
        /// Checks if the given method implements <see cref="IDisposable.Dispose"/> or overrides an implementation of <see cref="IDisposable.Dispose"/>.
        /// </summary>
        public static bool IsDisposeImplementation(this IMethodSymbol method, INamedTypeSymbol iDisposable)
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
                method.Parameters.Length == 0 &&
                method.IsImplementationOfInterfaceMethod(null, iDisposable, "Dispose");
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
        /// Checks if the given method has the signature "Task DisposeAsync()".
        /// </summary>
        private static bool HasDisposeAsyncMethodSignature(this IMethodSymbol method, INamedTypeSymbol task)
        {
            return method.Name == "DisposeAsync" &&
                method.MethodKind == MethodKind.Ordinary &&
                method.ReturnType.Equals(task) &&
                method.Parameters.IsEmpty;
        }

        /// <summary>
        /// Checks if the given method has the signature "override Task DisposeCoreAsync(bool)".
        /// </summary>
        private static bool HasOverriddenDisposeCoreAsyncMethodSignature(this IMethodSymbol method, INamedTypeSymbol task)
        {
            return method.Name == "DisposeCoreAsync" &&
                method.MethodKind == MethodKind.Ordinary &&
                method.IsOverride &&
                method.ReturnType.Equals(task) &&
                method.Parameters.Length == 1 &&
                method.Parameters[0].Type.SpecialType == SpecialType.System_Boolean;
        }

        /// <summary>
        /// Gets the <see cref="DisposeMethodKind"/> for the given method.
        /// </summary>
        public static DisposeMethodKind GetDisposeMethodKind(
            this IMethodSymbol method,
            INamedTypeSymbol iDisposable,
            INamedTypeSymbol task)
        {
            if (method.ContainingType.IsDisposable(iDisposable))
            {
                if (IsDisposeImplementation(method, iDisposable) ||
                    (Equals(method.ContainingType, iDisposable) &&
                     method.HasDisposeMethodSignature()))
                {
                    return DisposeMethodKind.Dispose;
                }
                else if (method.HasDisposeBoolMethodSignature())
                {
                    return DisposeMethodKind.DisposeBool;
                }
                else if (method.HasDisposeAsyncMethodSignature(task))
                {
                    return DisposeMethodKind.DisposeAsync;
                }
                else if (method.HasOverriddenDisposeCoreAsyncMethodSignature(task))
                {
                    return DisposeMethodKind.DisposeCoreAsync;
                }
                else if (method.HasDisposeCloseMethodSignature())
                {
                    return DisposeMethodKind.Close;
                }
            }

            return DisposeMethodKind.None;
        }

        /// <summary>
        /// Checks if the given method implements 'System.Runtime.Serialization.IDeserializationCallback.OnDeserialization' or overrides an implementation of 'System.Runtime.Serialization.IDeserializationCallback.OnDeserialization'/>.
        /// </summary>
        public static bool IsOnDeserializationImplementation(this IMethodSymbol method, INamedTypeSymbol iDeserializationCallback)
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
            return method.MethodKind == MethodKind.PropertyGet ||
                   method.MethodKind == MethodKind.PropertySet;
        }

        /// <summary>
        /// Checks if the method is an accessor for an event.
        /// </summary>
        public static bool IsEventAccessor(this IMethodSymbol method)
        {
            return method.MethodKind == MethodKind.EventAdd ||
                   method.MethodKind == MethodKind.EventRaise ||
                   method.MethodKind == MethodKind.EventRemove;
        }

        public static bool IsOperator(this IMethodSymbol methodSymbol)
        {
            return methodSymbol.MethodKind == MethodKind.UserDefinedOperator || methodSymbol.MethodKind == MethodKind.BuiltinOperator;
        }

        public static bool HasOptionalParameters(this IMethodSymbol methodSymbol)
        {
            return methodSymbol.Parameters.Any(p => p.IsOptional);
        }

        public static IEnumerable<IMethodSymbol> GetOverloads(this IMethodSymbol method)
        {
            foreach (var member in method?.ContainingType?.GetMembers(method.Name).OfType<IMethodSymbol>())
            {
                if (!member.Equals(method))
                {
                    yield return member;
                }
            }
        }

        /// <summary>
        /// Determine if the specific method is an Add method that adds to a collection.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <param name="iCollectionTypes">Collection types.</param>
        /// <returns>'true' if <paramref name="method"/> is believed to be the add method of a collection.</returns>
        /// <remarks>
        /// The current heuristic is that we consider a method to be an add method if its name begins with "Add" and its
        /// enclosing type derives from ICollection or any instantiation of ICollection&lt;T&gt;.
        /// </remarks>
        public static bool IsCollectionAddMethod(this IMethodSymbol method, ImmutableHashSet<INamedTypeSymbol> iCollectionTypes)
            => !iCollectionTypes.IsEmpty &&
               method.Name.StartsWith("Add", StringComparison.Ordinal) &&
               method.ContainingType.AllInterfaces.Any(i => iCollectionTypes.Contains(i.OriginalDefinition));

        /// <summary>
        /// Determine if the specific method is a Task.FromResult method that wraps a result in a task.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <param name="taskType">Task type.</param>
        public static bool IsTaskFromResultMethod(this IMethodSymbol method, INamedTypeSymbol taskType)
            => method.Name.Equals("FromResult", StringComparison.Ordinal) &&
               method.ContainingType.Equals(taskType);

        /// <summary>
        /// Determine if the specific method is a Task.ConfigureAwait(bool) method.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <param name="genericTaskType">Generic task type.</param>
        public static bool IsTaskConfigureAwaitMethod(this IMethodSymbol method, INamedTypeSymbol genericTaskType)
            => method.Name.Equals("ConfigureAwait", StringComparison.Ordinal) &&
               method.Parameters.Length == 1 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_Boolean &&
               method.ContainingType.OriginalDefinition.Equals(genericTaskType);

#if HAS_IOPERATION
        /// <summary>
        /// PERF: Cache from method symbols to their topmost block operations to enable interprocedural flow analysis
        /// across analyzers and analyzer callbacks to re-use the operations, semanticModel and control flow graph.
        /// </summary>
        /// <remarks>Also see <see cref="IOperationExtensions.s_operationToCfgCache"/></remarks>
        private static readonly BoundedCache<Compilation, ConcurrentDictionary<IMethodSymbol, IBlockOperation>> s_methodToTopmostOperationBlockCache
            = new BoundedCache<Compilation, ConcurrentDictionary<IMethodSymbol, IBlockOperation>>();

        /// <summary>
        /// Returns the topmost <see cref="IBlockOperation"/> for given <paramref name="method"/>.
        /// </summary>
        public static IBlockOperation GetTopmostOperationBlock(this IMethodSymbol method, Compilation compilation, CancellationToken cancellationToken = default)
        {
            var methodToBlockMap = s_methodToTopmostOperationBlockCache.GetOrCreateValue(compilation);
            return methodToBlockMap.GetOrAdd(method, ComputeTopmostOperationBlock);

            // Local functions.
            IBlockOperation ComputeTopmostOperationBlock(IMethodSymbol unused)
            {
                if (!Equals(method.ContainingAssembly, compilation.Assembly))
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
            switch (method.MethodKind)
            {
                case MethodKind.LambdaMethod:
                case MethodKindEx.LocalFunction:
                case MethodKind.DelegateInvoke:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsLambdaOrLocalFunction(this IMethodSymbol method)
        {
            switch (method.MethodKind)
            {
                case MethodKind.LambdaMethod:
                case MethodKindEx.LocalFunction:
                    return true;

                default:
                    return false;
            }
        }

        public static int GetParameterIndex(this IMethodSymbol methodSymbol, IParameterSymbol parameterSymbol)
        {
            for (var i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                if (Equals(parameterSymbol, methodSymbol.Parameters[i]))
                {
                    return i;
                }
            }

            throw new ArgumentException("Invalid paramater", nameof(parameterSymbol));
        }

        /// <summary>
        /// Returns true for void returning methods with two parameters, where
        /// the first parameter is of <see cref="object"/> type and the second
        /// parameter inherits from or equals <see cref="EventArgs"/> type.
        /// </summary>
        public static bool HasEventHandlerSignature(this IMethodSymbol method, INamedTypeSymbol eventArgsType)
            => eventArgsType != null &&
               method.Parameters.Length == 2 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
               method.Parameters[1].Type.DerivesFrom(eventArgsType, baseTypesOnly: true);

        public static bool IsLockMethod(this IMethodSymbol method, INamedTypeSymbol systemThreadingMonitor)
        {
            // "System.Threading.Monitor.Enter(object)" OR "System.Threading.Monitor.Enter(object, bool)"
            return method.Name == "Enter" &&
                   method.ContainingType.Equals(systemThreadingMonitor) &&
                   method.ReturnsVoid &&
                   method.Parameters.Length >= 1 &&
                   method.Parameters[0].Type.SpecialType == SpecialType.System_Object;
        }


        public static bool IsInterlockedExchangeMethod(this IMethodSymbol method, INamedTypeSymbol systemThreadingInterlocked)
        {
            Debug.Assert(method.ContainingType.OriginalDefinition.Equals(systemThreadingInterlocked));

            // "System.Threading.Interlocked.Exchange(ref T, T)"
            return method.Name == "Exchange" &&
                   method.Parameters.Length == 2 &&
                   method.Parameters[0].RefKind == RefKind.Ref &&
                   method.Parameters[1].RefKind != RefKind.Ref &&
                   method.Parameters[0].Type.Equals(method.Parameters[1].Type);
        }

        public static bool IsInterlockedCompareExchangeMethod(this IMethodSymbol method, INamedTypeSymbol systemThreadingInterlocked)
        {
            Debug.Assert(method.ContainingType.OriginalDefinition.Equals(systemThreadingInterlocked));

            // "System.Threading.Interlocked.CompareExchange(ref T, T, T)"
            return method.Name == "CompareExchange" &&
                   method.Parameters.Length == 3 &&
                   method.Parameters[0].RefKind == RefKind.Ref &&
                   method.Parameters[1].RefKind != RefKind.Ref &&
                   method.Parameters[2].RefKind != RefKind.Ref &&
                   method.Parameters[0].Type.Equals(method.Parameters[1].Type) &&
                   method.Parameters[1].Type.Equals(method.Parameters[2].Type);
        }

        public static bool HasParameterWithDelegateType(this IMethodSymbol methodSymbol)
            => methodSymbol.Parameters.Any(p => p.Type.TypeKind == TypeKind.Delegate);

        /// <summary>
        /// Find out if the method overrides from target virtual method of a certain type
        /// or it is the virtual method itself.
        /// </summary>
        /// <param name="methodSymbol">The method</param>
        /// <param name="typeSymbol">The type has virtual method</param>
        public static bool IsOverrideOrVirtualMethodOf(this IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
        {
            if (methodSymbol == null)
            {
                return false;
            }
            else
            {
                if (methodSymbol.ContainingType.Equals(typeSymbol))
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
    }
}
