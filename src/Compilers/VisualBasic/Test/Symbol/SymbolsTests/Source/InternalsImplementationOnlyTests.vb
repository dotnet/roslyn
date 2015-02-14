' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class InternalsImplementationOnlyTests
        Inherits BasicTestBase

        Public Sub TestInternalImplementationOnly()
            Dim ref = MetadataReference.CreateFromAssembly(GetType(System.Runtime.CompilerServices.InternalsVisibleToAttribute).Assembly)
            Const aSource = "<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""B"")>
Namespace System.Runtime.CompilerServices
    Friend NotInheritable Class InternalImplementationOnlyAttribute
        Inherits System.Attribute
    End Class
End Namespace
<System.Runtime.CompilerServices.InternalImplementationOnly>
Public Interface IA1 : End Interface
Public Interface IA2 : End Interface
"
            Dim compa = CreateVisualBasicCompilation(aSource, assemblyName:="A", referencedAssemblies:={ref})
            compa.VerifyDiagnostics()
            Const bSource = "<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""C"")>
Public Interface IB1 : Inherits IA1 : End Interface
Public Interface IB2 : Inherits IA2 : End Interface
Public Class B1 : Implements IA1 : End Class
Public Class B2 : Implements IA2 : End Class
"
            Dim compb = CreateVisualBasicCompilation(code:=bSource, assemblyName:="B", referencedAssemblies:={ref}, referencedCompilations:={compa})
            compb.VerifyDiagnostics()
            Const cSource = "Public Interface IC1 : Inherits IA1 : End Interface '' error
Public Interface IC2 : Inherits IA2 : End Interface
Public Interface IC3 : Inherits IB1 : End Interface
Public Interface IC4 : Inherits IB2 : End Interface
Public Interface IC5 : Inherits IB1, IA1 : End Interface '' error
Public Interface IC6 : Inherits IB2, IA2 : End Interface
Public Class C1 : Inherits B1 : End Class
Public Class C2 : Inherits B2 : End Class
"
            Dim compc = CreateVisualBasicCompilation(code:=cSource, assemblyName:="C", referencedAssemblies:={ref}, referencedCompilations:={compa, compb})
            compc.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InternalImplementationOnly, "IC1").WithArguments("IC1", "IA1").WithLocation(1, 18),
                Diagnostic(ERRID.ERR_InternalImplementationOnly, "IC5").WithArguments("IC5", "IA1").WithLocation(5, 18))
        End Sub
    End Class
End Namespace
