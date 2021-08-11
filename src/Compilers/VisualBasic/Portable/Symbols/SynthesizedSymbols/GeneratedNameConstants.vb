' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class GeneratedNameConstants
        Public Const DotReplacementInTypeNames As Char = "-"c
        Public Const MethodNameSeparator As Char = "_"c
        Public Const AnonymousTypeOrDelegateCommonPrefix = "VB$Anonymous"
        Public Const AnonymousTypeTemplateNamePrefix = AnonymousTypeOrDelegateCommonPrefix & "Type_"
        Public Const AnonymousDelegateTemplateNamePrefix = AnonymousTypeOrDelegateCommonPrefix & "Delegate_"
        Public Const DelegateStubParameterPrefix As String = "a"

        Public Const Group As String = "$VB$Group"
        Public Const It As String = "$VB$It"
        Public Const It1 As String = "$VB$It1"
        Public Const It2 As String = "$VB$It2"
        Public Const ItAnonymous As String = "$VB$ItAnonymous"

        ' EE recognized names (prefixes):
        Public Const HoistedMeName As String = "$VB$Me"
        Public Const HoistedUserVariablePrefix As String = "$VB$Local_"
        Public Const HoistedSpecialVariablePrefix As String = "$VB$NonLocal_" ' prefixes Me and Closure variables when hoisted
        Public Const HoistedWithLocalPrefix As String = "$W"
        Public Const StateMachineHoistedUserVariablePrefix As String = "$VB$ResumableLocal_"
        Public Const ClosureVariablePrefix As String = "$VB$Closure_"
        Public Const DisplayClassPrefix As String = "_Closure$__"
        Public Const StateMachineTypeNamePrefix As String = "VB$StateMachine_"

        ' Do not change the following strings. Other teams (FxCop) use this string to identify lambda functions in its analysis
        ' If you have to change this string, please contact the VB language PM and consider the impact of that break.
        Public Const LambdaMethodNamePrefix As String = "_Lambda$__"
        Public Const DisplayClassGenericParameterNamePrefix As String = "$CLS"
        Public Const BaseMethodWrapperNamePrefix As String = "$VB$ClosureStub_"

        ' Microsoft.VisualStudio.VIL.VisualStudioHost.AsyncReturnStackFrame depends on these names.
        Public Const StateMachineBuilderFieldName As String = "$Builder"
        Public Const StateMachineStateFieldName As String = "$State"

        Public Const DelegateRelaxationDisplayClassPrefix As String = DisplayClassPrefix & "R"
        Public Const DelegateRelaxationMethodNamePrefix As String = LambdaMethodNamePrefix & "R"
        Public Const HoistedSynthesizedLocalPrefix As String = "$S"
        Public Const LambdaCacheFieldPrefix As String = "$I"
        Public Const DelegateRelaxationCacheFieldPrefix As String = "$IR"
        Public Const StateMachineAwaiterFieldPrefix As String = "$A"
        Public Const ReusableHoistedLocalFieldName As String = "$U"
        Public Const StateMachineExpressionCapturePrefix As String = "$V"

        Public Const StateMachineTypeParameterPrefix As String = "SM$"

        Public Const IteratorCurrentFieldName As String = "$Current"
        Public Const IteratorInitialThreadIdName As String = "$InitialThreadId"
        Public Const IteratorParameterProxyPrefix As String = "$P_"

        Public Const StaticLocalFieldNamePrefix = "$STATIC$"
    End Class
End Namespace
