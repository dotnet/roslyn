namespace Roslyn.Compilers
{
    /// <summary>
    /// AssemblySymbol implementation common for C# and VB.
    /// </summary>

    class FullAndPublicKeyResults
    {
        //The strong name key associated with the identity of this assembly. 
        //This contains the contents of the user-supplied key file exactly as extracted.
        internal readonly byte[] m_KeyPair;
        //The CSP key container containing the public key used to produce this assembly identity
        internal readonly string m_KeyContainer;
        internal readonly ReadOnlyArray<byte> m_PublicKey;
        //Any diagnostics that were created in the process of determining the key
        internal readonly DiagnosticBag m_Diagnostics;

        internal readonly string m_KeyFileName;

        internal FullAndPublicKeyResults(byte[] keyPair, ReadOnlyArray<byte> publicKey, string container, DiagnosticBag bag, string keyFileName)
        {
            m_KeyPair = keyPair;
            m_PublicKey = publicKey;
            m_KeyContainer = container;
            m_Diagnostics = bag;
            m_KeyFileName = keyFileName;
            System.Diagnostics.Debug.Assert(m_KeyContainer == null || m_KeyPair == null, "Only one of keyContainer or keyPair can be non-null");
            System.Diagnostics.Debug.Assert((m_KeyPair != null) ? (m_KeyFileName != null) : true, "if m_KeyPair is set, so must m_KeyFileName.");
        }
    }
}
