using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace VAR.Json
{
    public class JsonWriterConfiguration
    {
        private bool _indent;
        public bool Indent { get => _indent; }

        private bool _useTabForIndent;
        public bool UseTabForIndent { get => _useTabForIndent; }

        private int _indentChars;
        public int IndentChars { get => _indentChars; }

        private int _indentThresold;
        public int IndentThresold { get => _indentThresold;  }

        public JsonWriterConfiguration(
            bool indent = false,
            bool useTablForIndent = false,
            int indentChars = 4,
            int indentThresold = 3)
        {
            _indent = indent;
            _useTabForIndent = useTablForIndent;
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
            if(other is JsonWriterConfiguration)
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

        private void WriteIndent(StringBuilder sbOutput, int level)
        {
            if (!_config.Indent)
            {
                return;
            }
            sbOutput.Append('\n');
            if (_config.UseTabForIndent)
            {
                for (int i = 0; i < level; i++) { sbOutput.Append('\t'); }
            }
            else
            {
                int n = level * _config.IndentChars;
                for (int i = 0; i < n; i++) { sbOutput.Append(' '); }
            }
        }

        private void WriteString(StringBuilder sbOutput, string str)
        {
            sbOutput.Append('"');
            char c;
            int n = str.Length;
            for (int i = 0; i < n; i++)
            {
                c = str[i];
                if (c == '"') { sbOutput.Append("\\\""); }
                else if (c == '\\') { sbOutput.Append("\\\\"); }
                else if (c == '/') { sbOutput.Append("\\/"); }
                else if (c == '\b') { sbOutput.Append("\\b"); }
                else if (c == '\f') { sbOutput.Append("\\f"); }
                else if (c == '\n') { sbOutput.Append("\\n"); }
                else if (c == '\r') { sbOutput.Append("\\r"); }
                else if (c == '\t') { sbOutput.Append("\\t"); }
                else if (c < 32 || c >= 127) { sbOutput.AppendFormat("\\u{0:X04}", (int)c); }
                else { sbOutput.Append(c); }
            }
            sbOutput.Append('"');
        }

        private void WriteValue(StringBuilder sbOutput, object obj, List<object> parentLevels, bool useReflection)
        {
            if (obj == null || obj is DBNull)
            {
                // NULL
                sbOutput.Append("null");
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
                sbOutput.Append(obj.ToString());
            }
            else if (obj is string)
            {
                // Strings
                WriteString(sbOutput, (string)obj);
            }
            else if (obj is bool)
            {
                // Booleans
                sbOutput.Append(((bool)obj) ? "true" : "false");
            }
            else if (obj is DateTime)
            {
                // DateTime
                sbOutput.Append('"');
                sbOutput.Append(((DateTime)obj).ToString("yyyy-MM-ddTHH:mm:ssZ"));
                sbOutput.Append('"');
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

        private void WriteList(StringBuilder sbOutput, object obj, List<object> parentLevels)
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
                sbOutput.Append("[ ]");
                return;
            }

            // Write array
            bool first = true;
            sbOutput.Append("[ ");
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(sbOutput, parentLevels.Count + 1);
            }
            foreach (object childObj in list)
            {
                if (!first)
                {
                    sbOutput.Append(", ");
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
            sbOutput.Append(" ]");
        }

        private void WriteObject(StringBuilder sbOutput, object obj, List<object> parentLevels)
        {
            IDictionary map = (IDictionary)obj;
            int n = map.Count;

            // Empty
            if (map.Count == 0)
            {
                sbOutput.Append("{ }");
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
            sbOutput.Append("{ ");
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(sbOutput, parentLevels.Count + 1);
            }
            foreach (object key in map.Keys)
            {
                object value = map[key];
                if (!first)
                {
                    sbOutput.Append(", ");
                    if (!isLeaf || n > _config.IndentThresold)
                    {
                        WriteIndent(sbOutput, parentLevels.Count + 1);
                    }
                }
                first = false;
                WriteString(sbOutput, Convert.ToString(key));
                sbOutput.Append(": ");
                parentLevels.Add(obj);
                WriteValue(sbOutput, value, parentLevels, true);
                parentLevels.Remove(obj);
            }
            if (!isLeaf || n > _config.IndentThresold)
            {
                WriteIndent(sbOutput, parentLevels.Count);
            }
            sbOutput.Append(" }");
        }

        private void WriteReflectedObject(StringBuilder sbOutput, object obj, List<object> parentLevels)
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
                sbOutput.Append("{ }");
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
            sbOutput.Append("{ ");
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
                    sbOutput.Append(", ");
                    if (!isLeaf || n > _config.IndentThresold)
                    {
                        WriteIndent(sbOutput, parentLevels.Count + 1);
                    }
                }
                first = false;
                WriteString(sbOutput, property.Name);
                sbOutput.Append(": ");
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
            sbOutput.Append(" }");
        }

        #endregion Private methods

        #region Public methods

        public string Write(object obj)
        {
            StringBuilder sbOutput = new StringBuilder();
            WriteValue(sbOutput, obj, new List<object>(), true);
            return sbOutput.ToString();
        }

        #endregion Public methods
    }
}