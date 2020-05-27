// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Shared.Lightup
{
    internal static class WrapperHelper
    {
        private static readonly ImmutableDictionary<Type, Type?> WrappedTypes;

        static WrapperHelper()
        {
            var codeAnalysisAssembly = typeof(ISymbol).GetTypeInfo().Assembly;
            var builder = ImmutableDictionary.CreateBuilder<Type, Type?>();

            builder.Add(typeof(IFunctionPointerTypeSymbolWrapper), codeAnalysisAssembly.GetType(IFunctionPointerTypeSymbolWrapper.WrappedTypeName));

            WrappedTypes = builder.ToImmutable();
        }

        /// <summary>
        /// Gets the type that is wrapped by the given wrapper.
        /// </summary>
        /// <param name="wrapperType">Type of the wrapper for which the wrapped type should be retrieved.</param>
        /// <returns>The wrapped type, or <see langword="null"/> if there is no info.</returns>
        internal static Type? GetWrappedType(Type wrapperType)
        {
            if (WrappedTypes.TryGetValue(wrapperType, out var wrappedType))
                return wrappedType;

            return null;
        }
    }
}
