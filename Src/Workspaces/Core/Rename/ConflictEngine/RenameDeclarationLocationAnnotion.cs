using System;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// This annotation will be placed on a token that represents the name of a declared symbol.
    /// E.g. this annotation will be put on each token starting at the positions from Symbol.Locations.
    /// </summary>
    [Serializable]
    internal class RenameDeclarationLocationAnnotion : RenameAnnotation
    {
        public RenameDeclarationLocationAnnotion(long sessionId)
            : base(sessionId)
        {
        }
    }
}
