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

        private void WriteIndent(TextWriter sbOutput, int level)
        {
            if (!_config.Indent)
            {
                return;
            }
            sbOutput.Write('\n');
            if (_config.UseTabForIndent)
            {
                for (int i = 0; i < level; i++) { sbOutput.Write('\t'); }
            }
            else
            {
                int n = level * _config.IndentChars;
                for (int i = 0; i < n; i++) { sbOutput.Write(' '); }
            }
        }

        private void WriteString(TextWriter sbOutput, string str)
        {
            sbOutput.Write('"');
            char c;
            int n = str.Length;
            for (int i = 0; i < n; i++)
            {
                c = str[i];
                if (c == '"') { sbOutput.Write("\\\""); }
                else if (c == '\\') { sbOutput.Write("\\\\"); }
                else if (c == '/') { sbOutput.Write("\\/"); }
                else if (c == '\b') { sbOutput.Write("\\b"); }
                else if (c == '\f') { sbOutput.Write("\\f"); }
                else if (c == '\n') { sbOutput.Write("\\n"); }
                else if (c == '\r') { sbOutput.Write("\\r"); }
                else if (c == '\t') { sbOutput.Write("\\t"); }
                else if (c < 32 || c >= 127) { sbOutput.Write("\\u{0:X04}", (int)c); }
                else { sbOutput.Write(c); }
            }
            sbOutput.Write('"');
        }

        private void WriteValue(TextWriter sbOutput, object obj, List<object> parentLevels, bool useReflection)
        {
            if (obj == null || obj is DBNull)
            {
                // NULL
                sbOutput.Write("null");
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
                sbOutput.Write(obj.ToString());
            }
            else if (obj is string)
            {
                // Strings
                WriteString(sbOutput, (string)obj);
            }
            else if (obj is bool)
            {
                // Booleans
                sbOutput.Write(((bool)obj) ? "true" : "false");
            }
            else if (obj is DateTime)
            {
                // DateTime
                sbOutput.Write('"');
                sbOutput.Write(((DateTime)obj).ToString("yyyy-MM-ddTHH:mm:ssZ"));
                sbOutput.Write('"');
            }
            else if (obj is IDictionary)
            {
                // Objects
                WriteObject(sbOutput, obj, parentLevels);
            }
            else if (obj is IEnumerable)
            {
                // Array/List
                WriteList(sbOutput, obj, parentLevels);
            }
            else
            {
                if (useReflection)
                {
                    // Reflected object
                    WriteReflectedObject(sbOutput, obj, parentLevels);
                }
                else
                {
                    WriteString(sbOutput, Convert.ToString(obj));
                }
            }
        }

        private void WriteList(TextWriter sbOutput, object obj, List<object> parentLevels)
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
                sbOutput.Write("[ ]");
                return;
            }

            // Write array
            bool first = true;
            sbOutput.Write("[ ");
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(sbOutput, parentLevels.Count + 1);
            }
            foreach (object childObj in list)
            {
                if (!first)
                {
                    sbOutput.Write(", ");
                    if (!isLeaf || n > _config.IndentThresold)
                    {
                        WriteIndent(sbOutput, parentLevels.Count + 1);
                    }
                }
                first = false;
                parentLevels.Add(obj);
                WriteValue(sbOutput, childObj, parentLevels, true);
                parentLevels.Remove(obj);
            }
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(sbOutput, parentLevels.Count);
            }
            sbOutput.Write(" ]");
        }

        private void WriteObject(TextWriter sbOutput, object obj, List<object> parentLevels)
        {
            IDictionary map = (IDictionary)obj;
            int n = map.Count;

            // Empty
            if (map.Count == 0)
            {
                sbOutput.Write("{ }");
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
            sbOutput.Write("{ ");
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(sbOutput, parentLevels.Count + 1);
            }
            foreach (object key in map.Keys)
            {
                object value = map[key];
                if (!first)
                {
                    sbOutput.Write(", ");
                    if (!isLeaf || n > _config.IndentThresold)
                    {
                        WriteIndent(sbOutput, parentLevels.Count + 1);
                    }
                }
                first = false;
                WriteString(sbOutput, Convert.ToString(key));
                sbOutput.Write(": ");
                parentLevels.Add(obj);
                WriteValue(sbOutput, value, parentLevels, true);
                parentLevels.Remove(obj);
            }
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(sbOutput, parentLevels.Count);
            }
            sbOutput.Write(" }");
        }

        private void WriteReflectedObject(TextWriter sbOutput, object obj, List<object> parentLevels)
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
                sbOutput.Write("{ }");
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
            sbOutput.Write("{ ");
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(sbOutput, parentLevels.Count + 1);
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
                    sbOutput.Write(", ");
                    if (!isLeaf || n > _config.IndentThresold)
                    {
                        WriteIndent(sbOutput, parentLevels.Count + 1);
                    }
                }
                first = false;
                WriteString(sbOutput, property.Name);
                sbOutput.Write(": ");
                parentLevels.Add(obj);
                if (value != obj && parentLevels.Contains(value) == false)
                {
                    WriteValue(sbOutput, value, parentLevels, false);
                }
                else
                {
                    WriteValue(sbOutput, null, parentLevels, false);
                }
                parentLevels.Remove(obj);
            }
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(sbOutput, parentLevels.Count);
            }
            sbOutput.Write(" }");
        }

        #endregion Private methods

        #region Public methods

        public string Write(object obj)
        {
            StringWriter sbOutput = new StringWriter();
            WriteValue(sbOutput, obj, new List<object>(), true);
            return sbOutput.ToString();
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