using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDCMonInterface.Devices;

namespace WdcMon6502DebugAdapter
{
    internal class Monitor
    {
        static string? GetToken(string str, out string token, params string[] specialTokens)
        {
            int i = int.MaxValue;
            token = "";
            foreach (var t in specialTokens)
            {
                int index = str.IndexOf(t, StringComparison.InvariantCultureIgnoreCase);
                if (index > 0 && index < i)
                {
                    i = index;
                    token = str[0..i];
                }
                else if (index == 0)
                {
                    i = 0;
                    token = str[0..t.Length];
                }
            }

            if (token.Length == 0)
            {
                token = str;
                return null;
            }

            return str[token.Length..];
        }

        static bool ParseAsAddress(string s, out ushort value)
        {
            System.Globalization.NumberStyles numberStyle = System.Globalization.NumberStyles.Any;
            if (s.StartsWith("$"))
            {
                s = s[1..];
                numberStyle = System.Globalization.NumberStyles.HexNumber;
            }
            else if (s.StartsWith("0x"))
            {
                s = s[2..];
                numberStyle = System.Globalization.NumberStyles.HexNumber;
            }

            bool b = ushort.TryParse(s, numberStyle, System.Globalization.CultureInfo.InvariantCulture, out value);
            return b;
        }

        static bool ParseAsData(string s, out byte value)
        {
            System.Globalization.NumberStyles numberStyle = System.Globalization.NumberStyles.Any;
            if (s.StartsWith("$"))
            {
                s = s[1..];
                numberStyle = System.Globalization.NumberStyles.HexNumber;
            }
            else if (s.StartsWith("0x"))
            {
                s = s[2..];
                numberStyle = System.Globalization.NumberStyles.HexNumber;
            }

            bool b = byte.TryParse(s, numberStyle, System.Globalization.CultureInfo.InvariantCulture, out value);
            return b;
        }

        public static string PerformAction(ISystemInterface system, string? str)
        {
            List<string> Tokenized = new List<string>();
            while (str != null && str.Length > 0)
            {
                str = GetToken(str, out string nextToken, ":", "-", " ", "\t");
                if (!String.IsNullOrWhiteSpace(nextToken))
                {
                    Tokenized.Add(nextToken.Trim());
                }
            }

            if (Tokenized.Count > 1 && ParseAsAddress(Tokenized[0], out ushort startAddress) && Tokenized[1] == ":")
            {
                List<byte> data = new List<byte>();
                for (int i = 2; i < Tokenized.Count; ++i)
                {
                    if (!ParseAsData(Tokenized[i], out byte value))
                    {
                        return "No data?";
                    }
                    data.Add(value);
                }

                system.WriteMemory(startAddress, data.ToArray());
                return "OK.";
            }
            else if (Tokenized.Count > 0 && ParseAsAddress(Tokenized[0], out ushort readStart))
            {
                ushort readEnd = readStart;
                if (Tokenized.Count >= 3 && Tokenized[1] == "-" && ParseAsAddress(Tokenized[2], out ushort _end))
                {
                    readEnd = _end;
                }

                if (readEnd < readStart)
                {
                    var tmp = readEnd;
                    readEnd = readStart;
                    readStart = tmp;
                }

                var length = (readEnd - readStart) + 1;
                if (length <= 0) length = 1;
                var data = system.ReadMemory(readStart, (ushort)length);
                StringBuilder result = new();
                for (int i = 0; i < data.Length; ++i)
                {
                    if (i % 16 == 0 && data.Length > 1)
                    {
                        var currAddress = readStart + i;
                        result.Append($"${currAddress:X4} ");
                    }

                    result.Append($"${data[i]:X2} ");
                    if (i % 16 == 15)
                    {
                        result.AppendLine();
                    }
                }
                return result.ToString();
            }
            else return "bad command";

        }
    }
}
