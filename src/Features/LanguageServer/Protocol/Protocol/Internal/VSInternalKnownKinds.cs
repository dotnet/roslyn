// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Collections.Generic;

    /// <summary>
    /// Known VS response kinds.
    /// </summary>
    internal static class VSInternalKnownKinds
    {
        /// <summary>
        /// Response kind string for 'text'.
        /// </summary>
        public const string Text = "text";

        /// <summary>
        /// Response kind string for 'method'.
        /// </summary>
        public const string Method = "method";

        /// <summary>
        /// Response kind string for 'function'.
        /// </summary>
        public const string Function = "function";

        /// <summary>
        /// Response kind string for 'constructor'.
        /// </summary>
        public const string Constructor = "constructor";

        /// <summary>
        /// Response kind string for 'field'.
        /// </summary>
        public const string Field = "field";

        /// <summary>
        /// Response kind string for 'variable'.
        /// </summary>
        public const string Variable = "variable";

        /// <summary>
        /// Response kind string for 'class'.
        /// </summary>
        public const string Class = "class";

        /// <summary>
        /// Response kind string for 'interface'.
        /// </summary>
        public const string Interface = "interface";

        /// <summary>
        /// Response kind string for 'module'.
        /// </summary>
        public const string Module = "module";

        /// <summary>
        /// Response kind string for 'property'.
        /// </summary>
        public const string Property = "property";

        /// <summary>
        /// Response kind string for 'unit'.
        /// </summary>
        public const string Unit = "unit";

        /// <summary>
        /// Response kind string for 'value'.
        /// </summary>
        public const string Value = "value";

        /// <summary>
        /// Response kind string for 'enum'.
        /// </summary>
        public const string Enum = "enum";

        /// <summary>
        /// Response kind string for 'keyword'.
        /// </summary>
        public const string Keyword = "keyword";

        /// <summary>
        /// Response kind string for 'snippet'.
        /// </summary>
        public const string Snippet = "snippet";

        /// <summary>
        /// Response kind string for 'color'.
        /// </summary>
        public const string Color = "color";

        /// <summary>
        /// Response kind string for 'file'.
        /// </summary>
        public const string File = "file";

        /// <summary>
        /// Response kind string for 'reference'.
        /// </summary>
        public const string Reference = "reference";

        /// <summary>
        /// Response kind string for 'folder'.
        /// </summary>
        public const string Folder = "folder";

        /// <summary>
        /// Response kind string for 'enumMember'.
        /// </summary>
        public const string EnumMember = "enumMember";

        /// <summary>
        /// Response kind string for 'constant'.
        /// </summary>
        public const string Constant = "constant";

        /// <summary>
        /// Response kind string for 'struct'.
        /// </summary>
        public const string Struct = "struct";

        /// <summary>
        /// Response kind string for 'event'.
        /// </summary>
        public const string Event = "event";

        /// <summary>
        /// Response kind string for 'operator'.
        /// </summary>
        public const string Operator = "operator";

        /// <summary>
        /// Response kind string for 'typeParameter'.
        /// </summary>
        public const string TypeParameter = "typeParameter";

        /// <summary>
        /// Response kind string for 'namespace'.
        /// </summary>
        public const string Namespace = "namespace";

        /// <summary>
        /// Response kind string for 'package'.
        /// </summary>
        public const string Package = "package";

        /// <summary>
        /// Response kind string for 'string'.
        /// </summary>
        public const string StringKind = "string";

        /// <summary>
        /// Response kind string for 'number'.
        /// </summary>
        public const string Number = "number";

        /// <summary>
        /// Response kind string for 'boolean'.
        /// </summary>
        public const string Boolean = "boolean";

        /// <summary>
        /// Response kind string for 'array'.
        /// </summary>
        public const string Array = "array";

        /// <summary>
        /// Response kind string for 'object'.
        /// </summary>
        public const string ObjectKind = "object";

        /// <summary>
        /// Response kind string for 'key'.
        /// </summary>
        public const string Key = "key";

        /// <summary>
        /// Response kind string for 'null'.
        /// </summary>
        public const string Null = "null";

        /// <summary>
        /// Response kind string for 'macro'.
        /// </summary>
        public const string Macro = "macro";

        /// <summary>
        /// Response kind string for 'template'.
        /// </summary>
        public const string Template = "template";

        /// <summary>
        /// Response kind string for 'typedef'.
        /// </summary>
        public const string Typedef = "typedef";

        /// <summary>
        /// Response kind string for 'union'.
        /// </summary>
        public const string Union = "union";

        /// <summary>
        /// Response kind string for 'delegate'.
        /// </summary>
        public const string Delegate = "delegate";

        /// <summary>
        /// Response kind string for 'tag'.
        /// </summary>
        public const string Tag = "tag";

        /// <summary>
        /// Response kind string for 'attribute'.
        /// </summary>
        public const string Attribute = "attribute";

        /// <summary>
        /// Collection of response kind strings.
        /// </summary>
        public static readonly IReadOnlyCollection<string> AllKinds = new[]
        {
            Text,
            Method,
            Function,
            Constructor,
            Field,
            Variable,
            Class,
            Interface,
            Module,
            Property,
            Unit,
            Value,
            Enum,
            Keyword,
            Snippet,
            Color,
            File,
            Reference,
            Folder,
            EnumMember,
            Constant,
            Struct,
            Event,
            Operator,
            TypeParameter,
            Namespace,
            Package,
            StringKind,
            Number,
            Boolean,
            Array,
            ObjectKind,
            Key,
            Null,
            Macro,
            Template,
            Typedef,
            Union,
            Delegate,
            Tag,
            Attribute,
        };
    }
}
