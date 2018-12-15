// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Shared sinks between InformationDisclosure and XSS.
    /// </summary>
    internal static class WebOutputSinks
    {
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static WebOutputSinks()
        {
            // TODO paulming: Review why InformationDisclosure and XSS sinks are different.
            ImmutableHashSet<SinkInfo>.Builder builder = ImmutableHashSet.CreateBuilder<SinkInfo>();

            SinkKind[] sinkKinds = new SinkKind[] { SinkKind.InformationDisclosure, SinkKind.XSS };

            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIITextControl,
                sinkKinds,
                isInterface: true,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] { "Text" },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebHttpResponseBase,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("Write", new[] { "ch", "obj", "s", "buffer" } ),
                    ("BinaryWrite", new[] { "buffer" } ),
                    ("TransmitFile", new[] { "filename" } ),
                    ("WriteFile", new[] { "filename" } )
                });
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebHttpResponse,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("Write", new[] { "ch", "obj", "s", "buffer" } ),
                    ("BinaryWrite", new[] { "buffer" } ),
                    ("TransmitFile", new[] { "filename" } ),
                    ("WriteFile", new[] { "filename" } )
                });

            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIDesignerDataBoundLiteralControl,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIHtmlControlsHtmlContainerControl,  // Test this covers HtmlSelect, HtmlTable, HtmlTableRow
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "InnerHtml",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIHtmlControlsHtmlTitle,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIHtmlTextWriter,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "AddAttribute", new[] { "value" }),
                });
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUILiteralControl,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIResourceBasedLiteralControl,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUITemplateBuilder,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUITemplateParser,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsBaseDataList,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Caption",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsBaseValidator,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsButton,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsButtonColumn,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsButtonField,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsCalendar,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Caption",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsCheckBox,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                    "TextAlign",    // Test TextAlign, not a string
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsCheckBoxField,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsCheckBoxList,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] { "TextAlign" },   // Test TextAlign, not a string
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsDetailsView,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Caption",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsFormView,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Caption",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsGridView,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Caption",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsHyperLink,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsHyperLinkColumn,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsHyperLinkField,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsImageButton,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsLabel,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsLinkButton,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsListControl,    // Test this covers BulletedList, CheckBoxList, RadioButtonList
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsListItem,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsLiteral,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsLogin,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "TextLayout",   // Test LoginTextLayout, not a string
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsMenuItem,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsMenuItemBinding,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                    "TextField",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsPasswordRecovery,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "TextLayout",   // Test LoginTextLayout, not a string
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsRadioButtonList,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "TextAlign",   // Test TextAlign, not a string
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsRepeatInfo,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Caption",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsTable,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Caption",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsTableCell,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsTextBox,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                    "TextMode",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsTreeNode,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsTreeNodeBinding,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                    "TextField",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogAddVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogCloseVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCloseVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCancelVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCloseVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConfigureVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConnectVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsDisconnectVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartDeleteVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorApplyVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorCancelVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorOKVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartExportVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHeaderCloseVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHelpVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartMinimizeVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartRestoreVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartVerb,
                sinkKinds,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Text",
                },
                sinkMethodParameters: null);

            SinkInfos = builder.ToImmutable();
        }
    }
}
