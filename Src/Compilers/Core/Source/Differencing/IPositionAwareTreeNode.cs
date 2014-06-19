namespace Microsoft.CodeAnalysis.Differencing
{
    internal interface IPositionAwareTreeNode
    {
        int Position { get; }
    }
}
