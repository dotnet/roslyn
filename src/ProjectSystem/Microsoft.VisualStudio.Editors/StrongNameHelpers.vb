Imports System.Security
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Namespace Microsoft.Runtime.Hosting
    Friend Class StrongNameHelpers
        ' Methods
        <SecurityCritical()> _
        Public Shared Function StrongNameErrorInfo() As Integer
            Return StrongNameHelpers.ts_LastStrongNameHR
        End Function

        <SecurityCritical()> _
        Public Shared Sub StrongNameFreeBuffer(ByVal pbMemory As IntPtr)
            StrongNameHelpers.StrongNameUsingIntPtr.StrongNameFreeBuffer(pbMemory)
        End Sub

        <SecurityCritical()> _
        Public Shared Function StrongNameGetPublicKey(ByVal pwzKeyContainer As String, ByVal pbKeyBlob As IntPtr, ByVal cbKeyBlob As Integer, <Out()> ByRef ppbPublicKeyBlob As IntPtr, <Out()> ByRef pcbPublicKeyBlob As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongNameUsingIntPtr.StrongNameGetPublicKey(pwzKeyContainer, pbKeyBlob, cbKeyBlob, ppbPublicKeyBlob, pcbPublicKeyBlob)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                ppbPublicKeyBlob = IntPtr.Zero
                pcbPublicKeyBlob = 0
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameGetPublicKey(ByVal pwzKeyContainer As String, ByVal bKeyBlob As Byte(), ByVal cbKeyBlob As Integer, <Out()> ByRef ppbPublicKeyBlob As IntPtr, <Out()> ByRef pcbPublicKeyBlob As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongName.StrongNameGetPublicKey(pwzKeyContainer, bKeyBlob, cbKeyBlob, ppbPublicKeyBlob, pcbPublicKeyBlob)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                ppbPublicKeyBlob = IntPtr.Zero
                pcbPublicKeyBlob = 0
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameKeyDelete(ByVal pwzKeyContainer As String) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongName.StrongNameKeyDelete(pwzKeyContainer)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameKeyGen(ByVal pwzKeyContainer As String, ByVal dwFlags As Integer, <Out()> ByRef ppbKeyBlob As IntPtr, <Out()> ByRef pcbKeyBlob As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongName.StrongNameKeyGen(pwzKeyContainer, dwFlags, ppbKeyBlob, pcbKeyBlob)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                ppbKeyBlob = IntPtr.Zero
                pcbKeyBlob = 0
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameKeyInstall(ByVal pwzKeyContainer As String, ByVal pbKeyBlob As IntPtr, ByVal cbKeyBlob As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongNameUsingIntPtr.StrongNameKeyInstall(pwzKeyContainer, pbKeyBlob, cbKeyBlob)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameKeyInstall(ByVal pwzKeyContainer As String, ByVal bKeyBlob As Byte(), ByVal cbKeyBlob As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongName.StrongNameKeyInstall(pwzKeyContainer, bKeyBlob, cbKeyBlob)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameSignatureGeneration(ByVal pwzFilePath As String, ByVal pwzKeyContainer As String, ByVal pbKeyBlob As IntPtr, ByVal cbKeyBlob As Integer) As Boolean
            Dim ppbSignatureBlob As IntPtr = IntPtr.Zero
            Dim cbSignatureBlob As Integer = 0
            Return StrongNameHelpers.StrongNameSignatureGeneration(pwzFilePath, pwzKeyContainer, pbKeyBlob, cbKeyBlob, (ppbSignatureBlob), cbSignatureBlob)
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameSignatureGeneration(ByVal pwzFilePath As String, ByVal pwzKeyContainer As String, ByVal bKeyBlob As Byte(), ByVal cbKeyBlob As Integer) As Boolean
            Dim ppbSignatureBlob As IntPtr = IntPtr.Zero
            Dim cbSignatureBlob As Integer = 0
            Return StrongNameHelpers.StrongNameSignatureGeneration(pwzFilePath, pwzKeyContainer, bKeyBlob, cbKeyBlob, (ppbSignatureBlob), cbSignatureBlob)
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameSignatureGeneration(ByVal pwzFilePath As String, ByVal pwzKeyContainer As String, ByVal pbKeyBlob As IntPtr, ByVal cbKeyBlob As Integer, ByRef ppbSignatureBlob As IntPtr, <Out()> ByRef pcbSignatureBlob As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongNameUsingIntPtr.StrongNameSignatureGeneration(pwzFilePath, pwzKeyContainer, pbKeyBlob, cbKeyBlob, ppbSignatureBlob, pcbSignatureBlob)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                pcbSignatureBlob = 0
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameSignatureGeneration(ByVal pwzFilePath As String, ByVal pwzKeyContainer As String, ByVal bKeyBlob As Byte(), ByVal cbKeyBlob As Integer, ByRef ppbSignatureBlob As IntPtr, <Out()> ByRef pcbSignatureBlob As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongName.StrongNameSignatureGeneration(pwzFilePath, pwzKeyContainer, bKeyBlob, cbKeyBlob, ppbSignatureBlob, pcbSignatureBlob)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                pcbSignatureBlob = 0
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameSignatureSize(ByVal pbPublicKeyBlob As IntPtr, ByVal cbPublicKeyBlob As Integer, <Out()> ByRef pcbSize As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongNameUsingIntPtr.StrongNameSignatureSize(pbPublicKeyBlob, cbPublicKeyBlob, pcbSize)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                pcbSize = 0
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameSignatureSize(ByVal bPublicKeyBlob As Byte(), ByVal cbPublicKeyBlob As Integer, <Out()> ByRef pcbSize As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongName.StrongNameSignatureSize(bPublicKeyBlob, cbPublicKeyBlob, pcbSize)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                pcbSize = 0
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameSignatureVerification(ByVal pwzFilePath As String, ByVal dwInFlags As Integer, <Out()> ByRef pdwOutFlags As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongName.StrongNameSignatureVerification(pwzFilePath, dwInFlags, pdwOutFlags)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                pdwOutFlags = 0
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameSignatureVerificationEx(ByVal pwzFilePath As String, ByVal fForceVerification As Boolean, <Out()> ByRef pfWasVerified As Boolean) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongName.StrongNameSignatureVerificationEx(pwzFilePath, fForceVerification, pfWasVerified)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                pfWasVerified = False
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameTokenFromPublicKey(ByVal bPublicKeyBlob As Byte(), ByVal cbPublicKeyBlob As Integer, <Out()> ByRef ppbStrongNameToken As IntPtr, <Out()> ByRef pcbStrongNameToken As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongName.StrongNameTokenFromPublicKey(bPublicKeyBlob, cbPublicKeyBlob, ppbStrongNameToken, pcbStrongNameToken)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                ppbStrongNameToken = IntPtr.Zero
                pcbStrongNameToken = 0
                Return False
            End If
            Return True
        End Function

        <SecurityCritical()> _
        Public Shared Function StrongNameTokenFromPublicKey(ByVal pbPublicKeyBlob As IntPtr, ByVal cbPublicKeyBlob As Integer, <Out()> ByRef ppbStrongNameToken As IntPtr, <Out()> ByRef pcbStrongNameToken As Integer) As Boolean
            Dim hr As Integer = StrongNameHelpers.StrongNameUsingIntPtr.StrongNameTokenFromPublicKey(pbPublicKeyBlob, cbPublicKeyBlob, ppbStrongNameToken, pcbStrongNameToken)
            If (hr < 0) Then
                StrongNameHelpers.ts_LastStrongNameHR = hr
                ppbStrongNameToken = IntPtr.Zero
                pcbStrongNameToken = 0
                Return False
            End If
            Return True
        End Function


        ' Properties
        Private Shared ReadOnly Property StrongName As IClrStrongName
            Get
                If (StrongNameHelpers.s_StrongName Is Nothing) Then
                    StrongNameHelpers.s_StrongName = DirectCast(RuntimeEnvironment.GetRuntimeInterfaceAsObject(New Guid("B79B0ACD-F5CD-409b-B5A5-A16244610B92"), New Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D")), IClrStrongName)
                End If
                Return StrongNameHelpers.s_StrongName
            End Get
        End Property

        Private Shared ReadOnly Property StrongNameUsingIntPtr As IClrStrongNameUsingIntPtr
            Get
                Return DirectCast(StrongNameHelpers.StrongName, IClrStrongNameUsingIntPtr)
            End Get
        End Property


        ' Fields
        Private Const S_OK As Integer = 0
        <SecurityCritical()> _
        <ThreadStatic()> _
        Private Shared s_StrongName As IClrStrongName
        <ThreadStatic()> _
        Private Shared ts_LastStrongNameHR As Integer = 0
    End Class

    <ComImport(), SecurityCritical(), ComConversionLoss(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D")> _
    Friend Interface IClrStrongNameUsingIntPtr
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromAssemblyFile(<[In](), MarshalAs(UnmanagedType.LPStr)> ByVal pszFilePath As String, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromAssemblyFileW(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromBlob(<[In]()> ByVal pbBlob As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchBlob As Integer, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=4)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromFile(<[In](), MarshalAs(UnmanagedType.LPStr)> ByVal pszFilePath As String, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromFileW(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromHandle(<[In]()> ByVal hFile As IntPtr, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameCompareAssemblies(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzAssembly1 As String, <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzAssembly2 As String, <Out(), MarshalAs(UnmanagedType.U4)> ByRef dwResult As Integer) As <MarshalAs(UnmanagedType.U4)> Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameFreeBuffer(<[In]()> ByVal pbMemory As IntPtr) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameGetBlob(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=2)> ByVal pbBlob As Byte(), <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameGetBlobFromImage(<[In]()> ByVal pbBase As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwLength As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbBlob As Byte(), <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameGetPublicKey(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String, <[In]()> ByVal pbKeyBlob As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbKeyBlob As Integer, <Out()> ByRef ppbPublicKeyBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbPublicKeyBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameHashSize(<[In](), MarshalAs(UnmanagedType.U4)> ByVal ulHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef cbSize As Integer) As <MarshalAs(UnmanagedType.U4)> Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameKeyDelete(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameKeyGen(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwFlags As Integer, <Out()> ByRef ppbKeyBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbKeyBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameKeyGenEx(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwFlags As Integer, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwKeySize As Integer, <Out()> ByRef ppbKeyBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbKeyBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameKeyInstall(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String, <[In]()> ByVal pbKeyBlob As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbKeyBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureGeneration(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String, <[In]()> ByVal pbKeyBlob As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbKeyBlob As Integer, <[In](), Out()> ByVal ppbSignatureBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbSignatureBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureGenerationEx(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal wszFilePath As String, <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal wszKeyContainer As String, <[In]()> ByVal pbKeyBlob As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbKeyBlob As Integer, <[In](), Out()> ByVal ppbSignatureBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbSignatureBlob As Integer, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwFlags As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureSize(<[In]()> ByVal pbPublicKeyBlob As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbPublicKeyBlob As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbSize As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureVerification(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwInFlags As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef dwOutFlags As Integer) As <MarshalAs(UnmanagedType.U4)> Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureVerificationEx(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <[In](), MarshalAs(UnmanagedType.I1)> ByVal fForceVerification As Boolean, <Out(), MarshalAs(UnmanagedType.I1)> ByRef fWasVerified As Boolean) As <MarshalAs(UnmanagedType.U4)> Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureVerificationFromImage(<[In]()> ByVal pbBase As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwLength As Integer, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwInFlags As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef dwOutFlags As Integer) As <MarshalAs(UnmanagedType.U4)> Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameTokenFromAssembly(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <Out()> ByRef ppbStrongNameToken As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbStrongNameToken As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameTokenFromAssemblyEx(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <Out()> ByRef ppbStrongNameToken As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbStrongNameToken As Integer, <Out()> ByRef ppbPublicKeyBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbPublicKeyBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameTokenFromPublicKey(<[In]()> ByVal pbPublicKeyBlob As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbPublicKeyBlob As Integer, <Out()> ByRef ppbStrongNameToken As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbStrongNameToken As Integer) As Integer
    End Interface
    <ComImport(), SecurityCritical(), ComConversionLoss(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D")> _
    Friend Interface IClrStrongName
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromAssemblyFile(<[In](), MarshalAs(UnmanagedType.LPStr)> ByVal pszFilePath As String, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromAssemblyFileW(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromBlob(<[In]()> ByVal pbBlob As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchBlob As Integer, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=4)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromFile(<[In](), MarshalAs(UnmanagedType.LPStr)> ByVal pszFilePath As String, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromFileW(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function GetHashFromHandle(<[In]()> ByVal hFile As IntPtr, <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef piHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbHash As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cchHash As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pchHash As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameCompareAssemblies(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzAssembly1 As String, <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzAssembly2 As String, <Out(), MarshalAs(UnmanagedType.U4)> ByRef dwResult As Integer) As <MarshalAs(UnmanagedType.U4)> Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameFreeBuffer(<[In]()> ByVal pbMemory As IntPtr) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameGetBlob(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=2)> ByVal pbBlob As Byte(), <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameGetBlobFromImage(<[In]()> ByVal pbBase As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwLength As Integer, <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbBlob As Byte(), <[In](), Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameGetPublicKey(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String, <[In](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=2)> ByVal pbKeyBlob As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbKeyBlob As Integer, <Out()> ByRef ppbPublicKeyBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbPublicKeyBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameHashSize(<[In](), MarshalAs(UnmanagedType.U4)> ByVal ulHashAlg As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef cbSize As Integer) As <MarshalAs(UnmanagedType.U4)> Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameKeyDelete(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameKeyGen(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwFlags As Integer, <Out()> ByRef ppbKeyBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbKeyBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameKeyGenEx(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwFlags As Integer, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwKeySize As Integer, <Out()> ByRef ppbKeyBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbKeyBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameKeyInstall(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String, <[In](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=2)> ByVal pbKeyBlob As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbKeyBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureGeneration(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzKeyContainer As String, <[In](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbKeyBlob As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbKeyBlob As Integer, <[In](), Out()> ByVal ppbSignatureBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbSignatureBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureGenerationEx(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal wszFilePath As String, <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal wszKeyContainer As String, <[In](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pbKeyBlob As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbKeyBlob As Integer, <[In](), Out()> ByVal ppbSignatureBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbSignatureBlob As Integer, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwFlags As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureSize(<[In](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=1)> ByVal pbPublicKeyBlob As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbPublicKeyBlob As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbSize As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureVerification(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwInFlags As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef dwOutFlags As Integer) As <MarshalAs(UnmanagedType.U4)> Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureVerificationEx(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <[In](), MarshalAs(UnmanagedType.I1)> ByVal fForceVerification As Boolean, <Out(), MarshalAs(UnmanagedType.I1)> ByRef fWasVerified As Boolean) As <MarshalAs(UnmanagedType.U4)> Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameSignatureVerificationFromImage(<[In]()> ByVal pbBase As IntPtr, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwLength As Integer, <[In](), MarshalAs(UnmanagedType.U4)> ByVal dwInFlags As Integer, <Out(), MarshalAs(UnmanagedType.U4)> ByRef dwOutFlags As Integer) As <MarshalAs(UnmanagedType.U4)> Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameTokenFromAssembly(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <Out()> ByRef ppbStrongNameToken As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbStrongNameToken As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameTokenFromAssemblyEx(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal pwzFilePath As String, <Out()> ByRef ppbStrongNameToken As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbStrongNameToken As Integer, <Out()> ByRef ppbPublicKeyBlob As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbPublicKeyBlob As Integer) As Integer
        <PreserveSig(), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        Function StrongNameTokenFromPublicKey(<[In](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=1)> ByVal pbPublicKeyBlob As Byte(), <[In](), MarshalAs(UnmanagedType.U4)> ByVal cbPublicKeyBlob As Integer, <Out()> ByRef ppbStrongNameToken As IntPtr, <Out(), MarshalAs(UnmanagedType.U4)> ByRef pcbStrongNameToken As Integer) As Integer
    End Interface

End Namespace

