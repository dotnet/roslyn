namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Classification
{
    public static class ClassificationTags
    {
        public static string GetClassificationTypeName(string textTag) => textTag.ToClassificationTypeName();
    }
}
