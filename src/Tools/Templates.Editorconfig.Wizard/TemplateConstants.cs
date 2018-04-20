// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Templates.EditorConfig.FileGenerator
{
    internal static class TemplateConstants
    {
        public const string FileName = ".editorconfig";
        public const string DotNetFileContentIsRoot = @"# To learn more about .editorconfig see https://aka.ms/editorconfigdocs
root = true

# All files
[*]
indent_style = space

# Code files
[*.{cs,csx,vb,vbx}]
indent_size = 4
end_of_line = crlf

# Xml files
[*.xml]
indent_size = 2

# Dotnet code style
[*.{cs,vb}]
# Organize usings
dotnet_sort_system_directives_first = true

# Avoid this. unless absolutely necessary
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion

# Use language keywords instead of BCL types
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion

# Naming conventions
dotnet_naming_style.pascal_case_style.capitalization        = pascal_case
# Classes, structs, methods, enums, events, properties, namespaces, delegates must be PascalCase
dotnet_naming_rule.general_naming.severity                  = suggestion
dotnet_naming_rule.general_naming.symbols                   = general
dotnet_naming_rule.general_naming.style                     = pascal_case_style
dotnet_naming_symbols.general.applicable_kinds              = class,struct,enum,property,method,event,namespace,delegate
dotnet_naming_symbols.general.applicable_accessibilities    = *
";
        public const string DotNetFileContent = @"# To learn more about .editorconfig see https://aka.ms/editorconfigdocs

# All files
[*]
indent_style = space

# Code files
[*.{cs,csx,vb,vbx}]
indent_size = 4
end_of_line = crlf

# Xml files
[*.xml]
indent_size = 2

# Dotnet code style
[*.{cs,vb}]
# Organize usings
dotnet_sort_system_directives_first = true

# Avoid this. unless absolutely necessary
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion

# Use language keywords instead of BCL types
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion

# Naming conventions
dotnet_naming_style.pascal_case_style.capitalization        = pascal_case
# Classes, structs, methods, enums, events, properties, namespaces, delegates must be PascalCase
dotnet_naming_rule.general_naming.severity                  = suggestion
dotnet_naming_rule.general_naming.symbols                   = general
dotnet_naming_rule.general_naming.style                     = pascal_case_style
dotnet_naming_symbols.general.applicable_kinds              = class,struct,enum,property,method,event,namespace,delegate
dotnet_naming_symbols.general.applicable_accessibilities    = *
";
        public const string DefaultFileContentIsRoot = @"# To learn more about .editorconfig see https://aka.ms/editorconfigdocs
root = true

# All files
[*]
indent_style = space

# Xml files
[*.xml]
indent_size = 2
";
        public const string DefaultFileContent = @"# To learn more about .editorconfig see https://aka.ms/editorconfigdocs

# All files
[*]
indent_style = space

# Xml files
[*.xml]
indent_size = 2
";
    }
}
