// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Templates.EditorConfig.FileGenerator
{
    internal static class TemplateConstants
    {
        public const string FileName = ".editorconfig";
        public const string DefaultFileContent = "[*]\r\nend_of_line = crlf\r\n\r\n[*.xml]\r\nindent_style = space";
    }
}
