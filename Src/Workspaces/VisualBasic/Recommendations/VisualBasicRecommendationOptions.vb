Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.Recommendations
    Public Module VisualBasicRecommendationOptions
        Friend Const RecommendationsFeatureName = "CSharp/Recommendations"

#If MEF Then
        <ExportOption>
#End If
        Public ReadOnly HideAdvancedMembers As New [Option](Of Boolean)(RecommendationsFeatureName, "HideAdvancedMembers", defaultValue:=False)

#If MEF Then
        <ExportOption>
#End If
        Public ReadOnly FilterOutOfScopeLocals As New [Option](Of Boolean)(RecommendationsFeatureName, "FilterOutOfScopeLocals", defaultValue:=True)

    End Module
End Namespace