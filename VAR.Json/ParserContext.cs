namespace VAR.Json
{
    public class ParserContext
    {
        #region Declarations

        private readonly string _text;
        private readonly int _length;
        private int _i;
        private int _markStart;

        #endregion Declarations

        #region Creator

        public ParserContext(string text)
        {
            _text = text;
            _length = text.Length;
            _i = 0;
            _markStart = 0;
        }

        #endregion Creator

        #region Public methods

        public char SkipWhite()
        {
            while (_i < _length && char.IsWhiteSpace(_text[_i]))
            {
                _i++;
            }

            if (AtEnd())
            {
                return (char)0;
            }

            return _text[_i];
        }

        public char Next()
        {
            _i++;
            if (AtEnd())
            {
                return (char)0;
            }

            return _text[_i];
        }

        public bool AtEnd()
        {
            return _i >= _length;
        }

        public void Mark()
        {
            _markStart = _i;
        }

        public string GetMarked()
        {
            if (_i < _length && _markStart < _length)
            {
                return _text.Substring(_markStart, _i - _markStart);
            }
            else
            {
                if (_markStart < _length)
                {
                    return _text.Substring(_markStart, _length - _markStart);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        #endregion Public methods
    }
}