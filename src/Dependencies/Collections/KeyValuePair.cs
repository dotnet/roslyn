// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

// Copied from:
// https://github.com/dotnet/runtime/blob/9214279d93b8b422495a98eb4edda91e92bd60c3/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/KeyValuePair.cs

#if NET

using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable RS0016 // Add public types and members to the declared API (this is a supporting forwarder for an internal polyfill API)
[assembly: TypeForwardedTo(typeof(KeyValuePair))]
#pragma warning restore RS0016 // Add public types and members to the declared API

#else

namespace System.Collections.Generic;

internal static class KeyValuePair
{
    public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value) =>
        new KeyValuePair<TKey, TValue>(key, value);
}

#endif
