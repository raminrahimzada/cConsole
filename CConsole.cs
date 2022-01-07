using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace raminrahimzada
{
    public class CConsole : IFormatProvider, ICustomFormatter
    {
        private const char ColorSeparator = ':';
        private const char Separator = (char)65535;//non-printable character

        private static readonly Type CustomFormatterType;
        private static readonly Dictionary<string, ConsoleColor> Colors;
        private static readonly CConsole This = new();

        static CConsole()
        {
            CustomFormatterType = typeof(ICustomFormatter);
            Colors = Enum.GetValues<ConsoleColor>().ToDictionary(x => x.ToString().ToLowerInvariant(), x => x);
        }

        public object GetFormat(Type formatType)
        {
            return CustomFormatterType == formatType ? this : null;
        }

        private static ConsoleColor? GetColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) return null;
            if (Colors.TryGetValue(color.Trim().ToLowerInvariant(), out var consoleColor))
            {
                return consoleColor;
            }

            throw new Exception($"System.ConsoleColor enum does not have a member named {color}");
        }

        private static (ConsoleColor? foreground, ConsoleColor? background)? GetColors(string colors)
        {
            if (string.IsNullOrWhiteSpace(colors)) return null;
            if (colors.Contains(ColorSeparator))
            {
                var split = colors.Split(ColorSeparator);
                var foreground = GetColor(split[0]);
                var background = GetColor(split[1]);
                return (foreground, background);
            }

            var single = GetColor(colors);
            return (single, null);
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (format == null) return arg?.ToString();
            var sb = new StringBuilder();
            sb.Append(Separator);
            sb.Append(format);
            sb.Append(Separator);
            sb.Append(arg);
            sb.Append(Separator);
            return sb.ToString();
        }
         
        public static void WriteLine(FormattableString f)
        {
            Write(f);
            Console.WriteLine();
        }

        public static void Write(FormattableString f)
        {
            lock (This)
            {
                WriteInternal(f);
            }
        }

        private static void WriteInternal(FormattableString f)
        {
            var str = f.ToString(This);
            var sb = new StringBuilder();
            var format = new StringBuilder();
            var arg = new StringBuilder();
            bool? state = null;//null->normal,true->separator start,2->separator end

            var defaultForegroundColor = Console.ForegroundColor;
            var defaultBackgroundColor = Console.BackgroundColor;

            void Print(ConsoleColor? foreground, ConsoleColor? background, string text)
            {
                Console.ForegroundColor = foreground ?? defaultForegroundColor;
                Console.BackgroundColor = background ?? defaultBackgroundColor;
                Console.Write(text);
            }

            foreach (var ch in str)
            {
                switch (state)
                {
                    case null when ch == Separator:
                        Print(null, null, sb.ToString());
                        sb.Clear();
                        state = true;
                        break;
                    case null:
                        sb.Append(ch);
                        break;
                    case true when ch == Separator:
                        state = false;
                        break;
                    case true:
                        format.Append(ch);
                        break;
                    case false when ch == Separator:
                        state = null;
                        var colors = GetColors(format.ToString());
                        Print(colors?.foreground, colors?.background, arg.ToString());
                        format.Clear();
                        arg.Clear();
                        break;
                    case false:
                        arg.Append(ch);
                        break;
                }
            }

            if (format.Length > 0 && arg.Length > 0)
            {
                var colors = GetColors(format.ToString());
                Print(colors?.foreground, colors?.background, arg.ToString());
                format.Clear();
            }

            if (sb.Length > 0)
            {
                Print(null, null, sb.ToString());
            }

            Console.ForegroundColor = defaultForegroundColor;
            Console.BackgroundColor = defaultBackgroundColor;
        }
    }
}
