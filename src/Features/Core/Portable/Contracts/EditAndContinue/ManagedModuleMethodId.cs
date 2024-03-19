// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

/// <summary>
/// ManagedModuleMethodId is a token/version pair which is used to uniquely identify the
/// symbol store's understanding of a particular CLR method within a module context.
/// See <see cref="ManagedMethodId"/> for more details.
/// </summary>
[DataContract]
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
internal readonly struct ManagedModuleMethodId : IEquatable<ManagedModuleMethodId>
{
    /// <summary>
    /// Creates a ManagedModuleMethodId.
    /// </summary>
    /// <param name="token">Method token.</param>
    /// <param name="version">Method version.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="token"/> is less or equals 0x06000000 or <paramref name="version"/> is less or equals zero. 
    /// </exception>
    public ManagedModuleMethodId(
        int token,
        int version)
    {
        // 0x06 means that the token is for a MethodDef.
        // Valid method tokens are expected to be greather than 0x06000000.
        if (token <= 0x06000000)
            throw new ArgumentOutOfRangeException(nameof(token));
        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version));

        Token = token;
        Version = version;
    }

    /// <summary>
    /// The method definition metadata token of the method that contains this symbol.
    /// </summary>
    [DataMember(Name = "token")]
    public int Token { get; }

    /// <summary>
    /// MethodVersion is a 1-based index. This will be '1' for methods that have not
    /// been edited through Edit-and-continue. For edited methods, the version indicates
    /// the EnC apply of this method.
    /// Thus, if the user does 5 EnC applies and a particular method is only edited in the 5th apply, 
    /// then there are two method ids for this method, and they have Version = 1 and Version = 5.
    ///
    /// The debugger needs to deal with old versions of the method because they will
    /// continue to be on the call stack until control is unwound.The debugger can also hit
    /// breakpoints or stop for exceptions within exception handling regions of old
    /// methods. In other words, if the user sets a breakpoint within the catch block of a
    /// non-leaf method, the debugger needs to set that breakpoint within the old version
    /// of the method.
    /// 
    /// In scenarios such as function breakpoint binding, the value '0' may used to
    /// indicate the current version of the method.
    /// </summary>
    [DataMember(Name = "version")]
    public int Version { get; }

    public bool Equals(ManagedModuleMethodId other)
    {
        return Token == other.Token && Version == other.Version;
    }

    public override bool Equals(object? obj) => obj is ManagedModuleMethodId method && Equals(method);

    public override int GetHashCode() => Token ^ Version;

    public static bool operator ==(ManagedModuleMethodId left, ManagedModuleMethodId right) => left.Equals(right);

    public static bool operator !=(ManagedModuleMethodId left, ManagedModuleMethodId right) => !(left == right);

    internal string GetDebuggerDisplay() => $"0x{Token:X8} v{Version}";
}
