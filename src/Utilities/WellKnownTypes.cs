// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class WellKnownTypes
    {
        public const string SystemWebHttpRequest = "System.Web.HttpRequest";
        public const string SystemDataIDataAdapter= "System.Data.IDataAdapter";
        public const string SystemDataIDbCommand = "System.Data.IDbCommand";
        public const string SystemException = "System.Exception";
        public const string SystemDiagnosticContractsContract = "System.Diagnostics.Contracts.Contract";
        public const string SystemIDisposable = "System.IDisposable";
        public const string SystemThreadingMonitor = "System.Threading.Monitor";
        public const string SystemThreadingTasksTask = "System.Threading.Tasks.Task";
        public const string SystemCollectionsICollection = "System.Collections.ICollection";
        public const string SystemCollectionsGenericICollection = "System.Collections.Generic.ICollection`1";
        public const string SystemCollectionsGenericIReadOnlyCollection = "System.Collections.Generic.IReadOnlyCollection`1";
        public const string SystemRuntimeSerializationSerializationInfo = "System.Runtime.Serialization.SerializationInfo";
        public const string SystemIEquatable1 = "System.IEquatable`1";
        public const string SystemWebUIWebControlsSqlDataSource = "System.Web.UI.WebControls.SqlDataSource";
        public const string SystemDataSqlClientSqlParameter = "System.Data.SqlClient.SqlParameter";
        public const string SystemDataOleDbOleDbParameter = "System.Data.OleDb.OleDbParameter";
        public const string SystemDataOdbcOdbcParameter = "System.Data.Odbc.OdbcParameter";
        public const string SystemBoolean = "System.Boolean";
        public const string SystemByte = "System.Byte";
        public const string SystemChar = "System.Char";
        public const string SystemDateTime = "System.DateTime";
        public const string SystemDecimal = "System.Decimal";
        public const string SystemDouble = "System.Double";
        public const string SystemGlobalizationTimeSpanParse = "System.Globalization.TimeSpanParse";
        public const string SystemGuid = "System.Guid";
        public const string SystemInt16 = "System.Int16";
        public const string SystemInt32 = "System.Int32";
        public const string SystemInt64 = "System.Int64";
        public const string SystemNumber = "System.Number";
        public const string SystemSingle = "System.Single";
        public const string SystemTimeSpan = "System.TimeSpan";
        public const string SystemWebHttpCookie = "System.Web.HttpCookie";
        public const string SystemWebHttpRequestBase = "System.Web.HttpRequestBase";
        public const string SystemWebHttpRequestWrapper = "System.Web.HttpRequestWrapper";
        public const string SystemWebUIAdaptersPageAdapter = "System.Web.UI.Adapters.PageAdapter";
        public const string SystemWebUIDataBoundLiteralControl = "System.Web.UI.DataBoundLiteralControl";
        public const string SystemWebUIDesignerDataBoundLiteralControl = "System.Web.UI.DesignerDataBoundLiteralControl";
        public const string SystemWebUIHtmlControlsHtmlInputControl = "System.Web.UI.HtmlControls.HtmlInputControl";
        public const string SystemWebUIHtmlControlsHtmlInputFile = "System.Web.UI.HtmlControls.HtmlInputFile";
        public const string SystemWebUIHtmlControlsHtmlInputRadioButton = "System.Web.UI.HtmlControls.HtmlInputRadioButton";
        public const string SystemWebUIHtmlControlsHtmlInputText = "System.Web.UI.HtmlControls.HtmlInputText";
        public const string SystemWebUIHtmlControlsHtmlSelect = "System.Web.UI.HtmlControls.HtmlSelect";
        public const string SystemWebUIHtmlControlsHtmlTextArea = "System.Web.UI.HtmlControls.HtmlTextArea";
        public const string SystemWebUIHtmlControlsHtmlTitle = "System.Web.UI.HtmlControls.HtmlTitle";
        public const string SystemWebUIIndexedString = "System.Web.UI.IndexedString";
        public const string SystemWebUILiteralControl = "System.Web.UI.LiteralControl";
        public const string SystemWebUIResourceBasedLiteralControl = "System.Web.UI.ResourceBasedLiteralControl";
        public const string SystemWebUISimplePropertyEntry = "System.Web.UI.SimplePropertyEntry";
        public const string SystemWebUIStateItem = "System.Web.UI.StateItem";
        public const string SystemWebUIStringPropertyBuilder = "System.Web.UI.StringPropertyBuilder";
        public const string SystemWebUITemplateBuilder = "System.Web.UI.TemplateBuilder";
        public const string SystemWebUITemplateParser = "System.Web.UI.TemplateParser";
        public const string SystemWebUIWebControlsBaseValidator = "System.Web.UI.WebControls.BaseValidator";
        public const string SystemWebUIWebControlsBulletedList = "System.Web.UI.WebControls.BulletedList";
        public const string SystemWebUIWebControlsButton = "System.Web.UI.WebControls.Button";
        public const string SystemWebUIWebControlsButtonColumn = "System.Web.UI.WebControls.ButtonColumn";
        public const string SystemWebUIWebControlsButtonField = "System.Web.UI.WebControls.ButtonField";
        public const string SystemWebUIWebControlsChangePassword = "System.Web.UI.WebControls.ChangePassword";
        public const string SystemWebUIWebControlsCheckBox = "System.Web.UI.WebControls.CheckBox";
        public const string SystemWebUIWebControlsCheckBoxField = "System.Web.UI.WebControls.CheckBoxField";
        public const string SystemWebUIWebControlsCheckBoxList = "System.Web.UI.WebControls.CheckBoxList";
        public const string SystemWebUIWebControlsCommandEventArgs = "System.Web.UI.WebControls.CommandEventArgs";
        public const string SystemWebUIWebControlsCreateUserWizard = "System.Web.UI.WebControls.CreateUserWizard";
        public const string SystemWebUIWebControlsDataKey = "System.Web.UI.WebControls.DataKey";
        public const string SystemWebUIWebControlsDataList = "System.Web.UI.WebControls.DataList";
        public const string SystemWebUIWebControlsDetailsView = "System.Web.UI.WebControls.DetailsView";
        public const string SystemWebUIWebControlsDetailsViewInsertEventArgs = "System.Web.UI.WebControls.DetailsViewInsertEventArgs";
        public const string SystemWebUIWebControlsDetailsViewUpdateEventArgs = "System.Web.UI.WebControls.DetailsViewUpdateEventArgs";
        public const string SystemWebUIWebControlsFormView = "System.Web.UI.WebControls.FormView";
        public const string SystemWebUIWebControlsFormViewInsertEventArgs = "System.Web.UI.WebControls.FormViewInsertEventArgs";
        public const string SystemWebUIWebControlsFormViewUpdateEventArgs = "System.Web.UI.WebControls.FormViewUpdateEventArgs";
        public const string SystemWebUIWebControlsGridView = "System.Web.UI.WebControls.GridView";
        public const string SystemWebUIWebControlsHiddenField = "System.Web.UI.WebControls.HiddenField";
        public const string SystemWebUIWebControlsHyperLink = "System.Web.UI.WebControls.HyperLink";
        public const string SystemWebUIWebControlsHyperLinkColumn = "System.Web.UI.WebControls.HyperLinkColumn";
        public const string SystemWebUIWebControlsHyperLinkField = "System.Web.UI.WebControls.HyperLinkField";
        public const string SystemWebUIWebControlsImageButton = "System.Web.UI.WebControls.ImageButton";
        public const string SystemWebUIWebControlsLabel = "System.Web.UI.WebControls.Label";
        public const string SystemWebUIWebControlsLinkButton = "System.Web.UI.WebControls.LinkButton";
        public const string SystemWebUIWebControlsListControl = "System.Web.UI.WebControls.ListControl";
        public const string SystemWebUIWebControlsListItem = "System.Web.UI.WebControls.ListItem";
        public const string SystemWebUIWebControlsLiteral = "System.Web.UI.WebControls.Literal";
        public const string SystemWebUIWebControlsLogin = "System.Web.UI.WebControls.Login";
        public const string SystemWebUIWebControlsMenu = "System.Web.UI.WebControls.Menu";
        public const string SystemWebUIWebControlsMenuItem = "System.Web.UI.WebControls.MenuItem";
        public const string SystemWebUIWebControlsMenuItemBinding = "System.Web.UI.WebControls.MenuItemBinding";
        public const string SystemWebUIWebControlsPasswordRecovery = "System.Web.UI.WebControls.PasswordRecovery";
        public const string SystemWebUIWebControlsQueryStringParameter = "System.Web.UI.WebControls.QueryStringParameter";
        public const string SystemWebUIWebControlsRadioButtonList = "System.Web.UI.WebControls.RadioButtonList";
        public const string SystemWebUIWebControlsServerValidateEventArgs = "System.Web.UI.WebControls.ServerValidateEventArgs";
        public const string SystemWebUIWebControlsTableCell = "System.Web.UI.WebControls.TableCell";
        public const string SystemWebUIWebControlsTextBox = "System.Web.UI.WebControls.TextBox";
        public const string SystemWebUIWebControlsTreeNode = "System.Web.UI.WebControls.TreeNode";
        public const string SystemWebUIWebControlsTreeNodeBinding = "System.Web.UI.WebControls.TreeNodeBinding";
        public const string SystemWebUIWebControlsTreeView = "System.Web.UI.WebControls.TreeView";
        public const string SystemWebUIWebControlsUnit = "System.Web.UI.WebControls.Unit";
        public const string SystemWebUIWebControlsWebPartsAppearanceEditorPart = "System.Web.UI.WebControls.WebParts.AppearanceEditorPart";
        public const string SystemWebUIWebControlsWebPartsPersonalizationEntry = "System.Web.UI.WebControls.WebParts.PersonalizationEntry";
        public const string SystemWebUIWebControlsWebPartsWebPartCatalogAddVerb = "System.Web.UI.WebControls.WebParts.WebPartCatalogAddVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartCatalogCloseVerb = "System.Web.UI.WebControls.WebParts.WebPartCatalogCloseVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartCloseVerb = "System.Web.UI.WebControls.WebParts.WebPartCloseVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartConnectionsCancelVerb = "System.Web.UI.WebControls.WebParts.WebPartConnectionsCancelVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartConnectionsCloseVerb = "System.Web.UI.WebControls.WebParts.WebPartConnectionsCloseVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartConnectionsConfigureVerb = "System.Web.UI.WebControls.WebParts.WebPartConnectionsConfigureVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartConnectionsConnectVerb = "System.Web.UI.WebControls.WebParts.WebPartConnectionsConnectVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartConnectionsDisconnectVerb = "System.Web.UI.WebControls.WebParts.WebPartConnectionsDisconnectVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartConnectVerb = "System.Web.UI.WebControls.WebParts.WebPartConnectVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartDeleteVerb = "System.Web.UI.WebControls.WebParts.WebPartDeleteVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartEditorApplyVerb = "System.Web.UI.WebControls.WebParts.WebPartEditorApplyVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartEditorCancelVerb = "System.Web.UI.WebControls.WebParts.WebPartEditorCancelVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartEditorOKVerb = "System.Web.UI.WebControls.WebParts.WebPartEditorOKVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartEditVerb = "System.Web.UI.WebControls.WebParts.WebPartEditVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartExportVerb = "System.Web.UI.WebControls.WebParts.WebPartExportVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartHeaderCloseVerb = "System.Web.UI.WebControls.WebParts.WebPartHeaderCloseVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartHelpVerb = "System.Web.UI.WebControls.WebParts.WebPartHelpVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartMinimizeVerb = "System.Web.UI.WebControls.WebParts.WebPartMinimizeVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartRestoreVerb = "System.Web.UI.WebControls.WebParts.WebPartRestoreVerb";
        public const string SystemWebUIWebControlsWebPartsWebPartVerb = "System.Web.UI.WebControls.WebParts.WebPartVerb";

        public static INamedTypeSymbol ICollection(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemCollectionsICollection);
        }

        public static INamedTypeSymbol GenericICollection(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemCollectionsGenericICollection);
        }

        public static INamedTypeSymbol GenericIReadOnlyCollection(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemCollectionsGenericIReadOnlyCollection);
        }

        public static INamedTypeSymbol IEnumerable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
        }

        public static INamedTypeSymbol IEnumerator(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.IEnumerator");
        }

        public static INamedTypeSymbol GenericIEnumerable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        }

        public static INamedTypeSymbol GenericIEnumerator(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerator`1");
        }

        public static INamedTypeSymbol IList(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.IList");
        }

        internal static INamedTypeSymbol HttpRequest(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemWebHttpRequest);
        }

        internal static INamedTypeSymbol NameValueCollection(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Specialized.NameValueCollection");
        }

        public static INamedTypeSymbol GenericIList(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1");
        }

        public static INamedTypeSymbol Array(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_Array);
        }

        public static INamedTypeSymbol FlagsAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.FlagsAttribute");
        }

        public static INamedTypeSymbol StringComparison(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.StringComparison");
        }

        public static INamedTypeSymbol CharSet(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.CharSet");
        }

        public static INamedTypeSymbol DllImportAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.DllImportAttribute");
        }

        public static INamedTypeSymbol MarshalAsAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.MarshalAsAttribute");
        }

        public static INamedTypeSymbol StringBuilder(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Text.StringBuilder");
        }

        public static INamedTypeSymbol UnmanagedType(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedType");
        }

        public static INamedTypeSymbol MarshalByRefObject(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.MarshalByRefObject");
        }

        public static INamedTypeSymbol ExecutionEngineException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ExecutionEngineException");
        }

        public static INamedTypeSymbol OutOfMemoryException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.OutOfMemoryException");
        }

        public static INamedTypeSymbol StackOverflowException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.StackOverflowException");
        }

        public static INamedTypeSymbol MemberInfo(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Reflection.MemberInfo");
        }

        public static INamedTypeSymbol ParameterInfo(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Reflection.ParameterInfo");
        }

        public static INamedTypeSymbol Monitor(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemThreadingMonitor);
        }

        public static INamedTypeSymbol Thread(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Threading.Thread");
        }

        public static INamedTypeSymbol Task(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemThreadingTasksTask);
        }

        public static INamedTypeSymbol WebMethodAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Services.WebMethodAttribute");
        }

        public static INamedTypeSymbol WebUIControl(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.UI.Control");
        }

        public static INamedTypeSymbol WebUILiteralControl(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.UI.LiteralControl");
        }

        public static INamedTypeSymbol WinFormsUIControl(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Windows.Forms.Control");
        }

        public static INamedTypeSymbol NotImplementedException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.NotImplementedException");
        }

        public static INamedTypeSymbol IDisposable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemIDisposable);
        }

        public static INamedTypeSymbol ISerializable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.ISerializable");
        }

        public static INamedTypeSymbol SerializationInfo(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemRuntimeSerializationSerializationInfo);
        }

        public static INamedTypeSymbol StreamingContext(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.StreamingContext");
        }

        public static INamedTypeSymbol OnDeserializingAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.OnDeserializingAttribute");
        }

        public static INamedTypeSymbol OnDeserializedAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.OnDeserializedAttribute");
        }

        public static INamedTypeSymbol OnSerializingAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.OnSerializingAttribute");
        }

        public static INamedTypeSymbol OnSerializedAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.OnSerializedAttribute");
        }

        public static INamedTypeSymbol SerializableAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.SerializableAttribute");
        }

        public static INamedTypeSymbol NonSerializedAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.NonSerializedAttribute");
        }

        public static INamedTypeSymbol Attribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Attribute");
        }

        public static INamedTypeSymbol AttributeUsageAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.AttributeUsageAttribute");
        }

        public static INamedTypeSymbol AssemblyVersionAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Reflection.AssemblyVersionAttribute");
        }

        public static INamedTypeSymbol CLSCompliantAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.CLSCompliantAttribute");
        }

        public static INamedTypeSymbol ConditionalAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Diagnostics.ConditionalAttribute");
        }

        public static INamedTypeSymbol IComparable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.IComparable");
        }

        public static INamedTypeSymbol GenericIComparable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.IComparable`1");
        }

        public static INamedTypeSymbol ComSourceInterfaceAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.ComSourceInterfacesAttribute");
        }

        public static INamedTypeSymbol GenericEventHandler(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.EventHandler`1");
        }

        public static INamedTypeSymbol EventArgs(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.EventArgs");
        }

        public static INamedTypeSymbol Uri(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Uri");
        }

        public static INamedTypeSymbol ComVisibleAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.ComVisibleAttribute");
        }

        public static INamedTypeSymbol NeutralResourcesLanguageAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Resources.NeutralResourcesLanguageAttribute");
        }

        public static INamedTypeSymbol GeneratedCodeAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.CodeDom.Compiler.GeneratedCodeAttribute");
        }

        public static INamedTypeSymbol Console(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Console");
        }

        public static INamedTypeSymbol String(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_String);
        }

        public static INamedTypeSymbol Object(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_Object);
        }

        public static INamedTypeSymbol Exception(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemException);
        }

        public static INamedTypeSymbol InvalidOperationException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.InvalidOperationException");
        }

        public static INamedTypeSymbol ArgumentException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ArgumentException");
        }

        public static INamedTypeSymbol NotSupportedException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.NotSupportedException");
        }

        public static INamedTypeSymbol KeyNotFoundException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.KeyNotFoundException");
        }

        public static INamedTypeSymbol GenericIEqualityComparer(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
        }

        public static INamedTypeSymbol GenericIEquatable(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemIEquatable1);
        }

        public static INamedTypeSymbol IHashCodeProvider(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Collections.IHashCodeProvider");
        }

        public static INamedTypeSymbol IntPtr(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_IntPtr);
        }

        public static INamedTypeSymbol UIntPtr(Compilation compilation)
        {
            return compilation.GetSpecialType(SpecialType.System_UIntPtr);
        }

        public static INamedTypeSymbol HandleRef(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.HandleRef");
        }

        public static INamedTypeSymbol DataMemberAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.Serialization.DataMemberAttribute");
        }

        public static INamedTypeSymbol ObsoleteAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ObsoleteAttribute");
        }

        public static INamedTypeSymbol PureAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Diagnostics.Contracts.PureAttribute");
        }

        public static INamedTypeSymbol MEFV1ExportAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ComponentModel.Composition.ExportAttribute");
        }

        public static INamedTypeSymbol MEFV2ExportAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Composition.ExportAttribute");
        }

        public static INamedTypeSymbol LocalizableAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.ComponentModel.LocalizableAttribute");
        }

        public static INamedTypeSymbol FieldOffsetAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.FieldOffsetAttribute");
        }

        public static INamedTypeSymbol StructLayoutAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
        }

        public static INamedTypeSymbol IDbCommand(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(SystemDataIDbCommand);
        }

        public static INamedTypeSymbol IDataAdapter(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Data.IDataAdapter");
        }

        public static INamedTypeSymbol MvcController(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.Controller");
        }

        public static INamedTypeSymbol MvcControllerBase(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.ControllerBase");
        }

        public static INamedTypeSymbol ActionResult(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.ActionResult");
        }

        public static INamedTypeSymbol ValidateAntiforgeryTokenAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.ValidateAntiForgeryTokenAttribute");
        }

        public static INamedTypeSymbol HttpGetAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.HttpGetAttribute");
        }

        public static INamedTypeSymbol HttpPostAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.HttpPostAttribute");
        }

        public static INamedTypeSymbol HttpPutAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.HttpPutAttribute");
        }

        public static INamedTypeSymbol HttpDeleteAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.HttpDeleteAttribute");
        }

        public static INamedTypeSymbol HttpPatchAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.HttpPatchAttribute");
        }

        public static INamedTypeSymbol AcceptVerbsAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.AcceptVerbsAttribute");
        }

        public static INamedTypeSymbol NonActionAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.NonActionAttribute");
        }

        public static INamedTypeSymbol ChildActionOnlyAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.ChildActionOnlyAttribute");
        }

        public static INamedTypeSymbol HttpVerbs(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Web.Mvc.HttpVerbs");
        }

        #region Test Framework Types
        public static INamedTypeSymbol TestCleanupAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute");
        }

        public static INamedTypeSymbol TestInitializeAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute");
        }

        public static INamedTypeSymbol TestMethodAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute");
        }

        public static INamedTypeSymbol DataTestMethodAttribute(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute");
        }

        public static INamedTypeSymbol ExpectedException(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ExpectedExceptionAttribute");
        }

        public static INamedTypeSymbol UnitTestingAssert(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert");
        }

        public static INamedTypeSymbol UnitTestingCollectionAssert(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert");
        }

        public static INamedTypeSymbol UnitTestingCollectionStringAssert(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.StringAssert");
        }

        public static INamedTypeSymbol XunitAssert(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Xunit.Assert");
        }

        public static INamedTypeSymbol XunitFact(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Xunit.FactAttribute");
        }

        public static INamedTypeSymbol XunitTheory(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Xunit.TheoryAttribute");
        }

        public static INamedTypeSymbol NunitAssert(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("NUnit.Framework.Assert");
        }

        public static INamedTypeSymbol NunitOneTimeSetUp(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("NUnit.Framework.OneTimeSetUpAttribute");
        }

        public static INamedTypeSymbol NunitOneTimeTearDown(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("NUnit.Framework.OneTimeTearDownAttribute");
        }

        public static INamedTypeSymbol NunitSetUp(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("NUnit.Framework.SetUpAttribute");
        }

        public static INamedTypeSymbol NunitSetUpFixture(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("NUnit.Framework.SetUpFixtureAttribute");
        }

        public static INamedTypeSymbol NunitTearDown(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("NUnit.Framework.TearDownAttribute");
        }

        public static INamedTypeSymbol NunitTest(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("NUnit.Framework.TestAttribute");
        }

        public static INamedTypeSymbol NunitTestCase(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("NUnit.Framework.TestCaseAttribute");
        }

        public static INamedTypeSymbol NunitTestCaseSource(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("NUnit.Framework.TestCaseSourceAttribute");
        }

        public static INamedTypeSymbol NunitTheory(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("NUnit.Framework.TheoryAttribute");
        }

        public static INamedTypeSymbol XmlWriter(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("System.Xml.XmlWriter");
        }

        #endregion
    }
}