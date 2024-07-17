// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Roslyn.Utilities;

/// <summary>
/// TODO: remove this exception: https://github.com/dotnet/roslyn/issues/40476
/// 
/// this represents soft crash request compared to hard crash which will bring down VS.
/// 
/// by soft crash, it means everything same as hard crash except it should use NFW and info bar
/// to inform users about unexpected condition instead of killing VS as traditional crash did.
/// 
/// in other words, no one should ever try to recover from this exception. but they must try to not hard crash.
/// 
/// this exception is based on cancellation exception since, in Roslyn code, cancellation exception is so far
/// only safest exception to throw without worrying about crashing VS 99%. there is still 1% case it will bring
/// down VS and those places should be guarded on this exception as we find such place.
/// 
/// for now, this is an opt-in based. if a feature wants to move to soft crash (ex, OOP), one should catch
/// exception and translate that to this exception and then add handler which report NFW and info bar in their
/// code path and make sure it doesn't bring down VS.
/// 
/// as we use soft-crash in more places, we should come up with more general framework.
/// </summary>
internal class SoftCrashException : OperationCanceledException
{
    public SoftCrashException() : base() { }

    public SoftCrashException(string message) : base(message) { }
    public SoftCrashException(CancellationToken token) : base(token) { }

    public SoftCrashException(string message, Exception innerException) : base(message, innerException) { }
    public SoftCrashException(string message, CancellationToken token) : base(message, token) { }
    public SoftCrashException(string message, Exception innerException, CancellationToken token) : base(message, innerException, token) { }
}
