// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Collections.Generic;

    /// <summary>
    /// Well-known semantic tokens types.
    /// </summary>
    internal static class SemanticTokenTypes
    {
        /// <summary>
        /// Semantic token modifier for 'namespace'.
        /// </summary>
        public const string Namespace = "namespace";

        /// <summary>
        /// Semantic token modifier for 'type'.
        /// </summary>
        public const string Type = "type";

        /// <summary>
        /// Semantic token modifier for 'class'.
        /// </summary>
        public const string Class = "class";

        /// <summary>
        /// Semantic token modifier for 'enum'.
        /// </summary>
        public const string Enum = "enum";

        /// <summary>
        /// Semantic token modifier for 'interface'.
        /// </summary>
        public const string Interface = "interface";

        /// <summary>
        /// Semantic token modifier for 'struct'.
        /// </summary>
        public const string Struct = "struct";

        /// <summary>
        /// Semantic token modifier for 'typeParameter'.
        /// </summary>
        public const string TypeParameter = "typeParameter";

        /// <summary>
        /// Semantic token modifier for 'parameter'.
        /// </summary>
        public const string Parameter = "parameter";

        /// <summary>
        /// Semantic token modifier for 'variable'.
        /// </summary>
        public const string Variable = "variable";

        /// <summary>
        /// Semantic token modifier for 'property'.
        /// </summary>
        public const string Property = "property";

        /// <summary>
        /// Semantic token modifier for 'enumMember'.
        /// </summary>
        public const string EnumMember = "enumMember";

        /// <summary>
        /// Semantic token modifier for 'event'.
        /// </summary>
        public const string Event = "event";

        /// <summary>
        /// Semantic token modifier for 'function'.
        /// </summary>
        public const string Function = "function";

        /// <summary>
        /// Semantic token modifier for 'method'.
        /// </summary>
        public const string Method = "method";

        /// <summary>
        /// Semantic token modifier for 'macro'.
        /// </summary>
        public const string Macro = "macro";

        /// <summary>
        /// Semantic token modifier for 'keyword'.
        /// </summary>
        public const string Keyword = "keyword";

        /// <summary>
        /// Semantic token modifier for 'modifier'.
        /// </summary>
        public const string Modifier = "modifier";

        /// <summary>
        /// Semantic token modifier for 'comment'.
        /// </summary>
        public const string Comment = "comment";

        /// <summary>
        /// Semantic token modifier for 'string'.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Protocol defines this as String")]
        public const string String = "string";

        /// <summary>
        /// Semantic token modifier for 'number'.
        /// </summary>
        public const string Number = "number";

        /// <summary>
        /// Semantic token modifier for 'regexp'.
        /// </summary>
        public const string Regexp = "regexp";

        /// <summary>
        /// Semantic token modifier for 'operator'.
        /// </summary>
        public const string Operator = "operator";

        /// <summary>
        /// Collection containing all well-known semantic tokens types.
        /// </summary>
        public static readonly IReadOnlyList<string> AllTypes = new[]
        {
            Namespace,
            Type,
            Class,
            Enum,
            Interface,
            Struct,
            TypeParameter,
            Parameter,
            Variable,
            Property,
            EnumMember,
            Event,
            Function,
            Method,
            Macro,
            Keyword,
            Modifier,
            Comment,
            String,
            Number,
            Regexp,
            Operator,
        };
    }
}
