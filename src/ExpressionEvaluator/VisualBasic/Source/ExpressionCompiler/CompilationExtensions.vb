Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend Module CompilationExtensions
        Private Function [GetType]([module] As PEModuleSymbol, typeHandle As TypeDefinitionHandle) As PENamedTypeSymbol
            Dim metadataDecoder = New MetadataDecoder([module])
            Return DirectCast(metadataDecoder.GetTypeOfToken(typeHandle), PENamedTypeSymbol)
        End Function

        <Extension>
        Friend Function [GetType](compilation As VisualBasicCompilation, moduleVersionId As Guid, typeToken As Integer, <Out> ByRef metadataDecoder As MetadataDecoder) As PENamedTypeSymbol
            Dim [module] = compilation.GetModule(moduleVersionId)
            Dim reader = [module].Module.MetadataReader
            Dim typeHandle = CType(MetadataTokens.Handle(typeToken), TypeDefinitionHandle)
            Dim type = [GetType]([module], typeHandle)
            metadataDecoder = New MetadataDecoder([module], type)
            Return type
        End Function

        <Extension>
        Friend Function GetSourceMethod(compilation As VisualBasicCompilation, moduleVersionId As Guid, methodToken As Integer) As PEMethodSymbol
            Dim methodHandle = CType(MetadataTokens.Handle(methodToken), MethodDefinitionHandle)
            Dim method = GetMethod(compilation, moduleVersionId, methodHandle)
            Dim metadataDecoder = New MetadataDecoder(DirectCast(method.ContainingModule, PEModuleSymbol))
            Dim containingType = method.ContainingType
            Dim sourceMethodName As String = Nothing
            If GeneratedNames.TryParseStateMachineTypeName(containingType.Name, sourceMethodName) Then
                For Each member In containingType.ContainingType.GetMembers(sourceMethodName)
                    Dim candidateMethod = TryCast(member, PEMethodSymbol)
                    If candidateMethod IsNot Nothing Then
                        Dim [module] = metadataDecoder.Module
                        methodHandle = candidateMethod.Handle
                        Dim stateMachineTypeName As String = Nothing
                        If [module].HasStringValuedAttribute(methodHandle, AttributeDescription.AsyncStateMachineAttribute, stateMachineTypeName) OrElse
                            [module].HasStringValuedAttribute(methodHandle, AttributeDescription.IteratorStateMachineAttribute, stateMachineTypeName) _
                        Then
                            If metadataDecoder.GetTypeSymbolForSerializedType(stateMachineTypeName).OriginalDefinition.Equals(containingType) Then
                                Return candidateMethod
                            End If
                        End If
                    End If
                Next
            End If
            Return method
        End Function

        <Extension>
        Friend Function GetMethod(compilation As VisualBasicCompilation, moduleVersionId As Guid, methodHandle As MethodDefinitionHandle) As PEMethodSymbol
            Dim [module] = compilation.GetModule(moduleVersionId)
            Dim reader = [module].Module.MetadataReader
            Dim typeHandle = reader.GetMethodDefinition(methodHandle).GetDeclaringType()
            Dim type = [GetType]([module], typeHandle)
            Dim method = DirectCast(New MetadataDecoder([module], type).GetMethodSymbolForMethodDefOrMemberRef(methodHandle, type), PEMethodSymbol)
            Return method
        End Function

        <Extension>
        Friend Function GetModule(compilation As VisualBasicCompilation, moduleVersionId As Guid) As PEModuleSymbol
            For Each pair In compilation.GetBoundReferenceManager().GetReferencedAssemblies()
                Dim assembly = DirectCast(pair.Value, AssemblySymbol)
                For Each [module] In assembly.Modules
                    Dim m = DirectCast([module], PEModuleSymbol)
                    Dim id = m.Module.GetModuleVersionIdOrThrow()
                    If id = moduleVersionId Then
                        Return m
                    End If
                Next
            Next

            Return Nothing
        End Function

        <Extension>
        Friend Function ToCompilation(metadataBlocks As ImmutableArray(Of MetadataBlock)) As VisualBasicCompilation
            Return VisualBasicCompilation.Create(
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName(),
                references:=metadataBlocks.MakeAssemblyReferences(),
                options:=CompilationOptions)
        End Function

        ' XML file references, #r directives not supported:
        Private ReadOnly CompilationOptions As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(
            outputKind:=OutputKind.DynamicallyLinkedLibrary,
            platform:=Platform.AnyCpu, ' Platform should match PEModule.Machine, in this case I386.
            optimizationLevel:=OptimizationLevel.Release,
            assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default).
            WithMetadataImportOptions(MetadataImportOptions.All)

    End Module

End Namespace