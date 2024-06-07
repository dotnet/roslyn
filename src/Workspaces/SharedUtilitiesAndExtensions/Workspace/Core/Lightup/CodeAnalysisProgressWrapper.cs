// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Lightup;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Progress;

namespace Microsoft.CodeAnalysis;

internal readonly struct CodeAnalysisProgressWrapper
{
    internal const string WrappedTypeName = "Microsoft.CodeAnalysis.CodeAnalysisProgress";
    private static readonly Type? s_wrappedType;

    internal static readonly IProgress<CodeAnalysisProgressWrapper> None = NullProgress<CodeAnalysisProgressWrapper>.Instance;

    private static readonly Func<string, object?> s_description;
    private static readonly Func<int, string?, object?> s_addIncompleteItems;
    private static readonly Func<int, string?, object?> s_addCompleteItems;
    private static readonly Func<object?> s_clear;

    private readonly object _instance;

    static CodeAnalysisProgressWrapper()
    {
        s_wrappedType = typeof(Workspace).Assembly.GetType(WrappedTypeName, throwOnError: false, ignoreCase: false);

        s_description = LightupHelpers.CreateStaticFunctionAccessor<string, object?>(s_wrappedType, nameof(Description), typeof(string), defaultValue: null);
        s_addIncompleteItems = LightupHelpers.CreateStaticFunctionAccessor<int, string?, object?>(s_wrappedType, nameof(AddIncompleteItems), typeof(int), typeof(string), defaultValue: null);
        s_addCompleteItems = LightupHelpers.CreateStaticFunctionAccessor<int, string?, object?>(s_wrappedType, nameof(AddCompleteItems), typeof(int), typeof(string), defaultValue: null);
        s_clear = LightupHelpers.CreateStaticFunctionAccessor<object?>(s_wrappedType, nameof(Clear), defaultValue: null);
    }

    private CodeAnalysisProgressWrapper(object instance)
    {
        _instance = instance;
    }

    public static Type? WrappedType => s_wrappedType;

    public object Instance => _instance;

    public static CodeAnalysisProgressWrapper Description(string description)
        => FromInstance(s_description(description));

    public static CodeAnalysisProgressWrapper AddIncompleteItems(int count, string? description = null)
        => FromInstance(s_addIncompleteItems(count, description));

    public static CodeAnalysisProgressWrapper AddCompleteItems(int count, string? description = null)
        => FromInstance(s_addCompleteItems(count, description));

    internal static CodeAnalysisProgressWrapper Clear()
        => FromInstance(s_clear());

#if !CODE_STYLE
    //public static implicit operator CodeAnalysisProgressWrapper(CodeAnalysisProgress instance)
    //    => new(instance);

    //public static implicit operator CodeAnalysisProgress(CodeAnalysisProgressWrapper wrapper)
    //    => wrapper._instance;
#endif

    public static CodeAnalysisProgressWrapper FromInstance(object? instance)
    {
        if (instance == null)
        {
            return default;
        }

        if (!IsInstance(instance))
        {
            throw new InvalidCastException($"Cannot cast '{instance.GetType().FullName}' to '{WrappedTypeName}'");
        }

        return new CodeAnalysisProgressWrapper(instance);
    }

    public static bool IsInstance([NotNullWhen(true)] object? instance)
    {
        return instance != null && LightupHelpers.CanWrapObject(instance, s_wrappedType);
    }
}
