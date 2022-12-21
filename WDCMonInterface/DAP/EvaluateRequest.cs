#pragma warning disable IDE1006 // Naming Styles

namespace WDCMonInterface
{
    public class EvaluateRequest
    {
        public string? expression { get; set; } = null;
        public int? frameId { get; set; } = null;
        public string? context { get; set; } = null;
    }
}