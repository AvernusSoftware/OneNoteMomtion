using System;
using System.Collections.Generic;
using System.Text;

namespace OneNoteMomtion.AddIn.Ipc
{
    internal static class PipeProtocol
    {
        public const string StatusOk = "OK";

        public const string StatusError = "ERR";

        private const char Separator = '\t';

        public static string Encode(params string?[] fields)
        {
            StringBuilder builder = new();

            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(Separator);
                }

                builder.Append(Escape(fields[i]));
            }

            return builder.ToString();
        }

        public static string[] Decode(string? line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return [];
            }

            string[] parts = line!.Split(Separator);
            List<string> result = new(parts.Length);

            foreach (string part in parts)
            {
                result.Add(Unescape(part));
            }

            return [.. result];
        }

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new(value!.Length);

            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        private static string Unescape(string value)
        {
            if (value.IndexOf('\\') < 0)
            {
                return value;
            }

            StringBuilder builder = new(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '\\' || i + 1 >= value.Length)
                {
                    builder.Append(value[i]);
                    continue;
                }

                i++;
                switch (value[i])
                {
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case '\\':
                        builder.Append('\\');
                        break;
                    default:
                        builder.Append('\\').Append(value[i]);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
