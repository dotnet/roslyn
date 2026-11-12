// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public sealed class CSharpPreviewLanguageFeaturesIntegrationTest_Legacy : IntegrationTestBase
{
    private const string LegacyTemplateBaseSource =
        """
        public abstract class LegacyTemplateBase
        {
            public virtual System.Threading.Tasks.Task ExecuteAsync()
                => System.Threading.Tasks.Task.CompletedTask;

            protected void WriteLiteral(string value)
            {
            }

            protected void Write(object value)
            {
            }

            protected TTagHelper CreateTagHelper<TTagHelper>()
                where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper
                => System.Activator.CreateInstance<TTagHelper>();

            protected void StartTagHelperWritingScope(System.Text.Encodings.Web.HtmlEncoder encoder)
            {
            }

            protected Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent EndTagHelperWritingScope()
                => throw new System.NotImplementedException();

            protected void BeginWriteTagHelperAttribute()
            {
            }

            protected string EndWriteTagHelperAttribute()
                => string.Empty;
        }
        """;

    public CSharpPreviewLanguageFeaturesIntegrationTest_Legacy()
        : base(layer: TestProject.Layer.Compiler)
    {
        AddCSharpSyntaxTree(LegacyTemplateBaseSource, filePath: "LegacyTemplateBase.cs");
    }

    public override string GetTestFileName([CallerMemberName] string? testName = null)
    {
        var fileName = $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";
        var directory = Path.GetDirectoryName(fileName);
        if (directory is not null)
        {
            Directory.CreateDirectory(Path.Combine(TestProjectRoot, directory));
        }

        return fileName;
    }
}
