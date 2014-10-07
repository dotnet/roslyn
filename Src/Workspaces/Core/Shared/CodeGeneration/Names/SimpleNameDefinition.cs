namespace Roslyn.Services.Shared.CodeGeneration
{
    internal abstract class SimpleNameDefinition : NameDefinition
    {
        public string Identifier { get; private set; }

        protected SimpleNameDefinition(string identifier)
        {
            this.Identifier = identifier;
        }
    }
}