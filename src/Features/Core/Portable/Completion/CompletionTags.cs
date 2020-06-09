// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// The set of well known tags used for the <see cref="CompletionItem.Tags"/> property.
    /// These tags influence the presentation of items in the list.
    /// </summary>
    [Obsolete("Use Microsoft.CodeAnalysis.Tags.WellKnownTags instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class CompletionTags
    {
        // accessibility
        public const string Public = WellKnownTags.Public;
        public const string Protected = WellKnownTags.Protected;
        public const string Private = WellKnownTags.Private;
        public const string Internal = WellKnownTags.Internal;

        // project elements
        public const string File = WellKnownTags.File;
        public const string Project = WellKnownTags.Project;
        public const string Folder = WellKnownTags.Folder;
        public const string Assembly = WellKnownTags.Assembly;

        // language elements
        public const string Class = WellKnownTags.Class;
        public const string Constant = WellKnownTags.Constant;
        public const string Delegate = WellKnownTags.Delegate;
        public const string Enum = WellKnownTags.Enum;
        public const string EnumMember = WellKnownTags.EnumMember;
        public const string Event = WellKnownTags.Event;
        public const string ExtensionMethod = WellKnownTags.ExtensionMethod;
        public const string Field = WellKnownTags.Field;
        public const string Interface = WellKnownTags.Interface;
        public const string Intrinsic = WellKnownTags.Intrinsic;
        public const string Keyword = WellKnownTags.Keyword;
        public const string Label = WellKnownTags.Label;
        public const string Local = WellKnownTags.Local;
        public const string Namespace = WellKnownTags.Namespace;
        public const string Method = WellKnownTags.Method;
        public const string Module = WellKnownTags.Module;
        public const string Operator = WellKnownTags.Operator;
        public const string Parameter = WellKnownTags.Parameter;
        public const string Property = WellKnownTags.Property;
        public const string RangeVariable = WellKnownTags.RangeVariable;
        public const string Reference = WellKnownTags.Reference;
        public const string Structure = WellKnownTags.Structure;
        public const string TypeParameter = WellKnownTags.TypeParameter;

        // other
        public const string Snippet = WellKnownTags.Snippet;
        public const string Error = WellKnownTags.Error;
        public const string Warning = WellKnownTags.Warning;
        internal const string StatusInformation = WellKnownTags.StatusInformation;
    }
}
