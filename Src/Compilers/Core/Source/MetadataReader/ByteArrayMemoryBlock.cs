using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a memory block backed by an array of bytes.
    /// </summary>
    internal sealed partial class ByteArrayMemoryBlock : AbstractMemoryBlock
    {
        private readonly GCHandle handleToPinnedBytes;
        private readonly ImmutableArray<byte> peImage;

        private static readonly FieldInfo arrayFieldInfo = typeof(ImmutableArray<byte>).GetField("array", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="peImage">The source byte array.</param>
        public ByteArrayMemoryBlock(ImmutableArray<byte> peImage)
        {
            this.peImage = peImage;

            // Use reflection to access the byte[] contained within the ImmutableArray<byte>.
            // While there's a perf hit associdated with reflection, it should be cheaper,
            // in general, than making a copy of the array just for the purposes of pinning.
            Debug.Assert(arrayFieldInfo != null);
            byte[] array = (byte[])arrayFieldInfo.GetValue(peImage);
            this.handleToPinnedBytes = GCHandle.Alloc(array, GCHandleType.Pinned);
        }

        protected override void Dispose(bool disposing)
        {
            handleToPinnedBytes.Free();
        }

        public override IntPtr Pointer
        {
            get
            {
                return handleToPinnedBytes.AddrOfPinnedObject();
            }
        }

        public override int Size
        {
            get
            {
                return peImage.Length;
            }
        }

        public override ImmutableArray<byte> GetContent()
        {
            return peImage;
        }
    }
}