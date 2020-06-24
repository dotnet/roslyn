using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineParamNameHints
{
    /// <summary>
    /// The simple tag that only holds information regarding the associated parameter name
    /// for the argument
    /// </summary>
    class InlineParamNameHintDataTag : ITag
    {
        public readonly string TagName;
        public InlineParamNameHintDataTag(string name)
        {
            TagName = name;
        }
    }
}
