namespace Microsoft.CodeAnalysis.AddImports
{
    /// <summary>
    /// Specifies the desired placement of added imports.
    /// </summary>
    internal enum AddImportPlacement
    {
        /// <summary>
        /// Allow imports inside or outside the namespace definition.
        /// </summary>
        Preserve,

        /// <summary>
        /// Place imports inside the namespace definition.
        /// </summary>
        InsideNamespace,

        /// <summary>
        /// Place imports outside the namespace definition.
        /// </summary>
        OutsideNamespace
    }
}
