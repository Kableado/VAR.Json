﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace VAR.Json
{
    public class JsonParser
    {
        #region Declarations

        private const int MaxRecursiveCount = 20;

        private ParserContext _ctx;
        private bool _tainted;

        private readonly List<Type> _knownTypes = new List<Type>();

        #endregion Declarations

        #region Properties

        public bool Tainted => _tainted;

        public List<Type> KnownTypes => _knownTypes;

        #endregion Properties

        #region Private methods

        private static readonly Dictionary<Type, PropertyInfo[]> _dictProperties =
            new Dictionary<Type, PropertyInfo[]>();

        private PropertyInfo[] Type_GetProperties(Type type)
        {
            PropertyInfo[] typeProperties;
            lock (_dictProperties)
            {
                if (_dictProperties.ContainsKey(type)) { typeProperties = _dictProperties[type]; }
                else
                {
                    typeProperties = type.GetProperties(BindingFlags.Public | BindingFlags.OptionalParamBinding |
                                                        BindingFlags.Instance);
                    _dictProperties.Add(type, typeProperties);
                }
            }

            return typeProperties;
        }

        private float CompareToType(Dictionary<string, object> obj, Type type)
        {
            PropertyInfo[] typeProperties = Type_GetProperties(type);
            int count = 0;
            foreach (PropertyInfo prop in typeProperties)
            {
                if (obj.ContainsKey(prop.Name))
                {
                    count++;
                }
            }

            return count / (float)typeProperties.Length;
        }

        private object ConvertToType(Dictionary<string, object> obj, Type type)
        {
            PropertyInfo[] typeProperties = Type_GetProperties(type);
            object newObj = ObjectActivator.CreateInstance(type);
            foreach (PropertyInfo prop in typeProperties)
            {
                if (obj.ContainsKey(prop.Name))
                {
                    Type underliningType = Nullable.GetUnderlyingType(prop.PropertyType);
                    Type effectiveType = underliningType ?? prop.PropertyType;
                    object valueOrig = obj[prop.Name];
                    object valueDest;
                    if (underliningType != null && valueOrig == null)
                    {
                        valueDest = null;
                    }
                    else if (effectiveType == typeof(Guid) && valueOrig is string valGuid)
                    {
                        valueDest = new Guid(valGuid);
                    }
                    else
                    {
                        try
                        {
                            valueDest = Convert.ChangeType(obj[prop.Name], effectiveType);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    prop.SetValue(newObj, valueDest, null);
                }
            }

            return newObj;
        }

        private object TryConvertToTypes(Dictionary<string, object> obj)
        {
            Type bestMatch = null;
            float bestMatchFactor = 0.0f;
            foreach (Type type in _knownTypes)
            {
                float matchFactor = CompareToType(obj, type);
                if (matchFactor > bestMatchFactor)
                {
                    bestMatch = type;
                    bestMatchFactor = matchFactor;
                }
            }

            if (bestMatch != null)
            {
                try
                {
                    object newObj = ConvertToType(obj, bestMatch);
                    return newObj;
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return obj;
        }

        private int ParseHexShort()
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                char c = _ctx.Next();
                if (char.IsDigit(c))
                {
                    value = (value << 4) | (c - '0');
                }
                else
                {
                    c = char.ToLower(c);
                    if (c >= 'a' && c <= 'f')
                    {
                        value = (value << 4) | ((c - 'a') + 10);
                    }
                }
            }

            return value;
        }

        private string ParseQuotedString()
        {
            StringBuilder scratch = new StringBuilder();
            char c = _ctx.SkipWhite();
            if (c == '"')
            {
                c = _ctx.Next();
            }

            do
            {
                if (c == '\\')
                {
                    c = _ctx.Next();
                    if (c == '"')
                    {
                        scratch.Append('"');
                    }
                    else if (c == '\\')
                    {
                        scratch.Append('\\');
                    }
                    else if (c == '/')
                    {
                        scratch.Append('/');
                    }
                    else if (c == 'b')
                    {
                        scratch.Append('\b');
                    }
                    else if (c == 'f')
                    {
                        scratch.Append('\f');
                    }
                    else if (c == 'n')
                    {
                        scratch.Append('\n');
                    }
                    else if (c == 'r')
                    {
                        scratch.Append('\r');
                    }
                    else if (c == 't')
                    {
                        scratch.Append('\t');
                    }
                    else if (c == 'u')
                    {
                        scratch.Append((char)ParseHexShort());
                    }
                    else
                    {
                        // StrictRules: Mark as tainted on unknown escaped character
                        _tainted = true;
                    }

                    c = _ctx.Next();
                }
                else if (c == '"')
                {
                    _ctx.Next();
                    break;
                }
                else
                {
                    // StrictRules: Mark as tainted on illegal characters
                    if (c == '\t' || c == '\n') { _tainted = true; }

                    scratch.Append(c);
                    c = _ctx.Next();
                }
            } while (!_ctx.AtEnd());

            return scratch.ToString();
        }

        private string ParseSingleQuotedString()
        {
            StringBuilder scratch = new StringBuilder();
            char c = _ctx.SkipWhite();
            if (c == '\'')
            {
                c = _ctx.Next();
            }

            do
            {
                if (c == '\\')
                {
                    c = _ctx.Next();
                    if (c == '\'')
                    {
                        scratch.Append('\'');
                    }
                    else if (c == '\\')
                    {
                        scratch.Append('\\');
                    }
                    else if (c == '/')
                    {
                        scratch.Append('/');
                    }
                    else if (c == 'b')
                    {
                        scratch.Append('\b');
                    }
                    else if (c == 'f')
                    {
                        scratch.Append('\f');
                    }
                    else if (c == 'n')
                    {
                        scratch.Append('\n');
                    }
                    else if (c == 'r')
                    {
                        scratch.Append('\r');
                    }
                    else if (c == 't')
                    {
                        scratch.Append('\t');
                    }
                    else if (c == 'u')
                    {
                        scratch.Append((char)ParseHexShort());
                    }
                    else
                    {
                        // StrictRules: Mark as tainted on unknown escaped character
                        _tainted = true;
                    }

                    c = _ctx.Next();
                }
                else if (c == '\'')
                {
                    _ctx.Next();
                    break;
                }
                else
                {
                    // StrictRules: Mark as tainted on illegal characters
                    if (c == '\t' || c == '\n') { _tainted = true; }

                    scratch.Append(c);
                    c = _ctx.Next();
                }
            } while (!_ctx.AtEnd());

            return scratch.ToString();
        }

        private string ParseString(bool mustBeQuoted = false)
        {
            char c = _ctx.SkipWhite();
            if (c == '"')
            {
                return ParseQuotedString();
            }

            if (c == '\'')
            {
                _tainted = true;
                return ParseSingleQuotedString();
            }

            if (mustBeQuoted) { _tainted = true; }

            StringBuilder scratch = new StringBuilder();

            while (!_ctx.AtEnd()
                   && (char.IsLetter(c) || char.IsDigit(c) || c == '_'))
            {
                scratch.Append(c);
                c = _ctx.Next();
            }

            return scratch.ToString();
        }

        private object ParseNumber()
        {
            StringBuilder scratch = new StringBuilder();
            bool isFloat = false;
            bool isExp = false;
            int numberLenght = 0;
            int expLenght = 0;
            char c = _ctx.SkipWhite();

            // Sign
            if (c == '-')
            {
                scratch.Append('-');
                c = _ctx.Next();
            }

            // Integer part
            bool leadingZeroes = true;
            int leadingZeroesLenght = 0;
            while (char.IsDigit(c))
            {
                // Count leading zeroes
                if (leadingZeroes && c == '0') { leadingZeroesLenght++; }
                else { leadingZeroes = false; }

                scratch.Append(c);
                c = _ctx.Next();
                numberLenght++;
            }

            // StrictRules: Mark as tainted with leading zeroes
            if ((leadingZeroesLenght > 0 && leadingZeroesLenght != numberLenght) || leadingZeroesLenght > 1)
            {
                _tainted = true;
            }

            // Decimal part
            if (c == '.')
            {
                isFloat = true;
                scratch.Append(".");
                c = _ctx.Next();
                while (char.IsDigit(c))
                {
                    scratch.Append(c);
                    c = _ctx.Next();
                    numberLenght++;
                }
            }

            if (numberLenght == 0)
            {
                _tainted = true;
                return null;
            }

            // Exponential part
            if (c == 'e' || c == 'E')
            {
                isFloat = true;
                isExp = true;
                scratch.Append('E');
                c = _ctx.Next();
                if (c == '+' || c == '-')
                {
                    scratch.Append(c);
                    c = _ctx.Next();
                }

                while (char.IsDigit(c))
                {
                    scratch.Append(c);
                    c = _ctx.Next();
                    numberLenght++;
                    expLenght++;
                }
            }

            if (isExp && expLenght == 0)
            {
                _tainted = true;
                return null;
            }

            // Build number from the parsed string
            string s = scratch.ToString();
            if (isFloat)
            {
                if (numberLenght < 17)
                {
                    return Convert.ToDouble(s, CultureInfo.InvariantCulture);
                }
                else
                {
                    return Convert.ToDecimal(s, CultureInfo.InvariantCulture);
                }
            }
            else
            {
                return Convert.ToInt32(s);
            }
        }

        private object ParseArray(int recursiveCount = 1)
        {
            // StrictRules: Mark as tainted when MaxRecursiveCount is exceeded
            if (recursiveCount >= MaxRecursiveCount) { _tainted = true; }

            bool correct = false;
            char c = _ctx.SkipWhite();
            List<object> array = new List<object>();
            Type arrayContentType = null;
            bool hasSameType = true;
            bool hasNulls = false;
            if (c == '[')
            {
                _ctx.Next();
            }

            bool? expectValue = null;
            do
            {
                c = _ctx.SkipWhite();
                if (c == ']')
                {
                    // StrictRules: Mark as tainted when unexpected end of array
                    if (expectValue == true) { _tainted = true; }

                    correct = true;
                    _ctx.Next();
                    break;
                }
                else if (c == ',')
                {
                    // StrictRules: Mark as tainted when unexpected comma on array
                    if (expectValue == true || array.Count == 0) { _tainted = true; }

                    _ctx.Next();
                    expectValue = true;
                }
                else
                {
                    // StrictRules: Mark as tainted when unexpected value on array
                    if (expectValue == false) { _tainted = true; }

                    object value = ParseValue(recursiveCount + 1);
                    array.Add(value);
                    expectValue = false;

                    if (hasSameType)
                    {
                        Type valueType = value?.GetType();
                        if (valueType == null) { hasNulls = true; }

                        if (arrayContentType == null || arrayContentType == valueType)
                        {
                            arrayContentType = valueType;
                        }
                        else
                        {
                            hasSameType = false;
                        }
                    }
                }
            } while (!_ctx.AtEnd());

            if (correct == false)
            {
                _tainted = true;
            }

            object result = array;
            bool isNullableType = arrayContentType?.IsClass == true;
            if (hasSameType && arrayContentType != null && (isNullableType || (hasNulls == false)))
            {
                var enumerableType = typeof(System.Linq.Enumerable);
                var castMethod = enumerableType.GetMethod("Cast")?.MakeGenericMethod(arrayContentType);
                var toListMethod = enumerableType.GetMethod("ToList")?.MakeGenericMethod(arrayContentType);
                IEnumerable<object> itemsToCast = array;
                var castedItems = castMethod?.Invoke(null, new object[] { itemsToCast });
                result = toListMethod?.Invoke(null, new[] { castedItems });
            }

            return result;
        }

        private object ParseObject(int recursiveCount = 1)
        {
            // StrictRules: Mark as tainted when MaxRecursiveCount is exceeded
            if (recursiveCount >= MaxRecursiveCount) { _tainted = true; }

            bool correct = false;
            char c = _ctx.SkipWhite();
            Dictionary<string, object> obj = new Dictionary<string, object>();
            if (c == '{')
            {
                _ctx.Next();
            }

            string attributeName = null;
            bool? expectedKey = null;
            bool? expectedValue = null;
            do
            {
                c = _ctx.SkipWhite();
                if (c == ':')
                {
                    _ctx.Next();
                    if (expectedValue == true)
                    {
                        object attributeValue = ParseValue(recursiveCount + 1);
                        obj.Add(attributeName, attributeValue);
                        expectedKey = null;
                        expectedValue = false;
                    }
                }
                else if (c == ',')
                {
                    _ctx.Next();
                    _ctx.SkipWhite();
                    expectedKey = true;
                    expectedValue = false;
                }
                else if (c == '}')
                {
                    // StrictRules: Mark as tainted on unexpected end of object
                    if (expectedValue == true || expectedKey == true)
                    {
                        _tainted = true;
                    }

                    correct = true;
                    _ctx.Next();
                    break;
                }
                else
                {
                    if (expectedKey != false)
                    {
                        attributeName = ParseString(true);
                        _ctx.SkipWhite();
                        expectedKey = false;
                        expectedValue = true;
                    }
                    else
                    {
                        // Unexpected character
                        _tainted = true;
                        break;
                    }
                }
            } while (!_ctx.AtEnd());

            if (correct == false)
            {
                _tainted = true;
            }

            object result = TryConvertToTypes(obj);
            return result;
        }

        private object ParseValue(int recursiveCount = 1)
        {
            char c = _ctx.SkipWhite();
            object token;
            switch (c)
            {
                case '"':
                    token = ParseQuotedString();
                    break;

                case '\'':
                    // StrictRules: Mark as tainted when parsing single quoted strings
                    _tainted = true;
                    token = ParseSingleQuotedString();
                    break;

                case '{':
                    token = ParseObject(recursiveCount);
                    break;

                case '[':
                    token = ParseArray(recursiveCount);
                    break;

                default:
                    if (char.IsDigit(c) || c == '-')
                    {
                        token = ParseNumber();
                    }
                    else
                    {
                        string aux = ParseString();
                        if (aux.Equals("true"))
                        {
                            token = true;
                        }
                        else if (aux.Equals("false"))
                        {
                            token = false;
                        }
                        else if (aux.Equals("null"))
                        {
                            token = null;
                        }
                        else
                        {
                            // Unexpected string
                            if (aux.Length == 0)
                            {
                                _ctx.Next();
                            }

                            _tainted = true;
                            token = null;
                        }
                    }

                    break;
            }

            return token;
        }

        #endregion Private methods

        #region Public methods

        public object Parse(string text)
        {
            // Get the first object
            _ctx = new ParserContext(text);
            _tainted = false;
            _ctx.Mark();
            object obj = ParseValue();
            _ctx.SkipWhite();
            if (_ctx.AtEnd())
            {
                // StrictRules: Mark as tainted when top level is not object or array
                if (obj is string || obj is decimal || obj is int || obj is double || obj is float)
                {
                    _tainted = true;
                }

                return obj;
            }

            // StrictRules: Mark as tainted when there is more content
            _tainted = true;

            return obj;
        }

        private static JsonParser _currentInstance;

        public static object ParseText(string text, params Type[] knownTypes)
        {
            if (_currentInstance == null)
            {
                _currentInstance = new JsonParser();
            }

            _currentInstance.KnownTypes.Clear();
            _currentInstance.KnownTypes.AddRange(knownTypes);
            return _currentInstance.Parse(text);
        }

        #endregion Public methods
    }
}