// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class WebInputSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for web input tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static WebInputSources()
        {
            ImmutableHashSet<SourceInfo>.Builder sourceInfosBuilder = ImmutableHashSet.CreateBuilder<SourceInfo>();

            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebHttpCookie,
                isInterface: false,
                taintedProperties: new string[] {
                    "Domain",
                    "Name",
                    "Item",
                    "Path",
                    "Value",
                    "Values",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebHttpRequest,
                isInterface: false,
                taintedProperties: new string[] {
                    "AcceptTypes",
                    "AnonymousID",
                    // Anything potentially bad in Browser?
                    "ContentType",
                    "Cookies",
                    "Files",
                    "Form",
                    "Headers",
                    "HttpMethod",
                    "InputStream",
                    "Item",
                    "Params",
                    "Path",
                    "PathInfo",
                    "QueryString",
                    "RawUrl",
                    "RequestType",
                    "Url",
                    "UrlReferrer",
                    "UserAgent",
                    "UserLanguages",
                },
                taintedMethods: new string[] {
                    "BinaryRead",
                    "GetBufferedInputStream",
                    "GetBufferlessInputStream",
                });
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebHttpRequestBase,
                isInterface: false,
                taintedProperties: new string[] {
                    "AcceptTypes",
                    "AnonymousID",
                    // Anything potentially bad in Browser?
                    "ContentType",
                    "Cookies",
                    "Files",
                    "Form",
                    "Headers",
                    "HttpMethod",
                    "InputStream",
                    "Item",
                    "Params",
                    "Path",
                    "PathInfo",
                    "QueryString",
                    "RawUrl",
                    "RequestType",
                    "Url",
                    "UrlReferrer",
                    "UserAgent",
                    "UserLanguages",
                },
                taintedMethods: new string[] {
                    "BinaryRead",
                    "GetBufferedInputStream",
                    "GetBufferlessInputStream",
                });
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebHttpRequestWrapper,
                isInterface: false,
                taintedProperties: new string[] {
                    "AcceptTypes",
                    "AnonymousID",
                    // Anything potentially bad in Browser?
                    "ContentType",
                    "Cookies",
                    "Files",
                    "Form",
                    "Headers",
                    "HttpMethod",
                    "InputStream",
                    "Item",
                    "Params",
                    "Path",
                    "PathInfo",
                    "QueryString",
                    "RawUrl",
                    "RequestType",
                    "Url",
                    "UrlReferrer",
                    "UserAgent",
                    "UserLanguages",
                },
                taintedMethods: new string[] {
                    "BinaryRead",
                    "GetBufferedInputStream",
                    "GetBufferlessInputStream",
                });
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIAdaptersPageAdapter,
                isInterface: false,
                taintedProperties: new string[] {
                    "QueryString",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIDataBoundLiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIDesignerDataBoundLiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIHtmlControlsHtmlInputControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIIndexedString,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value" },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUILiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIResourceBasedLiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text"
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUISimplePropertyEntry,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIStateItem,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIStringPropertyBuilder,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUITemplateBuilder,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUITemplateParser,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsBaseValidator,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsBulletedList,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsButton,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsButtonColumn,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsButtonField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsChangePassword,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsCheckBox,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextAlign",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsCheckBoxField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextAlign",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsCommandEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsCreateUserWizard,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsDataKey,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsDataList,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsDetailsView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsDetailsViewInsertEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsDetailsViewUpdateEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsFormView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsFormViewInsertEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsFormViewUpdateEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsGridView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsHiddenField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsHyperLink,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsHyperLinkColumn,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsHyperLinkField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsImageButton,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsLabel,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsLinkButton,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsListControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsListItem,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsLiteral,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsLogin,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                    "TextLayout",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsMenu,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsMenuItem,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsMenuItemBinding,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextField",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsPasswordRecovery,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                    "TextLayout",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsQueryStringParameter,
                isInterface: false,
                taintedProperties: new string[] {
                    "QueryStringField",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsRadioButtonList,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextAlign",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsServerValidateEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsTableCell,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsTextBox,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsTreeNode,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsTreeNodeBinding,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextField",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsTreeView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsUnit,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsAppearanceEditorPart,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsPersonalizationEntry,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogAddVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCancelVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConfigureVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConnectVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsDisconnectVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartDeleteVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorApplyVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorCancelVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorOKVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartExportVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHeaderCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHelpVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartMinimizeVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartRestoreVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSource(
                WellKnownTypes.SystemWebUIITextControl,
                isInterface: true,
                taintedProperties: new string[] {
                    "Text"
                },
                taintedMethods: null);
            SourceInfos = sourceInfosBuilder.ToImmutable();
        }
    }
}
