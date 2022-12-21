#pragma warning disable IDE1006 // Naming Styles

namespace WDCMonInterface
{
    public class SetVariableRequest
    {
        public int? variablesReference { get; set; }
        public string? name { get; set; }
        public string? value { get; set; }
    }
}