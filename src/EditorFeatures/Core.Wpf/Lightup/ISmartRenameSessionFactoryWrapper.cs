// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorFeatures.Lightup;

[Obsolete("Class has not been finalized and may change without warning.")]
internal readonly struct ISmartRenameSessionFactoryWrapper
{
    internal const string WrappedTypeName = "Microsoft.VisualStudio.Text.Editor.SmartRename.ISmartRenameSessionFactory";
    private static readonly Type? s_wrappedType;

    private static readonly Func<object, SnapshotSpan, object?>? s_createSmartRenameSession;

    private readonly object _instance;

    static ISmartRenameSessionFactoryWrapper()
    {
        try
        {
            s_wrappedType = typeof(AggregateFocusInterceptor).Assembly.GetType(WrappedTypeName, throwOnError: false, ignoreCase: false);
            s_createSmartRenameSession = LightupHelpers.CreateFunctionAccessor<object, SnapshotSpan, object?>(s_wrappedType, nameof(CreateSmartRenameSession), typeof(SnapshotSpan), SpecializedTasks.Null<object>());
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
            // Editor side might change the interface, catch all exceptions when use reflection & composing linq expression so user could use the normal rename UI.
        }
    }

    private ISmartRenameSessionFactoryWrapper(object instance)
    {
        this._instance = instance;
    }

    public static ISmartRenameSessionFactoryWrapper? FromInstance(object? instance)
    {
        if (instance is null || s_wrappedType is null || s_createSmartRenameSession is null)
        {
            return null;
        }

        if (!IsInstance(instance))
        {
            FatalError.ReportNonFatalError(new InvalidCastException($"Cannot cast '{instance.GetType().FullName}' to '{WrappedTypeName}'"));
            return null;
        }

        return new ISmartRenameSessionFactoryWrapper(instance);
    }

    public static bool IsInstance([NotNullWhen(true)] object? instance)
    {
        return instance != null && s_wrappedType != null && LightupHelpers.CanWrapObject(instance, s_wrappedType);
    }

    public ISmartRenameSessionWrapper? CreateSmartRenameSession(SnapshotSpan renamedIdentifier)
    {
        if (s_createSmartRenameSession == null)
            return null;

        var session = s_createSmartRenameSession(_instance, renamedIdentifier);
        if (!ISmartRenameSessionWrapper.IsInstance(session))
            return null;

        return ISmartRenameSessionWrapper.FromInstance(session);
    }
}
