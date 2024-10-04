// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

internal static class ComAggregate
{
    /// <summary>
    /// This method creates a native COM object that aggregates the passed in managed object.
    /// The reason we need to do this is to enable legacy managed code that expects managed casts
    /// expressions to perform a QI on the COM object wrapped by an RCW. These clients are relying
    /// on the fact that COM type equality is based on GUID, whereas type equality is identity in 
    /// the managed world.
    /// Example: IMethodXML is defined many times throughout VS and used by many managed clients
    ///          dealing with CodeFunction objects. If the CodeFunction objects they deal with are
    ///          direct references to managed objects, then casts operations are managed casts
    ///          (as opposed to QI calls), and they fail, since the managed type for IMethodXML
    ///          have different identity (since they are defined in different assemblies). The QI
    ///          works, since under the hood, the casts operations are converted to QI with 
    ///          a GUID which is shared between all these types.
    ///          The solution to this is to return to these managed clients a native object,
    ///          which wraps the managed implementation of these interface using aggregation.
    ///          This means the interfaces will be obtained through QI, while the implementation
    ///          will be forwarded to the managed implementation.
    /// </summary>
    internal static object CreateAggregatedObject(object managedObject)
        => WrapperPolicy.CreateAggregatedObject(managedObject);

    /// <summary>
    /// Return the RCW for the native IComWrapperFixed instance aggregating "managedObject"
    /// if there is one. Return "null" if "managedObject" is not aggregated.
    /// </summary>
    internal static IComWrapperFixed? TryGetWrapper(object managedObject)
        => WrapperPolicy.TryGetWrapper(managedObject);

    internal static T GetManagedObject<T>(object value) where T : class
    {
        Contract.ThrowIfNull(value, "value");

        if (value is IComWrapperFixed wrapper)
        {
            return GetManagedObject<T>(wrapper);
        }

        Debug.Assert(value is T, "Why are you casting an object to an reference type it doesn't support?");
        return (T)value;
    }

    internal static T GetManagedObject<T>(IComWrapperFixed comWrapper) where T : class
    {
        Contract.ThrowIfNull(comWrapper, "comWrapper");

        var handle = GCHandle.FromIntPtr(comWrapper.GCHandlePtr);
        var target = handle.Target;

        Contract.ThrowIfNull(target, "target");
        Debug.Assert(target is T, "Why are you casting an object to an reference type it doesn't support?");
        return (T)target;
    }

    internal static T? TryGetManagedObject<T>(object? value) where T : class
    {
        if (value is IComWrapperFixed wrapper)
        {
            return TryGetManagedObject<T>(wrapper);
        }

        return value as T;
    }

    internal static T? TryGetManagedObject<T>(IComWrapperFixed comWrapper) where T : class
    {
        if (comWrapper == null)
        {
            return null;
        }

        var handle = GCHandle.FromIntPtr(comWrapper.GCHandlePtr);
        return handle.Target as T;
    }
}
