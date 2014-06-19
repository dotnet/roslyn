namespace Roslyn.Compilers.CodeGen
{
    partial class PrivateImplementationDetails
    {
        internal static string SynthesizedStringHashFunctionName
        {
            get
            {
                return "$$method0x6000001-ComputeStringHash";
            }
        }

        internal bool HasSynthesizedStringHashFunction()
        {
            return this.GetMethod(SynthesizedStringHashFunctionName) != null;
        }
    }
}