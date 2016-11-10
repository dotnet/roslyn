Generated from mscorlib 2.0 using RefAsmGen:

refasmgen /contracts+ mscorlib.dll /o:Contracts\mscorlib.dll

and then clearing the CorFlags.Requires32Bit big in the COR header flag to change the architecture to AnyCPU.

Script to clear the flag:

#r "System.Reflection.Metadata.1.0.19-rc.nupkg"

using (var stream = new FileStream(args[0], FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
using (var peReader = new PEReader(stream))
using (var writer = new BinaryWriter(stream))
{
    const int OffsetFromStartOfCorHeaderToFlags =
	    sizeof(int)    // byte count
	  + sizeof(short)  // major version
	  + sizeof(short)  // minor version
	  + sizeof(long);  // metadata directory

	stream.Position = peReader.PEHeaders.CorHeaderStartOffset + OffsetFromStartOfCorHeaderToFlags;

	var flags = peReader.PEHeaders.CorHeader.Flags;
	Console.WriteLine($"Current flags: {flags}");
	flags &= ~CorFlags.Requires32Bit;
	Console.WriteLine($"New flags: {flags}");
	writer.Write((uint)flags);
}