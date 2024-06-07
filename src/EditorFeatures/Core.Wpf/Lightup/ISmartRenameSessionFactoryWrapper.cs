// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Lightup;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorFeatures.Lightup;

[Obsolete("Class has not been finalized and may change without warning.")]
internal readonly struct ISmartRenameSessionFactoryWrapper
{
    internal const string WrappedTypeName = "Microsoft.VisualStudio.Text.Editor.SmartRename.ISmartRenameSessionFactory";
    private static readonly Type s_wrappedType;

    private static readonly Func<object, SnapshotSpan, object?> s_createSmartRenameSession;

    private readonly object _instance;

    static ISmartRenameSessionFactoryWrapper()
    {
        s_wrappedType = typeof(AggregateFocusInterceptor).Assembly.GetType(WrappedTypeName, throwOnError: false, ignoreCase: false);
        s_createSmartRenameSession = LightupHelpers.CreateFunctionAccessor<object, SnapshotSpan, object?>(s_wrappedType, nameof(CreateSmartRenameSession), typeof(SnapshotSpan), SpecializedTasks.Null<object>());
    }

    private ISmartRenameSessionFactoryWrapper(object instance)
    {
        this._instance = instance;
    }

    public static ISmartRenameSessionFactoryWrapper FromInstance(object? instance)
    {
        if (instance == null)
        {
            return default;
        }

        if (!IsInstance(instance))
        {
            throw new InvalidCastException($"Cannot cast '{instance.GetType().FullName}' to '{WrappedTypeName}'");
        }

        return new ISmartRenameSessionFactoryWrapper(instance);
    }

    public static bool IsInstance([NotNullWhen(true)] object? instance)
    {
        return instance != null && LightupHelpers.CanWrapObject(instance, s_wrappedType);
    }

    public ISmartRenameSessionWrapper? CreateSmartRenameSession(SnapshotSpan renamedIdentifier)
    {
        var session = s_createSmartRenameSession(_instance, renamedIdentifier);
        if (!ISmartRenameSessionWrapper.IsInstance(session))
            return null;

        return ISmartRenameSessionWrapper.FromInstance(session);
    }
}
