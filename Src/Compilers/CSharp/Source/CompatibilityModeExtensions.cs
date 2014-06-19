namespace Roslyn.Compilers.CSharp
{
    internal static partial class CompatibilityModeExtensions
    {
        internal static bool IsValid(this CompatibilityMode value)
        {
            return value >= CompatibilityMode.None && value <= CompatibilityMode.ECMA2;
        }
    }
}