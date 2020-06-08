using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace VAR.Json
{
    public class JsonWriterConfiguration
    {
        private bool _indent;
        public bool Indent { get { return _indent; } }

        private bool _useTabForIndent;
        public bool UseTabForIndent { get { return _useTabForIndent; } }

        private int _indentChars;
        public int IndentChars { get { return _indentChars; } }

        private int _indentThresold;
        public int IndentThresold { get { return _indentThresold; } }

        public JsonWriterConfiguration(
            bool indent = false,
            bool useTabForIndent = false,
            int indentChars = 4,
            int indentThresold = 3)
        {
            _indent = indent;
            _useTabForIndent = useTabForIndent;
            _indentChars = indentChars;
            _indentThresold = indentThresold;
        }

        public bool Equals(JsonWriterConfiguration other)
        {
            return
                other.Indent == Indent &&
                other.UseTabForIndent == UseTabForIndent &&
                other.IndentChars == IndentChars &&
                other.IndentThresold == IndentThresold &&
                true;
        }

        public override bool Equals(object other)
        {
            if (other is JsonWriterConfiguration)
            {
                return Equals(other as JsonWriterConfiguration);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _indent.GetHashCode() ^ _useTabForIndent.GetHashCode() ^ _indentChars.GetHashCode() ^ _indentThresold.GetHashCode();
        }
    }

    public class JsonWriter
    {
        #region Declarations

        private JsonWriterConfiguration _config = null;

        #endregion Declarations

        #region Creator

        public JsonWriter(JsonWriterConfiguration config = null)
        {
            _config = config;
            if (_config == null)
            {
                _config = new JsonWriterConfiguration();
            }
        }

        #endregion Creator

        #region Private methods

        private bool IsValue(object obj)
        {
            if (obj == null)
            {
                return true;
            }
            if (
                (obj is float) ||
                (obj is double) ||
                (obj is short) ||
                (obj is int) ||
                (obj is long) ||
                (obj is string) ||
                (obj is bool) ||
                false)
            {
                return true;
            }
            return false;
        }

        private void WriteIndent(TextWriter textWriter, int level)
        {
            if (!_config.Indent)
            {
                return;
            }
            textWriter.Write('\n');
            if (_config.UseTabForIndent)
            {
                for (int i = 0; i < level; i++) { textWriter.Write('\t'); }
            }
            else
            {
                int n = level * _config.IndentChars;
                for (int i = 0; i < n; i++) { textWriter.Write(' '); }
            }
        }

        private void WriteString(TextWriter textWriter, string str)
        {
            textWriter.Write('"');
            char c;
            int n = str.Length;
            for (int i = 0; i < n; i++)
            {
                c = str[i];
                if (c == '"') { textWriter.Write("\\\""); }
                else if (c == '\\') { textWriter.Write("\\\\"); }
                else if (c == '/') { textWriter.Write("\\/"); }
                else if (c == '\b') { textWriter.Write("\\b"); }
                else if (c == '\f') { textWriter.Write("\\f"); }
                else if (c == '\n') { textWriter.Write("\\n"); }
                else if (c == '\r') { textWriter.Write("\\r"); }
                else if (c == '\t') { textWriter.Write("\\t"); }
                else if (c < 32 || c >= 127) { textWriter.Write("\\u{0:X04}", (int)c); }
                else { textWriter.Write(c); }
            }
            textWriter.Write('"');
        }

        private void WriteValue(TextWriter textWriter, object obj, List<object> parentLevels, bool useReflection)
        {
            if (obj == null || obj is DBNull)
            {
                // NULL
                textWriter.Write("null");
            }
            else if (
                (obj is float) ||
                (obj is double) ||
                (obj is short) ||
                (obj is int) ||
                (obj is long) ||
                false)
            {
                // Numbers
                textWriter.Write(obj.ToString());
            }
            else if (obj is string)
            {
                // Strings
                WriteString(textWriter, (string)obj);
            }
            else if (obj is bool)
            {
                // Booleans
                textWriter.Write(((bool)obj) ? "true" : "false");
            }
            else if (obj is DateTime)
            {
                // DateTime
                textWriter.Write('"');
                textWriter.Write(((DateTime)obj).ToString("yyyy-MM-ddTHH:mm:ss"));
                textWriter.Write('"');
            }
            else if (obj is IDictionary)
            {
                // Objects
                WriteObject(textWriter, obj, parentLevels);
            }
            else if (obj is IEnumerable)
            {
                // Array/List
                WriteList(textWriter, obj, parentLevels);
            }
            else
            {
                if (useReflection)
                {
                    // Reflected object
                    WriteReflectedObject(textWriter, obj, parentLevels);
                }
                else
                {
                    WriteString(textWriter, Convert.ToString(obj));
                }
            }
        }

        private void WriteList(TextWriter textWriter, object obj, List<object> parentLevels)
        {
            IEnumerable list = (IEnumerable)obj;
            int n = 0;

            // Check if it is a leaf object
            bool isLeaf = true;
            foreach (object childObj in list)
            {
                if (!IsValue(childObj))
                {
                    isLeaf = false;
                }
                n++;
            }

            // Empty
            if (n == 0)
            {
                textWriter.Write("[ ]");
                return;
            }

            // Write array
            bool first = true;
            textWriter.Write("[ ");
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(textWriter, parentLevels.Count + 1);
            }
            foreach (object childObj in list)
            {
                if (!first)
                {
                    textWriter.Write(", ");
                    if (!isLeaf || n > _config.IndentThresold)
                    {
                        WriteIndent(textWriter, parentLevels.Count + 1);
                    }
                }
                first = false;
                parentLevels.Add(obj);
                WriteValue(textWriter, childObj, parentLevels, true);
                parentLevels.Remove(obj);
            }
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(textWriter, parentLevels.Count);
            }
            textWriter.Write(" ]");
        }

        private void WriteObject(TextWriter textWriter, object obj, List<object> parentLevels)
        {
            IDictionary map = (IDictionary)obj;
            int n = map.Count;

            // Empty
            if (map.Count == 0)
            {
                textWriter.Write("{ }");
                return;
            }

            // Check if it is a leaf object
            bool isLeaf = true;
            foreach (object value in map.Values)
            {
                if (!IsValue(value))
                {
                    isLeaf = false;
                    break;
                }
            }

            // Write object
            bool first = true;
            textWriter.Write("{ ");
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(textWriter, parentLevels.Count + 1);
            }
            foreach (object key in map.Keys)
            {
                object value = map[key];
                if (!first)
                {
                    textWriter.Write(", ");
                    if (!isLeaf || n > _config.IndentThresold)
                    {
                        WriteIndent(textWriter, parentLevels.Count + 1);
                    }
                }
                first = false;
                WriteString(textWriter, Convert.ToString(key));
                textWriter.Write(": ");
                parentLevels.Add(obj);
                WriteValue(textWriter, value, parentLevels, true);
                parentLevels.Remove(obj);
            }
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(textWriter, parentLevels.Count);
            }
            textWriter.Write(" }");
        }

        private void WriteReflectedObject(TextWriter textWriter, object obj, List<object> parentLevels)
        {
            Type type = obj.GetType();
            PropertyInfo[] rawProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            List<PropertyInfo> properties = new List<PropertyInfo>();
            foreach (PropertyInfo property in rawProperties)
            {
                if (property.CanRead == false) { continue; }

                properties.Add(property);
            }
            int n = properties.Count;

            // Empty
            if (n == 0)
            {
                textWriter.Write("{ }");
                return;
            }

            // Check if it is a leaf object
            bool isLeaf = true;
            foreach (PropertyInfo property in properties)
            {
                object value = property.GetValue(obj, null);
                if (!IsValue(value))
                {
                    isLeaf = false;
                    break;
                }
            }

            // Write object
            bool first = true;
            textWriter.Write("{ ");
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(textWriter, parentLevels.Count + 1);
            }
            foreach (PropertyInfo property in properties)
            {
                object value = null;
                MethodInfo getMethod = property.GetGetMethod();
                ParameterInfo[] parameters = getMethod.GetParameters();
                if (parameters.Length == 0)
                {
                    value = property.GetValue(obj, null);
                }
                if (!first)
                {
                    textWriter.Write(", ");
                    if (!isLeaf || n > _config.IndentThresold)
                    {
                        WriteIndent(textWriter, parentLevels.Count + 1);
                    }
                }
                first = false;
                WriteString(textWriter, property.Name);
                textWriter.Write(": ");
                parentLevels.Add(obj);
                if (value != obj && parentLevels.Contains(value) == false)
                {
                    WriteValue(textWriter, value, parentLevels, false);
                }
                else
                {
                    WriteValue(textWriter, null, parentLevels, false);
                }
                parentLevels.Remove(obj);
            }
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(textWriter, parentLevels.Count);
            }
            textWriter.Write(" }");
        }

        #endregion Private methods

        #region Public methods

        public TextWriter Write(object obj, TextWriter textWriter)
        {
            if (textWriter == null)
            {
                textWriter = new StringWriter();
            }
            WriteValue(textWriter, obj, new List<object>(), true);
            return textWriter;
        }

        public string Write(object obj)
        {
            StringWriter textWriter = new StringWriter();
            WriteValue(textWriter, obj, new List<object>(), true);
            return textWriter.ToString();
        }

        private static Dictionary<JsonWriterConfiguration, JsonWriter> _dictInstances = new Dictionary<JsonWriterConfiguration, JsonWriter>();

        public static string WriteObject(object obj,
            JsonWriterConfiguration config = null,
            bool indent = false,
            bool useTabForIndent = false,
            int indentChars = 4,
            int indentThresold = 3)
        {
            JsonWriter jsonWriter = null;

            if (config != null)
            {
                if (_dictInstances.ContainsKey(config) == false)
                {
                    jsonWriter = new JsonWriter(config);
                    _dictInstances.Add(config, jsonWriter);
                }
                else
                {
                    jsonWriter = _dictInstances[config];
                }
                return jsonWriter.Write(obj);
            }

            foreach (KeyValuePair<JsonWriterConfiguration, JsonWriter> pair in _dictInstances)
            {
                if (
                    pair.Key.Indent == indent &&
                    pair.Key.UseTabForIndent == useTabForIndent &&
                    pair.Key.IndentChars == indentChars &&
                    pair.Key.IndentThresold == indentThresold &&
                    true)
                {
                    jsonWriter = pair.Value;
                    break;
                }
            }
            if (jsonWriter != null)
            {
                return jsonWriter.Write(obj);
            }

            JsonWriterConfiguration jsonWriterConfiguration = new JsonWriterConfiguration(
                indent: indent,
                useTabForIndent: useTabForIndent,
                indentChars: indentChars,
                indentThresold: indentThresold);
            jsonWriter = new JsonWriter(jsonWriterConfiguration);
            _dictInstances.Add(jsonWriterConfiguration, jsonWriter);

            return jsonWriter.Write(obj);
        }

        #endregion Public methods
    }
}