' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class GeneratedNameConstants
        Friend Const IdSeparator As Char = "-"c
        Friend Const DotReplacementInTypeNames As Char = "-"c
        Friend Const MethodNameSeparator As Char = "_"c
        Friend Const AnonymousTypeOrDelegateCommonPrefix = "VB$Anonymous"
        Friend Const AnonymousTypeTemplateNamePrefix = AnonymousTypeOrDelegateCommonPrefix & "Type_"
        Friend Const AnonymousDelegateTemplateNamePrefix = AnonymousTypeOrDelegateCommonPrefix & "Delegate_"
        Friend Const DelegateStubParameterPrefix As String = "a"

        Friend Const Group As String = "$VB$Group"
        Friend Const It As String = "$VB$It"
        Friend Const It1 As String = "$VB$It1"
        Friend Const It2 As String = "$VB$It2"
        Friend Const ItAnonymous As String = "$VB$ItAnonymous"

        ' EE recognized names (prefixes):
        Friend Const HoistedMeName As String = "$VB$Me"
        Friend Const HoistedUserVariablePrefix As String = "$VB$Local_"
        Friend Const HoistedSpecialVariablePrefix As String = "$VB$NonLocal_" ' prefixes Me and Closure variables when hoisted
        Friend Const HoistedWithLocalPrefix As String = "$W"
        Friend Const StateMachineHoistedUserVariableOrDisplayClassPrefix As String = "$VB$ResumableLocal_"
        Friend Const ClosureVariablePrefix As String = "$VB$Closure_"
        Friend Const DisplayClassPrefix As String = "_Closure$__"
        Friend Const StateMachineTypeNamePrefix As String = "VB$StateMachine_"

        ' Do not change the following strings. Other teams (FxCop) use this string to identify lambda functions in its analysis
        ' If you have to change this string, please contact the VB language PM and consider the impact of that break.
        Friend Const LambdaMethodNamePrefix As String = "_Lambda$__"
        Friend Const DisplayClassGenericParameterNamePrefix As String = "$CLS"
        Friend Const BaseMethodWrapperNamePrefix As String = "$VB$ClosureStub_"

        ' Microsoft.VisualStudio.VIL.VisualStudioHost.AsyncReturnStackFrame depends on these names.
        Friend Const StateMachineBuilderFieldName As String = "$Builder"
        Friend Const StateMachineStateFieldName As String = "$State"

        Friend Const DelegateRelaxationDisplayClassPrefix As String = DisplayClassPrefix & "R"
        Friend Const DelegateRelaxationMethodNamePrefix As String = LambdaMethodNamePrefix & "R"
        Friend Const HoistedSynthesizedLocalPrefix As String = "$S"
        Friend Const LambdaCacheFieldPrefix As String = "$I"
        Friend Const DelegateRelaxationCacheFieldPrefix As String = "$IR"
        Friend Const StateMachineAwaiterFieldPrefix As String = "$A"
        Friend Const ReusableHoistedLocalFieldName As String = "$U"
        Friend Const StateMachineExpressionCapturePrefix As String = "$V"

        Friend Const StateMachineTypeParameterPrefix As String = "SM$"

        Friend Const IteratorCurrentFieldName As String = "$Current"
        Friend Const IteratorInitialThreadIdName As String = "$InitialThreadId"
        Friend Const IteratorParameterProxyPrefix As String = "$P_"

        Public Const StaticLocalFieldNamePrefix = "$STATIC$"
    End Class
End Namespace
