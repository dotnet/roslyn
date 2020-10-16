﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class CustomObsoleteDiagnosticInfo : DiagnosticInfo
    {
        private DiagnosticDescriptor? _descriptor;
        internal ObsoleteAttributeData Data { get; }

        internal CustomObsoleteDiagnosticInfo(CommonMessageProvider messageProvider, int errorCode, ObsoleteAttributeData data, params object[] arguments)
            : base(messageProvider, errorCode, arguments)
        {
            Data = data;
        }

        private CustomObsoleteDiagnosticInfo(CustomObsoleteDiagnosticInfo baseInfo, DiagnosticSeverity effectiveSeverity)
            : base(baseInfo, effectiveSeverity)
        {
            Data = baseInfo.Data;
        }

        public override string MessageIdentifier
        {
            get
            {
                var id = Data.DiagnosticId;
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }

                return base.MessageIdentifier;
            }
        }

        public override DiagnosticDescriptor Descriptor
        {
            get
            {
                if (_descriptor == null)
                {
                    Interlocked.CompareExchange(ref _descriptor, CreateDescriptor(), null);
                }

                return _descriptor;
            }
        }

        internal override DiagnosticInfo GetInstanceWithSeverity(DiagnosticSeverity severity)
        {
            return new CustomObsoleteDiagnosticInfo(this, severity);
        }

        private DiagnosticDescriptor CreateDescriptor()
        {
            var baseDescriptor = base.Descriptor;
            var diagnosticId = Data.DiagnosticId;
            var urlFormat = Data.UrlFormat;
            if (diagnosticId is null && urlFormat is null)
            {
                return baseDescriptor;
            }

            var id = MessageIdentifier;
            var helpLinkUri = baseDescriptor.HelpLinkUri;

            if (urlFormat is object)
            {
                try
                {
                    helpLinkUri = string.Format(urlFormat, id);
                }
                catch
                {
                    // if string.Format fails we just want to use the default (non-user specified) URI.
                }
            }

            ImmutableArray<string> customTags;
            if (diagnosticId is null)
            {
                customTags = baseDescriptor.CustomTags.ToImmutableArray();
            }
            else
            {
                var capacity = 1;
                if (baseDescriptor.CustomTags is ICollection<string> { Count: int count })
                {
                    capacity += count;
                }
                var tagsBuilder = ArrayBuilder<string>.GetInstance(capacity);
                tagsBuilder.AddRange(baseDescriptor.CustomTags);
                tagsBuilder.Add(WellKnownDiagnosticTags.CustomObsolete);
                customTags = tagsBuilder.ToImmutableAndFree();
            }

            return new DiagnosticDescriptor(
                id: id,
                title: baseDescriptor.Title,
                messageFormat: baseDescriptor.MessageFormat,
                category: baseDescriptor.Category,
                defaultSeverity: baseDescriptor.DefaultSeverity,
                isEnabledByDefault: baseDescriptor.IsEnabledByDefault,
                description: baseDescriptor.Description,
                helpLinkUri: helpLinkUri,
                customTags: customTags);
        }
    }
}
