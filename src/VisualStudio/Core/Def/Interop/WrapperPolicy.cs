// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

internal static class WrapperPolicy
{
    /// <summary>
    /// Factory object for creating IComWrapperFixed instances.
    /// Internal and not readonly so that unit tests can provide an alternative implementation.
    /// </summary>
    internal static IComWrapperFactory s_ComWrapperFactory =
        (IComWrapperFactory)PackageUtilities.CreateInstance(typeof(IComWrapperFactory).GUID);

    internal static object CreateAggregatedObject(object managedObject) => s_ComWrapperFactory.CreateAggregatedObject(managedObject);

    /// <summary>
    /// Return the RCW for the native IComWrapperFixed instance aggregating "managedObject"
    /// if there is one. Return "null" if "managedObject" is not aggregated.
    /// </summary>
    internal static IComWrapperFixed? TryGetWrapper(object managedObject)
    {
        // Note: this method should be "return managedObject" once we can get rid of this while IComWrapperFixed
        // business.

        // This force the CLR to retrieve the "outer" object of "managedObject"
        // if "managedObject" has been aggregated
        var ptr = Marshal.GetIUnknownForObject(managedObject);
        try
        {
            // This asks the CLR to return the RCW corresponding to the
            // aggregator object.
            var wrapper = Marshal.GetObjectForIUnknown(ptr);

            // The aggregator (if there is one) implement IComWrapperFixed!
            return wrapper as IComWrapperFixed;
        }
        finally
        {
            Marshal.Release(ptr);
        }
    }
}
