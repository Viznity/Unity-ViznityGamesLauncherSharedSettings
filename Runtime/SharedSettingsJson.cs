using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Viznity.SharedSettings
{
    /// <summary>
    /// A small, strict JSON reader for the shared settings file. <c>JsonUtility</c> cannot read
    /// objects into dictionaries or report which keys exist, and settings added by a newer launcher
    /// must be readable without a new version of this package, so the file is parsed generically:
    /// objects become <c>Dictionary&lt;string, object&gt;</c>, arrays <c>List&lt;object&gt;</c>,
    /// numbers <c>double</c>, plus <c>string</c>, <c>bool</c> and <c>null</c>.
    /// Malformed input throws <see cref="FormatException"/>; nesting is capped so a hostile file cannot overflow the stack.
    /// </summary>
    public static class SharedSettingsJson
    {
        private const int MaxDepth = 32;

        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var reader = new Reader(json);
            reader.SkipWhitespace();
            object value = reader.ReadValue(0);
            reader.SkipWhitespace();
            if (!reader.AtEnd) throw reader.Error("unexpected text after the JSON value");
            return value;
        }

        private sealed class Reader
        {
            private readonly string _text;
            private int _pos;

            public Reader(string text)
            {
                _text = text;
                // A UTF-8 byte order mark read as text.
                if (_text.Length > 0 && _text[0] == '﻿') _pos = 1;
            }

            public bool AtEnd => _pos >= _text.Length;

            public FormatException Error(string message) => new FormatException($"Invalid JSON at {_pos}: {message}");

            public void SkipWhitespace()
            {
                while (_pos < _text.Length && (_text[_pos] == ' ' || _text[_pos] == '\t' || _text[_pos] == '\n' || _text[_pos] == '\r')) _pos++;
            }

            public object ReadValue(int depth)
            {
                if (depth > MaxDepth) throw Error("nested too deeply");
                if (AtEnd) throw Error("unexpected end");
                char c = _text[_pos];
                switch (c)
                {
                    case '{': return ReadObject(depth);
                    case '[': return ReadArray(depth);
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                        throw Error($"unexpected '{c}'");
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_text, _pos, word, 0, word.Length) != 0) throw Error($"expected {word}");
                _pos += word.Length;
            }

            private Dictionary<string, object> ReadObject(int depth)
            {
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                _pos++; // {
                SkipWhitespace();
                if (!AtEnd && _text[_pos] == '}') { _pos++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _text[_pos] != '"') throw Error("expected a key");
                    string key = ReadString();
                    SkipWhitespace();
                    if (AtEnd || _text[_pos] != ':') throw Error("expected ':'");
                    _pos++;
                    SkipWhitespace();
                    result[key] = ReadValue(depth + 1); // last one wins, like the launcher's serde_json
                    SkipWhitespace();
                    if (AtEnd) throw Error("unexpected end in object");
                    if (_text[_pos] == ',') { _pos++; continue; }
                    if (_text[_pos] == '}') { _pos++; return result; }
                    throw Error("expected ',' or '}'");
                }
            }

            private List<object> ReadArray(int depth)
            {
                var result = new List<object>();
                _pos++; // [
                SkipWhitespace();
                if (!AtEnd && _text[_pos] == ']') { _pos++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    result.Add(ReadValue(depth + 1));
                    SkipWhitespace();
                    if (AtEnd) throw Error("unexpected end in array");
                    if (_text[_pos] == ',') { _pos++; continue; }
                    if (_text[_pos] == ']') { _pos++; return result; }
                    throw Error("expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                _pos++; // opening quote
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd) throw Error("unterminated string");
                    char c = _text[_pos++];
                    if (c == '"') return sb.ToString();
                    if (c < ' ') throw Error("control character in string");
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw Error("unterminated escape");
                    char e = _text[_pos++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_pos + 4 > _text.Length) throw Error("short \\u escape");
                            if (!ushort.TryParse(_text.Substring(_pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                                throw Error("bad \\u escape");
                            sb.Append((char)code);
                            _pos += 4;
                            break;
                        default: throw Error($"unknown escape '\\{e}'");
                    }
                }
            }

            private double ReadNumber()
            {
                int start = _pos;
                if (_text[_pos] == '-') _pos++;
                while (!AtEnd && "0123456789+-.eE".IndexOf(_text[_pos]) >= 0) _pos++;
                string token = _text.Substring(start, _pos - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) throw Error($"bad number '{token}'");
                return value;
            }
        }
    }
}
