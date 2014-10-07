namespace Roslyn.Services.Formatting
{
    internal struct TokenWidthInfo
    {
        public bool ContainsLineBreak { get; set; }
        public int Width { get; set; }
    }
}
