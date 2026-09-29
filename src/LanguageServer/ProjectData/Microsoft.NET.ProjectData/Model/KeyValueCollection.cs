// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Optimized key-value collection that exploits the regular key structure of
/// project data. Keys are defined by a shared <see cref="KeySchema"/>, and values
/// are stored as a flat array of strings indexed by the schema. This avoids
/// dictionary overhead for what is essentially a fixed set of known keys.
/// </summary>
public readonly struct KeyValueCollection
{
	private readonly KeySchema schema;
	private readonly ImmutableArray<string?> values;

	public KeyValueCollection(KeySchema schema, ImmutableArray<string?> values)
	{
		Debug.Assert(schema is not null, "KeyValueCollection must be constructed with a non-null schema.");
		Debug.Assert(!values.IsDefault, "KeyValueCollection must be constructed with a non-default values array.");
		Debug.Assert(values.Length <= schema.Count, $"Values array length ({values.Length}) exceeds schema key count ({schema.Count}).");

		this.schema = schema;
		this.values = values;
	}

	public static KeyValueCollection Empty { get; } = new(new KeySchema([]), []);

	/// <summary>
	/// Gets the schema that defines the key layout.
	/// </summary>
	public KeySchema Schema => this.schema;

	/// <summary>
	/// Gets whether this collection has no values (was constructed with an empty values array).
	/// </summary>
	public bool IsEmpty => this.values.IsEmpty;

	/// <summary>
	/// Tries to get the value for a given key.
	/// Returns <see langword="true"/> if the key exists and has a non-null value.
	/// </summary>
	public bool TryGetValue(string key, out string? value)
	{
		Debug.Assert(this.schema is not null, "KeyValueCollection used before initialization.");

		if (this.schema.TryGetIndex(key, out int index) && index < this.values.Length)
		{
			value = this.values[index];
			return value is not null;
		}

		value = null;
		return false;
	}

	/// <summary>
	/// Gets the value for a given key, or <see langword="null"/> if not present.
	/// </summary>
	public string? this[string key]
	{
		get
		{
			this.TryGetValue(key, out string? value);
			return value;
		}
	}

	/// <summary>
	/// Enumerates all non-null key-value pairs.
	/// </summary>
	public Enumerator GetEnumerator() => new(this);

	/// <summary>
	/// Converts to a dictionary for serialization across process boundaries.
	/// Only includes non-null values.
	/// </summary>
	public Dictionary<string, string> ToDictionary()
	{
		Debug.Assert(this.schema is not null, "KeyValueCollection used before initialization.");

		Dictionary<string, string> dict = new(StringComparers.PropertyName);

		if (!this.values.IsEmpty)
		{
			for (int i = 0; i < this.values.Length; i++)
			{
				if (this.values[i] is string value)
				{
					dict[this.schema.GetKey(i)] = value;
				}
			}
		}

		return dict;
	}

	public struct Enumerator
	{
		private readonly KeyValueCollection collection;
		private int index;

		internal Enumerator(KeyValueCollection collection)
		{
			this.collection = collection;
			this.index = -1;
		}

		public KeyValuePair<string, string> Current
		{
			get
			{
				string key = this.collection.schema.GetKey(this.index);
				string value = this.collection.values[this.index]!;
				return new(key, value);
			}
		}

		public bool MoveNext()
		{
			if (this.collection.values.IsEmpty)
				return false;

			// values.Length <= schema.Count is enforced by the constructor.
			while (++this.index < this.collection.values.Length)
			{
				if (this.collection.values[this.index] is not null)
					return true;
			}
			return false;
		}
	}
}
