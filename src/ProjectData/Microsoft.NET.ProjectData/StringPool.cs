// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// A thread-safe string deduplication pool. Ensures that equal strings share a single
/// object instance, reducing memory when many projects carry identical paths (SDK refs,
/// analyzer paths, NuGet packages, capabilities, property keys, etc.).
///
/// <para>Internally uses striped <see cref="ConcurrentDictionary{TKey, TValue}"/>s so
/// concurrent readers and writers experience minimal contention. Striping improves
/// cache locality across the 16 independent buckets.</para>
///
/// <para>The pool is intended to live as long as the data it deduplicates (e.g. scoped to
/// the <c>DataModelSharedState</c> lifetime). It is not self-evicting — entries stay until
/// the pool is collected.</para>
/// </summary>
public sealed class StringPool
{
	private const int StripeCount = 16; // power of two for fast masking
	private const int StripeMask = StripeCount - 1;

	private readonly ConcurrentDictionary<string, string>[] stripes;
	private readonly Action? onGetOrAdd;

	public StringPool()
		: this(onGetOrAdd: null)
	{
	}

	internal StringPool(Action? onGetOrAdd)
	{
		this.onGetOrAdd = onGetOrAdd;
		this.stripes = new ConcurrentDictionary<string, string>[StripeCount];
		for (int i = 0; i < StripeCount; i++)
			this.stripes[i] = new(StringComparer.Ordinal);
	}

	/// <summary>
	/// Returns the canonical instance of <paramref name="value"/>. If an equal string
	/// is already in the pool, the pooled instance is returned and <paramref name="value"/>
	/// becomes eligible for GC. Otherwise <paramref name="value"/> is added to the pool
	/// and returned as-is.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetOrAdd(string value)
	{
		this.onGetOrAdd?.Invoke();
		int stripe = value.GetHashCode() & StripeMask;
		return this.stripes[stripe].GetOrAdd(value, static v => v);
	}
}
