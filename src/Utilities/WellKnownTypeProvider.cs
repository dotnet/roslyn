// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Provides and caches well known types in a compilation for <see cref="DataFlowAnalysis"/>.
    /// </summary>
    internal class WellKnownTypeProvider
    {
        private static readonly ConditionalWeakTable<Compilation, WellKnownTypeProvider> s_providerCache =
            new ConditionalWeakTable<Compilation, WellKnownTypeProvider>();
        private static readonly ConditionalWeakTable<Compilation, WellKnownTypeProvider>.CreateValueCallback s_ProviderCacheCallback =
            new ConditionalWeakTable<Compilation, WellKnownTypeProvider>.CreateValueCallback(compilation => new WellKnownTypeProvider(compilation));

        private WellKnownTypeProvider(Compilation compilation)
        {
            Compilation = compilation;
            TypeToFullName = new Dictionary<ISymbol, string>();
            FullNameToType = new Dictionary<string, INamedTypeSymbol>();

            Exception = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemException);
            Contract = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemDiagnosticContractsContract);
            IDisposable = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemIDisposable);
            Monitor = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemThreadingMonitor);
            Task = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemThreadingTasksTask);
            CollectionTypes = GetWellKnownCollectionTypes(compilation);
            SerializationInfo = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemRuntimeSerializationSerializationInfo);
            GenericIEquatable = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemIEquatable1);
            HttpRequest = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebHttpRequest);
            IDbCommand = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemDataIDbCommand);
            WebControlsSqlDataSource = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsSqlDataSource);
            Boolean = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemBoolean);
            Byte = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemByte);
            Char = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemChar);
            DateTime = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemDateTime);
            Decimal = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemDecimal);
            Double = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemDouble);
            TimeSpanParse = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemGlobalizationTimeSpanParse);
            Guid = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemGuid);
            Int16 = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemInt16);
            Int32 = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemInt32);
            Int64 = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemInt64);
            Number = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemNumber);
            Single = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemSingle);
            TimeSpan = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemTimeSpan);
            HttpCookie = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebHttpCookie);
            HttpRequestBase = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebHttpRequestBase);
            HttpRequestWrapper = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebHttpRequestWrapper);
            PageAdapter = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIAdaptersPageAdapter);
            DataBoundLiteralControl = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIDataBoundLiteralControl);
            DesignerDataBoundLiteralControl = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIDesignerDataBoundLiteralControl);
            HtmlInputControl = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIHtmlControlsHtmlInputControl);
            HtmlInputFile = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIHtmlControlsHtmlInputFile);
            HtmlInputRadioButton = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIHtmlControlsHtmlInputRadioButton);
            HtmlInputText = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIHtmlControlsHtmlInputText);
            HtmlSelect = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIHtmlControlsHtmlSelect);
            HtmlTextArea = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIHtmlControlsHtmlTextArea);
            HtmlTitle = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIHtmlControlsHtmlTitle);
            IndexedString = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIIndexedString);
            LiteralControl = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUILiteralControl);
            ResourceBasedLiteralControl = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIResourceBasedLiteralControl);
            SimplePropertyEntry = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUISimplePropertyEntry);
            StateItem = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIStateItem);
            StringPropertyBuilder = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIStringPropertyBuilder);
            TemplateBuilder = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUITemplateBuilder);
            TemplateParser = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUITemplateParser);
            BaseValidator = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsBaseValidator);
            BulletedList = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsBulletedList);
            Button = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsButton);
            ButtonColumn = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsButtonColumn);
            ButtonField = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsButtonField);
            ChangePassword = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsChangePassword);
            CheckBox = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsCheckBox);
            CheckBoxField = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsCheckBoxField);
            CheckBoxList = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsCheckBoxList);
            CommandEventArgs = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsCommandEventArgs);
            CreateUserWizard = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsCreateUserWizard);
            DataKey = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsDataKey);
            DataList = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsDataList);
            DetailsView = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsDetailsView);
            DetailsViewInsertEventArgs = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsDetailsViewInsertEventArgs);
            DetailsViewUpdateEventArgs = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsDetailsViewUpdateEventArgs);
            FormView = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsFormView);
            FormViewInsertEventArgs = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsFormViewInsertEventArgs);
            FormViewUpdateEventArgs = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsFormViewUpdateEventArgs);
            GridView = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsGridView);
            HiddenField = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsHiddenField);
            HyperLink = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsHyperLink);
            HyperLinkColumn = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsHyperLinkColumn);
            HyperLinkField = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsHyperLinkField);
            ImageButton = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsImageButton);
            Label = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsLabel);
            LinkButton = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsLinkButton);
            ListControl = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsListControl);
            ListItem = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsListItem);
            Literal = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsLiteral);
            Login = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsLogin);
            Menu = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsMenu);
            MenuItem = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsMenuItem);
            MenuItemBinding = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsMenuItemBinding);
            PasswordRecovery = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsPasswordRecovery);
            QueryStringParameter = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsQueryStringParameter);
            RadioButtonList = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsRadioButtonList);
            ServerValidateEventArgs = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsServerValidateEventArgs);
            TableCell = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsTableCell);
            TextBox = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsTextBox);
            TreeNode = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsTreeNode);
            TreeNodeBinding = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsTreeNodeBinding);
            TreeView = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsTreeView);
            Unit = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsUnit);
            AppearanceEditorPart = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsAppearanceEditorPart);
            PersonalizationEntry = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsPersonalizationEntry);
            WebPartCatalogAddVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogAddVerb);
            WebPartCatalogCloseVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogCloseVerb);
            WebPartCloseVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCloseVerb);
            WebPartConnectionsCancelVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCancelVerb);
            WebPartConnectionsCloseVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCloseVerb);
            WebPartConnectionsConfigureVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConfigureVerb);
            WebPartConnectionsConnectVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConnectVerb);
            WebPartConnectionsDisconnectVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsDisconnectVerb);
            WebPartConnectVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectVerb);
            WebPartDeleteVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartDeleteVerb);
            WebPartEditorApplyVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorApplyVerb);
            WebPartEditorCancelVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorCancelVerb);
            WebPartEditorOKVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorOKVerb);
            WebPartEditVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditVerb);
            WebPartExportVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartExportVerb);
            WebPartHeaderCloseVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHeaderCloseVerb);
            WebPartHelpVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHelpVerb);
            WebPartMinimizeVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartMinimizeVerb);
            WebPartRestoreVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartRestoreVerb);
            WebPartVerb = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartVerb);
        }

        public static WellKnownTypeProvider GetOrCreate(Compilation compilation) => s_providerCache.GetValue(compilation, s_ProviderCacheCallback);

        public Compilation Compilation { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Exception"/>
        /// </summary>
        public INamedTypeSymbol Exception { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Diagnostics.Contracts.Contract"/>
        /// </summary>
        public INamedTypeSymbol Contract { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.IDisposable"/>
        /// </summary>
        public INamedTypeSymbol IDisposable { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Tasks.Task"/>
        /// </summary>
        public INamedTypeSymbol Task { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Monitor"/>
        /// </summary>
        public INamedTypeSymbol Monitor { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Runtime.Serialization.SerializationInfo"/>
        /// </summary>
        public INamedTypeSymbol SerializationInfo { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.IEquatable{T}"/>
        /// </summary>
        public INamedTypeSymbol GenericIEquatable { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Web.HttpRequest"/>
        /// </summary>
        public INamedTypeSymbol HttpRequest { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Data.IDbCommand"/>
        /// </summary>
        public INamedTypeSymbol IDbCommand { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Web.UI.WebControls.SqlDataSource"/>
        /// </summary>
        public INamedTypeSymbol WebControlsSqlDataSource { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Boolean"/>
        /// </summary>
        public INamedTypeSymbol Boolean { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Byte"/>
        /// </summary>
        public INamedTypeSymbol Byte { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Char"/>
        /// </summary>
        public INamedTypeSymbol Char { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.DateTime"/>
        /// </summary>
        public INamedTypeSymbol DateTime { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Decimal"/>
        /// </summary>
        public INamedTypeSymbol Decimal { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Double"/>
        /// </summary>
        public INamedTypeSymbol Double { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Globalization.TimeSpanParse"/>
        /// </summary>
        public INamedTypeSymbol TimeSpanParse { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Guid"/>
        /// </summary>
        public INamedTypeSymbol Guid { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Int16"/>
        /// </summary>
        public INamedTypeSymbol Int16 { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Int32"/>
        /// </summary>
        public INamedTypeSymbol Int32 { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Int64"/>
        /// </summary>
        public INamedTypeSymbol Int64 { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Number"/>
        /// </summary>
        public INamedTypeSymbol Number { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Single"/>
        /// </summary>
        public INamedTypeSymbol Single { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.TimeSpan"/>
        /// </summary>
        public INamedTypeSymbol TimeSpan { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.HttpCookie"/>
        /// </summary>
        public INamedTypeSymbol HttpCookie { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.HttpRequestBase"/>
        /// </summary>
        public INamedTypeSymbol HttpRequestBase { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.HttpRequestWrapper"/>
        /// </summary>
        public INamedTypeSymbol HttpRequestWrapper { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.Adapters.PageAdapter"/>
        /// </summary>
        public INamedTypeSymbol PageAdapter { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.DataBoundLiteralControl"/>
        /// </summary>
        public INamedTypeSymbol DataBoundLiteralControl { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.DesignerDataBoundLiteralControl"/>
        /// </summary>
        public INamedTypeSymbol DesignerDataBoundLiteralControl { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.HtmlControls.HtmlInputControl"/>
        /// </summary>
        public INamedTypeSymbol HtmlInputControl { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.HtmlControls.HtmlInputFile"/>
        /// </summary>
        public INamedTypeSymbol HtmlInputFile { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.HtmlControls.HtmlInputRadioButton"/>
        /// </summary>
        public INamedTypeSymbol HtmlInputRadioButton { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.HtmlControls.HtmlInputText"/>
        /// </summary>
        public INamedTypeSymbol HtmlInputText { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.HtmlControls.HtmlSelect"/>
        /// </summary>
        public INamedTypeSymbol HtmlSelect { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.HtmlControls.HtmlTextArea"/>
        /// </summary>
        public INamedTypeSymbol HtmlTextArea { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.HtmlControls.HtmlTitle"/>
        /// </summary>
        public INamedTypeSymbol HtmlTitle { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.IndexedString"/>
        /// </summary>
        public INamedTypeSymbol IndexedString { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.LiteralControl"/>
        /// </summary>
        public INamedTypeSymbol LiteralControl { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.ResourceBasedLiteralControl"/>
        /// </summary>
        public INamedTypeSymbol ResourceBasedLiteralControl { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.SimplePropertyEntry"/>
        /// </summary>
        public INamedTypeSymbol SimplePropertyEntry { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.StateItem"/>
        /// </summary>
        public INamedTypeSymbol StateItem { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.StringPropertyBuilder"/>
        /// </summary>
        public INamedTypeSymbol StringPropertyBuilder { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.TemplateBuilder"/>
        /// </summary>
        public INamedTypeSymbol TemplateBuilder { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.TemplateParser"/>
        /// </summary>
        public INamedTypeSymbol TemplateParser { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.BaseValidator"/>
        /// </summary>
        public INamedTypeSymbol BaseValidator { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.BulletedList"/>
        /// </summary>
        public INamedTypeSymbol BulletedList { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.Button"/>
        /// </summary>
        public INamedTypeSymbol Button { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.ButtonColumn"/>
        /// </summary>
        public INamedTypeSymbol ButtonColumn { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.ButtonField"/>
        /// </summary>
        public INamedTypeSymbol ButtonField { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.ChangePassword"/>
        /// </summary>
        public INamedTypeSymbol ChangePassword { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.CheckBox"/>
        /// </summary>
        public INamedTypeSymbol CheckBox { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.CheckBoxField"/>
        /// </summary>
        public INamedTypeSymbol CheckBoxField { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.CheckBoxList"/>
        /// </summary>
        public INamedTypeSymbol CheckBoxList { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.CommandEventArgs"/>
        /// </summary>
        public INamedTypeSymbol CommandEventArgs { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.CreateUserWizard"/>
        /// </summary>
        public INamedTypeSymbol CreateUserWizard { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.DataKey"/>
        /// </summary>
        public INamedTypeSymbol DataKey { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.DataList"/>
        /// </summary>
        public INamedTypeSymbol DataList { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.DetailsView"/>
        /// </summary>
        public INamedTypeSymbol DetailsView { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.DetailsViewInsertEventArgs"/>
        /// </summary>
        public INamedTypeSymbol DetailsViewInsertEventArgs { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.DetailsViewUpdateEventArgs"/>
        /// </summary>
        public INamedTypeSymbol DetailsViewUpdateEventArgs { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.FormView"/>
        /// </summary>
        public INamedTypeSymbol FormView { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.FormViewInsertEventArgs"/>
        /// </summary>
        public INamedTypeSymbol FormViewInsertEventArgs { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.FormViewUpdateEventArgs"/>
        /// </summary>
        public INamedTypeSymbol FormViewUpdateEventArgs { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.GridView"/>
        /// </summary>
        public INamedTypeSymbol GridView { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.HiddenField"/>
        /// </summary>
        public INamedTypeSymbol HiddenField { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.HyperLink"/>
        /// </summary>
        public INamedTypeSymbol HyperLink { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.HyperLinkColumn"/>
        /// </summary>
        public INamedTypeSymbol HyperLinkColumn { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.HyperLinkField"/>
        /// </summary>
        public INamedTypeSymbol HyperLinkField { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.ImageButton"/>
        /// </summary>
        public INamedTypeSymbol ImageButton { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.Label"/>
        /// </summary>
        public INamedTypeSymbol Label { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.LinkButton"/>
        /// </summary>
        public INamedTypeSymbol LinkButton { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.ListControl"/>
        /// </summary>
        public INamedTypeSymbol ListControl { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.ListItem"/>
        /// </summary>
        public INamedTypeSymbol ListItem { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.Literal"/>
        /// </summary>
        public INamedTypeSymbol Literal { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.Login"/>
        /// </summary>
        public INamedTypeSymbol Login { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.Menu"/>
        /// </summary>
        public INamedTypeSymbol Menu { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.MenuItem"/>
        /// </summary>
        public INamedTypeSymbol MenuItem { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.MenuItemBinding"/>
        /// </summary>
        public INamedTypeSymbol MenuItemBinding { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.PasswordRecovery"/>
        /// </summary>
        public INamedTypeSymbol PasswordRecovery { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.QueryStringParameter"/>
        /// </summary>
        public INamedTypeSymbol QueryStringParameter { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.RadioButtonList"/>
        /// </summary>
        public INamedTypeSymbol RadioButtonList { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.ServerValidateEventArgs"/>
        /// </summary>
        public INamedTypeSymbol ServerValidateEventArgs { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.TableCell"/>
        /// </summary>
        public INamedTypeSymbol TableCell { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.TextBox"/>
        /// </summary>
        public INamedTypeSymbol TextBox { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.TreeNode"/>
        /// </summary>
        public INamedTypeSymbol TreeNode { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.TreeNodeBinding"/>
        /// </summary>
        public INamedTypeSymbol TreeNodeBinding { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.TreeView"/>
        /// </summary>
        public INamedTypeSymbol TreeView { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.Unit"/>
        /// </summary>
        public INamedTypeSymbol Unit { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.AppearanceEditorPart"/>
        /// </summary>
        public INamedTypeSymbol AppearanceEditorPart { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.PersonalizationEntry"/>
        /// </summary>
        public INamedTypeSymbol PersonalizationEntry { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartCatalogAddVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartCatalogAddVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartCatalogCloseVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartCatalogCloseVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartCloseVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartCloseVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartConnectionsCancelVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartConnectionsCancelVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartConnectionsCloseVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartConnectionsCloseVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartConnectionsConfigureVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartConnectionsConfigureVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartConnectionsConnectVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartConnectionsConnectVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartConnectionsDisconnectVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartConnectionsDisconnectVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartConnectVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartConnectVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartDeleteVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartDeleteVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartEditorApplyVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartEditorApplyVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartEditorCancelVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartEditorCancelVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartEditorOKVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartEditorOKVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartEditVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartEditVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartExportVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartExportVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartHeaderCloseVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartHeaderCloseVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartHelpVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartHelpVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartMinimizeVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartMinimizeVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartRestoreVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartRestoreVerb { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Web.UI.WebControls.WebParts.WebPartVerb"/>
        /// </summary>
        public INamedTypeSymbol WebPartVerb { get; }

        /// <summary>
        /// Set containing following named types, if not null:
        /// 1. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.ICollection"/>
        /// 2. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.ICollection{T}"/>
        /// 3. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>
        /// </summary>
        public ImmutableHashSet<INamedTypeSymbol> CollectionTypes { get; }

        /// <summary>
        /// Mapping of <see cref="ISymbol"/> to full name (e.g. "System.Exception").
        /// </summary>
        private Dictionary<ISymbol, string> TypeToFullName { get; }

        /// <summary>
        /// Mapping of full name to <see cref="INamedTypeSymbol"/>.
        /// </summary>
        private Dictionary<string, INamedTypeSymbol> FullNameToType { get; }

        /// <summary>
        /// Attempts to get the full type name (namespace + type) of the specifed symbol.
        /// </summary>
        /// <param name="symbol">Symbol, if any.</param>
        /// <param name="fullTypeName">Namespace + type name.</param>
        /// <returns>True if found, false otherwise.</returns>
        /// <remarks>This only works for types that this <see cref="WellKnownTypeProvider"/> knows about.</remarks>
        public bool TryGetFullTypeName(ISymbol symbol, out string fullTypeName)
        {
            return TypeToFullName.TryGetValue(symbol, out fullTypeName);
        }

        /// <summary>
        /// Attempts to get the type by the full type name.
        /// </summary>
        /// <param name="fullTypeName">>Namespace + type name.</param>
        /// <param name="namedTypeSymbol">Named type symbol, if any.</param>
        /// <returns>True if found, false otherwise.</returns>
        /// <remarks>This only works for types that this <see cref="WellKnownTypeProvider"/> knows about.</remarks>
        public bool TryGetType(string fullTypeName, out INamedTypeSymbol namedTypeSymbol)
        {
            return FullNameToType.TryGetValue(fullTypeName, out namedTypeSymbol);
        }

        /// <summary>
        /// Gets the INamedTypeSymbol from the compilation and caches a mapping from the INamedTypeSymbol to its canonical name.
        /// </summary>
        /// <param name="compilation">Compilation from which to retrieve the INamedTypeSymbol from.</param>
        /// <param name="metadataName">Metadata name.</param>
        /// <returns>INamedTypeSymbol, if any.</returns>
        private INamedTypeSymbol GetTypeByMetadataName(Compilation compilation, string metadataName)
        {
            INamedTypeSymbol namedTypeSymbol = compilation.GetTypeByMetadataName(metadataName);
            if (namedTypeSymbol != null)
            {
                this.TypeToFullName.Add(namedTypeSymbol, metadataName);
                this.FullNameToType.Add(metadataName, namedTypeSymbol);
            }

            return namedTypeSymbol;
        }

        private ImmutableHashSet<INamedTypeSymbol> GetWellKnownCollectionTypes(Compilation compilation)
        {
            var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
            var iCollection = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemCollectionsICollection);
            if (iCollection != null)
            {
                builder.Add(iCollection);
            }

            var genericICollection = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemCollectionsGenericICollection);
            if (genericICollection != null)
            {
                builder.Add(genericICollection);
            }

            var genericIReadOnlyCollection = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemCollectionsGenericIReadOnlyCollection);
            if (genericIReadOnlyCollection != null)
            {
                builder.Add(genericIReadOnlyCollection);
            }

            return builder.ToImmutable();
        }
    }
}
