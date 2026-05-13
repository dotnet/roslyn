// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class TagHelperTestData
{
    public static string Project1FilePath { get; }
    public static ProjectId Project1Id { get; }
    public static TagHelperDescriptor TagHelper1_Project1 { get; }
    public static TagHelperDescriptor TagHelper2_Project1 { get; }
    public static ImmutableArray<TagHelperDescriptor> Project1TagHelpers { get; }
    public static ImmutableArray<Checksum> Project1TagHelperChecksums { get; }

    public static string Project2FilePath { get; }
    public static ProjectId Project2Id { get; }
    public static TagHelperDescriptor TagHelper1_Project2 { get; }
    public static TagHelperDescriptor TagHelper2_Project2 { get; }
    public static ImmutableArray<TagHelperDescriptor> Project2TagHelpers { get; }
    public static ImmutableArray<Checksum> Project2TagHelperChecksums { get; }
    public static ImmutableArray<TagHelperDescriptor> Project1AndProject2TagHelpers { get; }
    public static ImmutableArray<Checksum> Project1AndProject2TagHelperChecksums { get; }

    static TagHelperTestData()
    {
        Project1FilePath = "C:/path/to/Project1/Project1.csproj";
        Project1Id = ProjectId.CreateNewId();
        TagHelper1_Project1 = TagHelperDescriptorBuilder.CreateTagHelper("TagHelper1", "Project1").Build();
        TagHelper2_Project1 = TagHelperDescriptorBuilder.CreateTagHelper("TagHelper2", "Project1").Build();
        Project1TagHelpers = ImmutableArray.Create(TagHelper1_Project1, TagHelper2_Project1);
        Project1TagHelperChecksums = Project1TagHelpers.SelectAsArray(t => t.Checksum);

        Project2FilePath = "C:/path/to/Project2/Project2.csproj";
        Project2Id = ProjectId.CreateNewId();
        TagHelper1_Project2 = TagHelperDescriptorBuilder.CreateTagHelper("TagHelper1", "Project2").Build();
        TagHelper2_Project2 = TagHelperDescriptorBuilder.CreateTagHelper("TagHelper2", "Project2").Build();
        Project2TagHelpers = ImmutableArray.Create(TagHelper1_Project2, TagHelper2_Project2);
        Project2TagHelperChecksums = Project2TagHelpers.SelectAsArray(t => t.Checksum);

        Project1AndProject2TagHelpers = ImmutableArray.Create(TagHelper1_Project1, TagHelper2_Project1, TagHelper1_Project2, TagHelper2_Project2);
        Project1AndProject2TagHelperChecksums = Project1AndProject2TagHelpers.SelectAsArray(t => t.Checksum);
    }
}
