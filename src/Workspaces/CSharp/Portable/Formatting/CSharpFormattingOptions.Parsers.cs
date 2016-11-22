// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    public static partial class CSharpFormattingOptions
    {
        internal static bool DetermineIfSpaceOptionIsSet(string value, SpacingWithinParenthesesOption parenthesesSpacingOption)
            => (from v in value.Split(',').Select(v => v.Trim())
                let option = ConvertToSpacingOption(v)
                where option.HasValue && option.Value == parenthesesSpacingOption
                select option)
                .Any();

        private static SpacingWithinParenthesesOption? ConvertToSpacingOption(string value)
        {
            switch (value)
            {
                case "expressions": return SpacingWithinParenthesesOption.Expressions;
                case "type_casts": return SpacingWithinParenthesesOption.TypeCasts;
                case "control_flow_statements": return SpacingWithinParenthesesOption.ControlFlowStatements;
                default: return null;
            }
        }

        internal static BinaryOperatorSpacingOptions ParseEditorConfigSpacingAroundBinaryOperator(string binaryOperatorSpacingValue)
        {
            switch (binaryOperatorSpacingValue)
            {
                case "ignore": return BinaryOperatorSpacingOptions.Ignore;
                case "none": return BinaryOperatorSpacingOptions.Remove;
                case "before_and_after": return BinaryOperatorSpacingOptions.Single;
                default: return BinaryOperatorSpacingOptions.Single;
            }
        }

        internal static LabelPositionOptions ParseEditorConfigLablePositioning(string lableIndentationValue)
        {
            switch (lableIndentationValue)
            {
                case "flush_left": return LabelPositionOptions.LeftMost;
                case "no_change": return LabelPositionOptions.NoIndent;
                case "one_less_than_current": return LabelPositionOptions.OneLess;
                default: return LabelPositionOptions.NoIndent;
            }
        }

        internal static bool DetermineIfNewLineOptionIsSet(string value, NewLineOption optionName)
        {
            var values = value.Split(',');

            if (values.Any(s => s.Trim() == "all"))
            {
                return true;
            }

            if (values.Any(s => s.Trim() == "none"))
            {
                return false;
            }

            return (from v in values
                    let option = ConvertToNewLineOption(v)
                    where option.HasValue && option.Value == optionName
                    select option)
                    .Any();
        }


        private static NewLineOption? ConvertToNewLineOption(string value)
        {
            switch (value.Trim())
            {
                case "accessors": return NewLineOption.Accessors;
                case "types": return NewLineOption.Types;
                case "methods": return NewLineOption.Methods;
                case "properties": return NewLineOption.Properties;
                case "indexers": return NewLineOption.Indexers;
                case "events": return NewLineOption.Events;
                case "anonymous_methods": return NewLineOption.AnonymousMethods;
                case "control_blocks": return NewLineOption.ControlBlocks;
                case "anonymous_types": return NewLineOption.AnonymousTypes;
                case "object_collection_array_initalizers": return NewLineOption.ObjectCollectionsArrayInitializers;
                case "lambdas": return NewLineOption.Lambdas;
                case "local_functions": return NewLineOption.LocalFunction;
                default: return null;
            }
        }

        internal static bool DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(string value)
            => value.Trim() == "ignore";

        internal enum SpacingWithinParenthesesOption
        {
            Expressions,
            TypeCasts,
            ControlFlowStatements
        }

        internal enum NewLineOption
        {
            Types,
            Methods,
            Properties,
            Indexers,
            Events,
            AnonymousMethods,
            ControlBlocks,
            AnonymousTypes,
            ObjectCollectionsArrayInitializers,
            Lambdas,
            LocalFunction,
            Accessors
        }
    }
}
