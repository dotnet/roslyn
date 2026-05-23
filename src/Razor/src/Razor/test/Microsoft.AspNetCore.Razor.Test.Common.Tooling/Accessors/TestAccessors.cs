// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/winforms/blob/8812d0e14e2cc9fd9870d70e31bfa083add7e541/src/Common/tests/TestUtilities/TestAccessors.cs

using System;

namespace Microsoft.AspNetCore.Razor.Test.Common.Accessors;

/// <summary>
///  Extension methods for associating internals test accessors with
///  types being tested.
/// </summary>
/// <remarks>
///  In the System namespace for implicit discovery.
/// </remarks>
public static partial class TestAccessors
{
    // Need to pass a null parameter when constructing a static instance
    // of TestAccessor. As this is pretty common and never changes, caching
    // the array here.
    private static readonly object?[] s_nullObjectParam = [null];

    /// <summary>
    ///  Extension that creates a generic internals test accessor for a
    ///  given instance or Type class (if only accessing statics).
    /// </summary>
    /// <param name="instanceOrType">
    ///  Instance or Type class (if only accessing statics).
    /// </param>
    /// <example>
    /// <![CDATA[
    ///  Version version = new Version(4, 1);
    ///  Assert.Equal(4, version.TestAccessor().Dynamic._Major));
    ///
    ///  // Or
    ///
    ///  dynamic accessor = version.TestAccessor().Dynamic;
    ///  Assert.Equal(4, accessor._Major));
    ///
    /// // Or
    ///
    ///  Version version2 = new Version("4.1");
    ///  dynamic accessor = typeof(Version).TestAccessor().Dynamic;
    ///  Assert.Equal(version2, accessor.Parse("4.1")));
    /// ]]>
    /// </example>
    public static ITestAccessor TestAccessor(this object instanceOrType)
    {
        var testAccessor = instanceOrType is Type type
            ? (ITestAccessor?)Activator.CreateInstance(
                typeof(TestAccessor<>).MakeGenericType(type),
                s_nullObjectParam)
            : (ITestAccessor?)Activator.CreateInstance(
                typeof(TestAccessor<>).MakeGenericType(instanceOrType.GetType()),
                instanceOrType);

        if (testAccessor is null)
        {
            throw new ArgumentException("Cannot create TestAccessor for Nullable<T> instances with no value.");
        }

        return testAccessor;
    }
}
