// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Text;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// A <see cref="TextWriter"/> that renders cache content into pooled <see cref="char"/> and
/// <see cref="byte"/> buffers, normalizing line endings in place, so the writer never allocates
/// the candidate as a <see cref="string"/> or a heap <see cref="byte"/> array on the common
/// path. The candidate string is produced lazily via <see cref="GetText"/> only when a newer
/// minor version's data has to be spliced in. Always use with <c>using</c> so both pooled
/// buffers are returned.
/// </summary>
internal sealed class PooledCacheRender : TextWriter
{
	private static readonly char[] NewLineChars = { '\n' };

	private char[] _chars;
	private int _charLength;
	private byte[]? _bytes;
	private int _byteLength;

	private PooledCacheRender(int initialCapacity)
	{
		this._chars = ArrayPool<char>.Shared.Rent(initialCapacity);
		this.CoreNewLine = NewLineChars;
	}

	public override Encoding Encoding => ProjectDataWriter.Utf8NoBom;

	public byte[] Bytes => this._bytes!;

	public int ByteLength => this._byteLength;

	public static PooledCacheRender Create(Action<TextWriter> writeContent)
	{
		var render = new PooledCacheRender(initialCapacity: 4096);
		try
		{
			writeContent(render);
			render.Finish();
			return render;
		}
		catch
		{
			render.Dispose();
			throw;
		}
	}

	/// <summary>Materializes the rendered content as a string (rare splice path only).</summary>
	public string GetText() => new string(this._chars, 0, this._charLength);

	public override void Write(char value)
	{
		this.EnsureCapacity(this._charLength + 1);
		this._chars[this._charLength++] = value;
	}

	public override void Write(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return;

		this.EnsureCapacity(this._charLength + value!.Length);
		value.CopyTo(0, this._chars, this._charLength, value.Length);
		this._charLength += value.Length;
	}

	public override void Write(char[] buffer, int index, int count)
	{
		if (count <= 0)
			return;

		this.EnsureCapacity(this._charLength + count);
		Array.Copy(buffer, index, this._chars, this._charLength, count);
		this._charLength += count;
	}

	/// <summary>
	/// Normalizes line endings in <paramref name="buffer"/> in place (<c>\r\n</c> and lone
	/// <c>\r</c> both collapse to <c>\n</c>), returning the new logical length. Mirrors
	/// <c>ProjectDataWriter.NormalizeLineEndings(string)</c> exactly but without allocating.
	/// </summary>
	private static int NormalizeNewLinesInPlace(char[] buffer, int length)
	{
		int firstCr = Array.IndexOf(buffer, '\r', 0, length);
		if (firstCr < 0)
			return length;

		int write = firstCr;
		for (int read = firstCr; read < length; read++)
		{
			char c = buffer[read];
			if (c == '\r')
			{
				buffer[write++] = '\n';
				if (read + 1 < length && buffer[read + 1] == '\n')
					read++; // collapse the "\n" half of a "\r\n" pair
			}
			else
			{
				buffer[write++] = c;
			}
		}

		return write;
	}

	private void Finish()
	{
		this._charLength = NormalizeNewLinesInPlace(this._chars, this._charLength);
		int byteCount = ProjectDataWriter.Utf8NoBom.GetByteCount(this._chars, 0, this._charLength);
		this._bytes = ArrayPool<byte>.Shared.Rent(byteCount);
		this._byteLength = ProjectDataWriter.Utf8NoBom.GetBytes(this._chars, 0, this._charLength, this._bytes, 0);
	}

	private void EnsureCapacity(int required)
	{
		if (required <= this._chars.Length)
			return;

		int newSize = Math.Max(required, this._chars.Length * 2);
		char[] larger = ArrayPool<char>.Shared.Rent(newSize);
		Array.Copy(this._chars, 0, larger, 0, this._charLength);
		ArrayPool<char>.Shared.Return(this._chars);
		this._chars = larger;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ArrayPool<char>.Shared.Return(this._chars);
			if (this._bytes != null)
				ArrayPool<byte>.Shared.Return(this._bytes);
		}

		base.Dispose(disposing);
	}
}
