// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Frozen;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Maps string keys to array indices for O(1) lookup in <see cref="KeyValueCollection"/>.
/// Instances are shared across all snapshots, so keys are defined once and reused.
/// </summary>
public sealed class KeySchema
{
	private readonly FrozenDictionary<string, int> keyToIndex;
	private readonly string[] indexToKey;

	public KeySchema(IReadOnlyList<string> keys)
		: this(keys, StringComparers.PropertyName)
	{
	}

	public KeySchema(IReadOnlyList<string> keys, StringComparer comparer)
	{
		this.indexToKey = new string[keys.Count];
		Dictionary<string, int> map = new(keys.Count, comparer);

		for (int i = 0; i < keys.Count; i++)
		{
			this.indexToKey[i] = keys[i];
			map[keys[i]] = i;
		}

		this.keyToIndex = map.ToFrozenDictionary(comparer);
	}

	/// <summary>
	/// Gets the number of keys in this schema.
	/// </summary>
	public int Count => this.indexToKey.Length;

	/// <summary>
	/// Gets the key name at the specified index.
	/// </summary>
	public string GetKey(int index) => this.indexToKey[index];

	/// <summary>
	/// Tries to get the index for a given key name.
	/// </summary>
	public bool TryGetIndex(string key, out int index) => this.keyToIndex.TryGetValue(key, out index);

	/// <summary>
	/// Gets the index for a given key name, or -1 if not found.
	/// </summary>
	public int GetIndexOrNegativeOne(string key) => this.keyToIndex.TryGetValue(key, out int index) ? index : -1;
}
