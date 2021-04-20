// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class OperationWrapperHelper
    {
        private static readonly Assembly s_codeAnalysisAssembly = typeof(SyntaxNode).GetTypeInfo().Assembly;

        private static readonly ImmutableDictionary<Type, Type?> WrappedTypes = ImmutableDictionary.Create<Type, Type?>()
            .Add(typeof(IUsingDeclarationOperationWrapper), s_codeAnalysisAssembly.GetType(IUsingDeclarationOperationWrapper.WrappedTypeName));

        /// <summary>
        /// Gets the type that is wrapped by the given wrapper.
        /// </summary>
        /// <param name = "wrapperType">Type of the wrapper for which the wrapped type should be retrieved.</param>
        /// <returns>The wrapped type, or <see langword="null"/> if there is no info.</returns>
        internal static Type? GetWrappedType(Type wrapperType)
        {
            if (WrappedTypes.TryGetValue(wrapperType, out var wrappedType))
            {
                return wrappedType;
            }

            return null;
        }
    }
}

#endif
