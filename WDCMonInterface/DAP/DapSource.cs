#pragma warning disable IDE1006 // Naming Styles

using WDCMonInterface.DAP;

namespace WDCMonInterface
{
    class GetSourceArguments
    {
        public DapSource? source { get; set; }
    }

    class GetSourceResponse
    {
        public string? content { get; set; } = null;
        public string? mimeType { get; set; } = null;

        public GetSourceResponse() { }
        public GetSourceResponse(string filename)
        {
            try
            {
                content = File.ReadAllText(filename);
                mimeType = "text/plain";
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading {filename}");
                Logger.Log(ex);
            }
        }
    }

    class DapSource
    {
        public string? name { get; set; }
        public string? path { get; set; }
        public double? sourceReference { get; set; }
        public string? presentationHint { get; set; }
        public string? origin { get; set; }
        public List<DapSource>? sources { get; set; }

        public static bool operator ==(DapSource? me, DapSource? other)
        {
            if (me is null && other is null) return true;
            if (me is null || other is null) return false;
            if (me.path?.ToLower() == other.path?.ToLower()) return true;
            return false;
        }
        public static bool operator !=(DapSource? me, DapSource? other)
        {
            if (me is null && other is null) return false;
            if (me is null || other is null) return true;
            if (me.path?.ToLower() == other.path?.ToLower()) return false;
            return true;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (obj is not DapSource src) return false;
            return this == src;
        }

        public override int GetHashCode()
        {
            return path?.GetHashCode() ?? 0;
        }
    }
}