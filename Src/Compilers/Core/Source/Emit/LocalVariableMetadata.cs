namespace Microsoft.CodeAnalysis.Emit
{
    public struct LocalVariableMetadata
    {
        public readonly string Name;

        public LocalVariableMetadata(string name)
        {
            this.Name = name;
        }
    }
}
