using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Assets.logic
{
    internal static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return Parser.Parse(json);
        }

        private sealed class Parser : IDisposable
        {
            private readonly StringReader _reader;

            private Parser(string json)
            {
                _reader = new StringReader(json);
            }

            public void Dispose()
            {
                _reader.Dispose();
            }

            public static object Parse(string json)
            {
                using var parser = new Parser(json);
                return parser.ParseValue();
            }

            private object ParseValue()
            {
                EatWhitespace();
                if (Peek == -1)
                {
                    return null;
                }

                switch (PeekChar)
                {
                    case '{':
                        return ParseObject();
                    case '[':
                        return ParseArray();
                    case '"':
                        return ParseString();
                    case 't':
                    case 'f':
                        return ParseLiteral();
                    case 'n':
                        ParseLiteral();
                        return null;
                    default:
                        if (char.IsDigit(PeekChar) || PeekChar == '-' || PeekChar == '+')
                        {
                            return ParseNumber();
                        }

                        return ParseString();
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                NextChar(); // consume '{'

                while (true)
                {
                    EatWhitespace();
                    if (Peek == -1)
                    {
                        break;
                    }

                    if (PeekChar == '}')
                    {
                        NextChar();
                        break;
                    }

                    var key = ParseString();
                    EatWhitespace();

                    if (PeekChar == ':')
                    {
                        NextChar();
                        var value = ParseValue();
                        table[key] = value;
                    }

                    EatWhitespace();
                    if (PeekChar == ',')
                    {
                        NextChar();
                        continue;
                    }

                    if (PeekChar == '}')
                    {
                        NextChar();
                        break;
                    }
                }

                return table;
            }

            private List<object> ParseArray()
            {
                var array = new List<object>();
                NextChar(); // consume '['

                while (true)
                {
                    EatWhitespace();
                    if (Peek == -1)
                    {
                        break;
                    }

                    if (PeekChar == ']')
                    {
                        NextChar();
                        break;
                    }

                    var value = ParseValue();
                    array.Add(value);

                    EatWhitespace();
                    if (PeekChar == ',')
                    {
                        NextChar();
                        continue;
                    }

                    if (PeekChar == ']')
                    {
                        NextChar();
                        break;
                    }
                }

                return array;
            }

            private string ParseString()
            {
                if (PeekChar == '"')
                {
                    NextChar();
                }

                var result = new StringBuilder();
                while (Peek != -1)
                {
                    var ch = NextChar();
                    if (ch == '"')
                    {
                        break;
                    }

                    if (ch == '\\')
                    {
                        if (Peek == -1)
                        {
                            break;
                        }

                        var escape = NextChar();
                        switch (escape)
                        {
                            case '"':
                            case '\\':
                            case '/':
                                result.Append(escape);
                                break;
                            case 'b':
                                result.Append('\b');
                                break;
                            case 'f':
                                result.Append('\f');
                                break;
                            case 'n':
                                result.Append('\n');
                                break;
                            case 'r':
                                result.Append('\r');
                                break;
                            case 't':
                                result.Append('\t');
                                break;
                            case 'u':
                                var hex = new char[4];
                                for (var i = 0; i < 4; i++)
                                {
                                    hex[i] = NextChar();
                                }

                                result.Append((char)Convert.ToInt32(new string(hex), 16));
                                break;
                        }
                    }
                    else
                    {
                        result.Append(ch);
                    }
                }

                return result.ToString();
            }

            private object ParseLiteral()
            {
                var word = new StringBuilder();

                while (Peek != -1 && char.IsLetter(PeekChar))
                {
                    word.Append(NextChar());
                }

                var literal = word.ToString();
                return literal switch
                {
                    "true" => true,
                    "false" => false,
                    "null" => null,
                    _ => literal
                };
            }

            private object ParseNumber()
            {
                var number = new StringBuilder();

                if (PeekChar == '-' || PeekChar == '+')
                {
                    number.Append(NextChar());
                }

                while (Peek != -1 && (char.IsDigit(PeekChar) || PeekChar == '.'))
                {
                    number.Append(NextChar());
                }

                var numberString = number.ToString();
                if (double.TryParse(numberString, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    return parsed;
                }

                return 0d;
            }

            private void EatWhitespace()
            {
                while (Peek != -1 && char.IsWhiteSpace(PeekChar))
                {
                    NextChar();
                }
            }

            private int Peek => _reader.Peek();

            private char PeekChar => Convert.ToChar(_reader.Peek());

            private char NextChar()
            {
                return Convert.ToChar(_reader.Read());
            }
        }
    }
}
