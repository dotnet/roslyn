// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Errors
{
    internal sealed class CustomObsoleteDiagnosticInfo : DiagnosticInfo
    {
        internal ObsoleteAttributeData Data { get; }

        internal CustomObsoleteDiagnosticInfo(Symbol obsoletedSymbol, ObsoleteAttributeData data, bool inCollectionInitializer)
            : base(CSharp.MessageProvider.Instance, (int)GetErrorCode(data, inCollectionInitializer), arguments: GetArguments(obsoletedSymbol, data))
        {
            Data = data;
        }

        private static ErrorCode GetErrorCode(ObsoleteAttributeData data, bool inCollectionInitializer)
        {
            // dev11 had a bug in this area (i.e. always produce a warning when there's no message) and we have to match it.
            if (data.Message is null)
            {
                return inCollectionInitializer ? ErrorCode.WRN_DeprecatedCollectionInitAdd : ErrorCode.WRN_DeprecatedSymbol;
            }

            return (data.IsError, inCollectionInitializer) switch
            {
                (true, true) => ErrorCode.ERR_DeprecatedCollectionInitAddStr,
                (true, false) => ErrorCode.ERR_DeprecatedSymbolStr,
                (false, true) => ErrorCode.WRN_DeprecatedCollectionInitAddStr,
                (false, false) => ErrorCode.WRN_DeprecatedSymbolStr
            };
        }

        private static object[] GetArguments(Symbol obsoletedSymbol, ObsoleteAttributeData obsoleteAttributeData)
        {
            var message = obsoleteAttributeData.Message;
            if (message is object)
            {
                return new object[] { obsoletedSymbol, message };
            }
            else
            {
                return new object[] { obsoletedSymbol };
            }
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
                        // TODO: should we report a meta-diagnostic of some kind when the string.Format fails?
                        // also, should we do some validation of the 'UrlFormat' values provided in source to prevent people from shipping
                        // obsoleted symbols with malformed 'UrlFormat' values?
                    }
                }

                var customTags = ArrayBuilder<string>.GetInstance();
                foreach (var tag in baseDescriptor.CustomTags)
                {
                    customTags.Add(tag);
                }
                customTags.Add(WellKnownDiagnosticTags.CustomObsolete);

                // TODO: we expect some users to repeatedly use
                // the same diagnostic IDs and url format values for many symbols.
                // do we want to cache similar diagnostic descriptors?
                return new DiagnosticDescriptor(
                    id: id,
                    title: baseDescriptor.Title,
                    messageFormat: baseDescriptor.MessageFormat,
                    category: baseDescriptor.Category,
                    defaultSeverity: baseDescriptor.DefaultSeverity,
                    isEnabledByDefault: baseDescriptor.IsEnabledByDefault,
                    description: baseDescriptor.Description,
                    helpLinkUri: helpLinkUri,
                    customTags: customTags.ToImmutableAndFree());
            }
        }
    }
}
