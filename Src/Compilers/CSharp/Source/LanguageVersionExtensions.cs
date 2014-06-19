namespace Roslyn.Compilers.CSharp
{
    internal static partial class LanguageVersionExtensions
    {
        internal static bool IsValid(this LanguageVersion value)
        {
            return value >= LanguageVersion.CSharp1 && value <= LanguageVersion.CSharp6;
        }
    }
}