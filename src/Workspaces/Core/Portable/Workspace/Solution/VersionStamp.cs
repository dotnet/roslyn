// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// VersionStamp should be only used to compare versions returned by same API.
/// </summary>
public readonly struct VersionStamp : IEquatable<VersionStamp>
{
    public static VersionStamp Default => default;

    private const int GlobalVersionMarker = -1;
    private const int InitialGlobalVersion = 10000;

    /// <summary>
    /// global counter to avoid collision within same session. 
    /// it starts with a big initial number just for a clarity in debugging
    /// </summary>
    private static int s_globalVersion = InitialGlobalVersion;

    /// <summary>
    /// time stamp
    /// </summary>
    private readonly DateTime _utcLastModified;

    /// <summary>
    /// indicate whether there was a collision on same item
    /// </summary>
    private readonly int _localIncrement;

    /// <summary>
    /// unique version in same session
    /// </summary>
    private readonly int _globalIncrement;

    private VersionStamp(DateTime utcLastModified)
        : this(utcLastModified, 0)
    {
    }

    private VersionStamp(DateTime utcLastModified, int localIncrement)
        : this(utcLastModified, localIncrement, GetNextGlobalVersion())
    {
    }

    private VersionStamp(DateTime utcLastModified, int localIncrement, int globalIncrement)
    {
        if (utcLastModified != default && utcLastModified.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(WorkspacesResources.DateTimeKind_must_be_Utc, nameof(utcLastModified));
        }

        _utcLastModified = utcLastModified;
        _localIncrement = localIncrement;
        _globalIncrement = globalIncrement;
    }

    /// <summary>
    /// Creates a new instance of a VersionStamp.
    /// </summary>
    public static VersionStamp Create()
        => new(DateTime.UtcNow);

    /// <summary>
    /// Creates a new instance of a version stamp based on the specified DateTime.
    /// </summary>
    public static VersionStamp Create(DateTime utcTimeLastModified)
        => new(utcTimeLastModified);

    /// <summary>
    /// compare two different versions and return either one of the versions if there is no collision, otherwise, create a new version
    /// that can be used later to compare versions between different items
    /// </summary>
    public VersionStamp GetNewerVersion(VersionStamp version)
    {
        // * NOTE *
        // in current design/implementation, there are 4 possible ways for a version to be created.
        //
        // 1. created from a file stamp (most likely by starting a new session). "increment" will have 0 as value
        // 2. created by modifying existing item (text changes, project changes etc).
        //    "increment" will have either 0 or previous increment + 1 if there was a collision.
        // 3. created from deserialization (probably by using persistent service).
        // 4. created by accumulating versions of multiple items.
        //
        // and this method is the one that is responsible for #4 case.

        if (_utcLastModified > version._utcLastModified)
        {
            return this;
        }

        if (_utcLastModified == version._utcLastModified)
        {
            var thisGlobalVersion = GetGlobalVersion(this);
            var thatGlobalVersion = GetGlobalVersion(version);

            if (thisGlobalVersion == thatGlobalVersion)
            {
                // given versions are same one
                return this;
            }

            // mark it as global version
            // global version can't be moved to newer version.
            return new VersionStamp(_utcLastModified, (thisGlobalVersion > thatGlobalVersion) ? thisGlobalVersion : thatGlobalVersion, GlobalVersionMarker);
        }

        return version;
    }

    /// <summary>
    /// Gets a new VersionStamp that is guaranteed to be newer than its base one
    /// this should only be used for same item to move it to newer version
    /// </summary>
    public VersionStamp GetNewerVersion()
    {
        // global version can't be moved to newer version
        Debug.Assert(_globalIncrement != GlobalVersionMarker);

        var now = DateTime.UtcNow;
        var incr = (now == _utcLastModified) ? _localIncrement + 1 : 0;

        return new VersionStamp(now, incr);
    }

    /// <summary>
    /// Returns the serialized text form of the VersionStamp.
    /// </summary>
    public override string ToString()
    {
        // 'o' is the roundtrip format that captures the most detail.
        return _utcLastModified.ToString("o") + "-" + _globalIncrement + "-" + _localIncrement;
    }

    public override int GetHashCode()
        => Hash.Combine(_utcLastModified.GetHashCode(), _localIncrement);

    public override bool Equals(object? obj)
    {
        if (obj is VersionStamp v)
        {
            return this.Equals(v);
        }

        return false;
    }

    public bool Equals(VersionStamp version)
    {
        if (_utcLastModified == version._utcLastModified)
        {
            return GetGlobalVersion(this) == GetGlobalVersion(version);
        }

        return false;
    }

    public static bool operator ==(VersionStamp left, VersionStamp right)
        => left.Equals(right);

    public static bool operator !=(VersionStamp left, VersionStamp right)
        => !left.Equals(right);

    private static int GetGlobalVersion(VersionStamp version)
    {
        // global increment < 0 means it is a global version which has its global increment in local increment
        return version._globalIncrement >= 0 ? version._globalIncrement : version._localIncrement;
    }

    private static int GetNextGlobalVersion()
    {
        // REVIEW: not sure what is best way to wrap it when it overflows. should I just throw or don't care.
        // with 50ms (typing) as an interval for a new version, it gives more than 1 year before int32 to overflow.
        // with 5ms as an interval, it gives more than 120 days before it overflows.
        // since global version is only for per VS session, I think we don't need to worry about overflow.
        // or we could use Int64 which will give more than a million years turn around even on 1ms interval.

        // this will let versions to be compared safely between multiple items
        // without worrying about collision within same session
        var globalVersion = Interlocked.Increment(ref VersionStamp.s_globalVersion);

        return globalVersion;
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(VersionStamp versionStamp)
    {

        /// <summary>
        /// True if this VersionStamp is newer than the specified one.
        /// </summary>
        internal bool IsNewerThan(in VersionStamp version)
        {
            if (versionStamp._utcLastModified > version._utcLastModified)
            {
                return true;
            }

            if (versionStamp._utcLastModified == version._utcLastModified)
            {
                return GetGlobalVersion(versionStamp) > GetGlobalVersion(version);
            }

            return false;
        }
    }
}
