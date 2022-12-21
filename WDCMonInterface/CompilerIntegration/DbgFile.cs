using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDCMonInterface.CompilerIntegration
{
    public class DbgFile
    {
        public class DbgFileLine
        {
            public string Type;
            public Dictionary<string, string> Parameters;

            public DbgFileLine(string line)
            {
                string[] parts = line.Split(' ', '\t');
                if (parts.Length != 2) throw new Exception($"Invalid line \"{line}\"");
                Type = parts[0].ToLower();
                Parameters = parts[1].Split(',').Where(s => s.Length > 0).Select(s => s.Split('=').ToArray()).ToDictionary(s => s[0].ToLower(), s => s[1]);
            }

            public long GetLong(string key, long defaultValue = -1)
            {
                string? sValue = GetString(key, defaultValue.ToString());
                if (sValue == null) return defaultValue;
                System.Globalization.NumberStyles style = System.Globalization.NumberStyles.None;
                if (sValue.ToLower().StartsWith("0x"))
                {
                    sValue = sValue[2..];
                    style = System.Globalization.NumberStyles.HexNumber;
                }
                if (long.TryParse(sValue, style, System.Globalization.CultureInfo.InvariantCulture, out long i)) return i;
                return defaultValue;
            }

            public int GetInt(string key, int defaultValue = -1) => (int)GetLong(key, defaultValue);

            public string? GetString(string key, string? defaultValue = null)
            {
                if (Parameters.TryGetValue(key.ToLower(), out string? s)) return s?.Trim('\"');
                return defaultValue;
            }
        }

        public class FileInfo
        {
            public string? Name;
            public int ID;
            public int Size;
            public long modified_time;
            public int module_id;
        }

        public class ModuleInfo
        {
            public string? Name;
            public int ID;
            public int file_id;
        }

        public class LineInfo
        {
            public int ID;
            public int file_id;
            public int line_number;
            public int? span;
        }

        public class SpanInfo
        {
            public int ID;
            public int segment_id;
            public int start;
            public int size;
        }

        public class SegmentInfo
        {
            public int ID;
            public string? Name;
            public int Start;
            public int Size;
            public string? addrsize;
            public string? type;
            public string? oname;
            public int ooffs;
        }

        public class ScopeInfo
        {
            public int ID;
            public string? Name;
            public int module_id;
            public int size;
            public int span_id;
        }

        public class SymbolInfo
        {
            public int ID;
            public string? Name;
            public string? addrsize;
            public int size;
            public int scope_id;
            public int parent_symbol_id;
            public int defining_line_id;
            public int value;
            public string? type;
        }

        public DbgFileLine? Info;
        public Dictionary<int, FileInfo> Files;
        public Dictionary<int, ModuleInfo> Modules;
        public Dictionary<int, LineInfo> Lines;
        public Dictionary<int, SpanInfo> Spans;
        public Dictionary<int, SegmentInfo> Segments;
        public Dictionary<int, ScopeInfo> Scopes;
        public Dictionary<int, SymbolInfo> Symbols;

        public DbgFile(string filename)
        {
            var s_lines = File.ReadAllLines(filename).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            var lines = s_lines.Select(s => new DbgFileLine(s)).ToArray();
            var version = lines.FirstOrDefault(l => l.Type == "version");
            Info = lines.FirstOrDefault(l => l.Type == "info");

            Files = lines.Where(l => l.Type == "file").Select(s => new FileInfo() { ID = s.GetInt("id"), Name = s.GetString("name")?.ToLower()?.Replace("\\", "/"), module_id = s.GetInt("mod"), modified_time = s.GetLong("mtime"), Size = s.GetInt("size") }).ToDictionary(s => s.ID, s => s);
            Modules = lines.Where(l => l.Type == "mod").Select(s => new ModuleInfo() { ID = s.GetInt("id"), Name = s.GetString("name"), file_id = s.GetInt("file") }).ToDictionary(s => s.ID, s => s);
            Lines = lines.Where(l => l.Type == "line").Select(s => new LineInfo() { ID = s.GetInt("id"), file_id = s.GetInt("file"), line_number = s.GetInt("line"), span = s.GetInt("span", -1) < 0 ? null : s.GetInt("span", -1) }).ToDictionary(s => s.ID, s => s);
            Spans = lines.Where(l => l.Type == "span").Select(s => new SpanInfo() { ID = s.GetInt("id"), segment_id = s.GetInt("seg"), size = s.GetInt("size", 1), start = s.GetInt("start") }).ToDictionary(s => s.ID, s => s);
            Segments = lines.Where(l => l.Type == "seg").Select(s => new SegmentInfo() { ID = s.GetInt("id"), Name = s.GetString("name"), Start = s.GetInt("start"), addrsize = s.GetString("addrsize"), type = s.GetString("type"), oname = s.GetString("oname"), ooffs = s.GetInt("ooffs"), Size = s.GetInt("size") }).ToDictionary(s => s.ID, s => s);
            Scopes = lines.Where(l => l.Type == "scope").Select(s => new ScopeInfo() { ID = s.GetInt("id"), Name = s.GetString("name"), module_id = s.GetInt("mod"), size = s.GetInt("size"), span_id = s.GetInt("span") }).ToDictionary(s => s.ID, s => s);
            Symbols = lines.Where(l => l.Type == "sym").Select(s => new SymbolInfo() { ID = s.GetInt("id"), Name = s.GetString("name"), addrsize = s.GetString("addrsize"), scope_id = s.GetInt("scope", -1), defining_line_id = s.GetInt("def"), value = s.GetInt("val"), parent_symbol_id = s.GetInt("parent", -1), size = s.GetInt("size", -1), type = s.GetString("type") }).ToDictionary(s => s.ID, s => s);

        }

        static bool SegmentContains(SegmentInfo segment, ushort address)
        {
            if (address < segment.Start) return false;
            var relative_address = address - segment.Start;
            if (relative_address < segment.Size) return true;
            else return false;
        }

        bool SpanContains(SpanInfo span, ushort address)
        {
            if (!Segments.TryGetValue(span.segment_id, out SegmentInfo? segment) || segment == null) return false;
            if (!SegmentContains(segment, address)) return false;
            var segment_relative_address = address - segment.Start;
            if (segment_relative_address < span.start) return false;
            var relative_address = segment_relative_address - span.start;
            if (relative_address < span.size) return true;
            else return false;
        }

        bool LineContains(LineInfo line, ushort address)
        {
            if (line.span == null || !Spans.TryGetValue(line.span.Value, out SpanInfo? span) || span == null) return false;
            return SpanContains(span, address);
        }

        bool ScopeContains(ScopeInfo scope, ushort address)
        {
            if (!Spans.TryGetValue(scope.span_id, out SpanInfo? span) || span == null) return false;
            return SpanContains(span, address);
        }

        public LineInfo? LineFromAddress(ushort address) => Lines.Select(s => s.Value).FirstOrDefault(l => LineContains(l, address));
        public ScopeInfo? ScopeFromAddress(ushort address) => Scopes.Select(s => s.Value).FirstOrDefault(s => ScopeContains(s, address));

        public string FileName(LineInfo? li)
        {
            if (li == null || !Files.TryGetValue(li.file_id, out FileInfo? file) || file == null) return "";
            return file.Name ?? "";
        }

        bool SymbolInScope(SymbolInfo sym, ScopeInfo scope)
        {
            if (sym.parent_symbol_id >= 0)
            {
                if (sym.parent_symbol_id == sym.ID) return false;
                if (!Symbols.TryGetValue(sym.parent_symbol_id, out SymbolInfo? parent) || parent == null) return false;
                return SymbolInScope(parent, scope);
            }

            if (sym.scope_id >= 0)
            {
                return sym.scope_id == scope.ID;
            }

            return false;
        }

        public string SymbolUniqueName(SymbolInfo sym)
        {
            if (sym.parent_symbol_id >= 0)
            {
                if (sym.parent_symbol_id != sym.ID)
                {
                    if (Symbols.TryGetValue(sym.parent_symbol_id, out SymbolInfo? parent) && parent != null)
                    {
                        return $"{SymbolUniqueName(parent)}::{sym.Name}";
                    }
                }
            }

            if (Scopes.TryGetValue(sym.scope_id, out ScopeInfo? si) && si != null)
            {
                return $"{si.Name}::{sym.Name}";
            }

            return $"UNKNOWN_SCOPE::{sym.Name}";
        }

        public List<SymbolInfo> SymbolsInScope(ScopeInfo scope) => Symbols.Select(s => s.Value).Where(s => SymbolInScope(s, scope)).ToList();

        public LineInfo? GetLine(string filename, int LineNumber)
        {
            int? fileNumber = Files.Select(s => s.Value).Where(s => s?.Name?.ToLower() == filename.ToLower().Replace("\\", "/")).Select(s => (int?)s.ID).FirstOrDefault();
            if (fileNumber == null || fileNumber < 0) return null;

            var Line = Lines.Select(s => s.Value).Where(s => s.file_id == fileNumber.Value && s.line_number >= LineNumber && s.span != null).OrderBy(s => s.line_number).FirstOrDefault();
            return Line;
        }

        public ushort AddressFromLine(LineInfo Line)
        {
            if (!Spans.TryGetValue(Line.span ?? 0, out SpanInfo? Span) || Span == null) return 0;
            if (!Segments.TryGetValue(Span.segment_id, out SegmentInfo? Segment) || Segment == null) return 0;
            return (ushort)(Segment.Start + Span.start);
        }
    }
}
