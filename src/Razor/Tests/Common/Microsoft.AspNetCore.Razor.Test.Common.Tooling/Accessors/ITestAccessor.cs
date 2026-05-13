// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/winforms/blob/8812d0e14e2cc9fd9870d70e31bfa083add7e541/src/Common/tests/TestUtilities/ITestAccessor.cs

using System;

namespace Microsoft.AspNetCore.Razor.Test.Common.Accessors;

/// <summary>
///  Interface for accessing internals from tests.
/// </summary>
/// <remarks>
///  <para>
///   A non generic representation of the accessor functionality is needed to
///   allow dynamically creating arbitrary <see cref="TestAccessor{T}"/> from
///   helper methods.
///  </para>
/// </remarks>
public interface ITestAccessor
{
    /// <summary>
    ///  Gets a dynamic accessor to internals on the test object.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   This does not work for ref structs as they are not yet accessible via reflection.
    ///   See https://github.com/dotnet/runtime/issues/10057.
    ///  </para>
    /// </remarks>
    dynamic Dynamic { get; }

    /// <summary>
    ///  Creates a delegate for the given non-public method.
    /// </summary>
    /// <param name="methodName">
    ///  The method name. If null, uses the name of the delegate for the method.
    /// </param>
    /// <remarks>
    ///  <para>
    ///   This provides a way to access methods that take ref structs.
    ///  </para>
    /// </remarks>
    /// <example>
    ///  <![CDATA[
    ///   // For ref structs you have to define a delegate as ref struct types
    ///   // cannot be used as generic arguments (e.g. to Func/Span)
    ///
    ///   private delegate int GetDirectoryNameOffset(ReadOnlySpan<char> path);
    ///
    ///   public int InternalGetDirectoryNameOffset(ReadOnlySpan<char> path)
    ///   {
    ///       var accessor = typeof(System.IO.Path).TestAccessor();
    ///       return accessor.CreateDelegate<GetDirectoryNameOffset>()(@"C:\Foo");
    ///   }
    ///
    ///   // Without ref structs you can just use Func/Action
    ///   var accessor = typeof(Color).TestAccessor();
    ///   bool result = accessor.CreateDelegate<Func<KnownColor, bool>>("IsKnownColorSystem")(KnownColor.Window);
    ///  ]]>
    /// </example>
    TDelegate CreateDelegate<TDelegate>(string? methodName = null)
        where TDelegate : Delegate;
}
