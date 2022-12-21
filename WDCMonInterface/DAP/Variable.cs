#pragma warning disable IDE1006 // Naming Styles

namespace WDCMonInterface
{
    public class Variable
    {
        public string? name { get; set; }
        public string? value { get; set; }
        public string? type { get; set; }
        public int? namedVariables { get; set; } = 0;
        public int? indexedVariables { get; set; } = 0;
        public int? variablesReference { get; set; } = 0;
        public VariablePresentationHint? presentationHint { get; set; } = new VariablePresentationHint() { };
    }

    public class VariablePresentationHint
    {
        public string? kind { get; set; } = "data";
        public string? attributes { get; set; } = "static";
        public string? visibility { get; set; } = "public";
        public bool? lazy { get; set; } = false;
    }

    class GotoTargetsRequest
    {
        public DapSource? Source { get; set; }
        public int? line { get; set; }
    }
}