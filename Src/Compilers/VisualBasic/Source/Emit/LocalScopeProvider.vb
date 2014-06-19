Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Reflection.Emit
Imports System.Text
Imports Roslyn.Compilers.CodeGen
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic.Emit

Namespace Roslyn.Compilers.VisualBasic

    Friend Class LocalScopeProvider
        Implements Microsoft.Cci.ILocalScopeProvider

        Public Function GetLocalScopes(
            methodBody As Microsoft.Cci.IMethodBody
        ) As IEnumerable(Of Microsoft.Cci.ILocalScope) Implements Microsoft.Cci.ILocalScopeProvider.GetLocalScopes
            Return DirectCast(methodBody, MethodBody).LocalScopes
        End Function

        Public Function GetNamespaceScopes(
            methodBody As Microsoft.Cci.IMethodBody
        ) As IEnumerable(Of Microsoft.Cci.INamespaceScope) Implements Microsoft.Cci.ILocalScopeProvider.GetNamespaceScopes
            Return SpecializedCollections.EmptyEnumerable(Of Microsoft.Cci.INamespaceScope)()
        End Function

        Public Function GetIteratorScopes(
            methodBody As Microsoft.Cci.IMethodBody
        ) As IList(Of Microsoft.Cci.ILocalScope) Implements Microsoft.Cci.ILocalScopeProvider.GetIteratorScopes
            Return New List(Of Microsoft.Cci.ILocalScope)()
        End Function

        Public Function GetConstantsInScope(
            scope As Microsoft.Cci.ILocalScope
        ) As IEnumerable(Of Microsoft.Cci.ILocalDefinition) Implements Microsoft.Cci.ILocalScopeProvider.GetConstantsInScope
            Return DirectCast(scope, LocalScope).Constants
        End Function

        Public Function GetVariablesInScope(
            scope As Microsoft.Cci.ILocalScope
        ) As IEnumerable(Of Microsoft.Cci.ILocalDefinition) Implements Microsoft.Cci.ILocalScopeProvider.GetVariablesInScope
            Return DirectCast(scope, LocalScope).Variables
        End Function

        Public Function IteratorClassName(
            methodBody As Microsoft.Cci.IMethodBody
        ) As String Implements Microsoft.Cci.ILocalScopeProvider.IteratorClassName
            Return Nothing
        End Function

    End Class

End Namespace