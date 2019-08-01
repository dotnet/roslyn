// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class WellKnownCommandNames
    {
        public const string Build_BuildSolution = "Build.BuildSolution";
        public const string Build_SolutionConfigurations = "Build.SolutionConfigurations";

        public const string Edit_GoToAll = "Edit.GoToAll";
        public const string Edit_GoToDefinition = "Edit.GoToDefinition";
        public const string Edit_GoToImplementation = "Edit.GoToImplementation";
        public const string Edit_ListMembers = "Edit.ListMembers";
        public const string Edit_ParameterInfo = "Edit.ParameterInfo";
        public const string Edit_QuickInfo = "Edit.QuickInfo";
        public const string Edit_ToggleCompletionMode = "Edit.ToggleCompletionMode";
        public const string Edit_Undo = "Edit.Undo";
        public const string Edit_Redo = "Edit.Redo";
        public const string Edit_SelectionCancel = "Edit.SelectionCancel";
        public const string Edit_LineStart = "Edit.LineStart";
        public const string Edit_LineEnd = "Edit.LineEnd";
        public const string Edit_LineStartExtend = "Edit.LineStartExtend";
        public const string Edit_LineEndExtend = "Edit.LineEndExtend";
        public const string Edit_SelectAll = "Edit.SelectAll";
        public const string Edit_Copy = "Edit.Copy";
        public const string Edit_Cut = "Edit.Cut";
        public const string Edit_Paste = "Edit.Paste";
        public const string Edit_Delete = "Edit.Delete";
        public const string Edit_LineUp = "Edit.LineUp";
        public const string Edit_LineDown = "Edit.LineDown";
        public const string Edit_FormatDocument = "Edit.FormatDocument";

        public const string File_OpenFile = "File.OpenFile";
        public const string File_SaveAll = "File.SaveAll";

        public const string InteractiveConsole_Reset = "InteractiveConsole.Reset";
        public const string InteractiveConsole_ClearScreen = "InteractiveConsole.ClearScreen";
        public const string InteractiveConsole_ExecuteInInteractive = "InteractiveConsole.ExecuteInInteractive";

        public const string ProjectAndSolutionContextMenus_Solution_RestoreNuGetPackages = "ProjectandSolutionContextMenus.Solution.RestoreNuGetPackages";
        public const string ProjectAndSolutionContextMenus_Project_ResetCSharpInteractiveFromProject
            = "ProjectandSolutionContextMenus.Project.ResetC#InteractiveFromProject";

        public const string Refactor_Rename = "Refactor.Rename";
        public const string Refactor_ExtractMethod = "Refactor.ExtractMethod";
        public const string Refactor_ExtractInterface = "Refactor.ExtractInterface";
        public const string Refactor_EncapsulateField = "Refactor.EncapsulateField";
        public const string Refactor_RemoveParameters = "Refactor.RemoveParameters";
        public const string Refactor_ReorderParameters = "Refactor.ReorderParameters";

        public const string Test_IntegrationTestService_Start = "Test.IntegrationTestService.Start";
        public const string Test_IntegrationTestService_Stop = "Test.IntegrationTestService.Stop";
        public const string Test_IntegrationTestService_DisableAsyncCompletion = "Test.IntegrationTestService.DisableAsyncCompletion";

        public const string View_ErrorList = "View.ErrorList";
        public const string View_ShowSmartTag = "View.ShowSmartTag";
        public const string View_Output = "View.Output";
    }
}
