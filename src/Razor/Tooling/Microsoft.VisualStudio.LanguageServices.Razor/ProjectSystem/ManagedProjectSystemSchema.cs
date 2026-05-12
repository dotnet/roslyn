// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Razor.ProjectSystem;

// Well-Known Schema and property names defined by the ManagedProjectSystem
internal static class ManagedProjectSystemSchema
{
    public static class ResolvedCompilationReference
    {
        public const string SchemaName = "ResolvedCompilationReference";

        public const string ItemName = "ResolvedCompilationReference";
    }

    public static class ContentItem
    {
        public const string SchemaName = "Content";

        public const string ItemName = "Content";
    }

    public static class NoneItem
    {
        public const string SchemaName = "None";

        public const string ItemName = "None";
    }

    public static class ItemReference
    {
        public const string FullPathPropertyName = "FullPath";

        public const string LinkPropertyName = "Link";
    }
}
