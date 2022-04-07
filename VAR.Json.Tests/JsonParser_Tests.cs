using System.Collections.Generic;
using Xunit;

namespace VAR.Json.Tests
{
    public class JsonParser_Tests
    {
        #region Parse

        private class SwallowObject
        {
            public string Text { get; set; }
            public int Number { get; set; }
        }

        [Fact]
        public void Parse__SwallowObject()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            SwallowObject result = parser.Parse(@"{""Text"": ""AAAA"", ""Number"": 42}") as SwallowObject;
            Assert.False(parser.Tainted);
            Assert.Equal("AAAA", result?.Text);
            Assert.Equal(42, result.Number);
        }

        private class DeeperObject_L1
        {
            public string Name { get; set; }
            public SwallowObject Object { get; set; }
        }

        [Fact]
        public void Parse__DeeperObject_L1()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            parser.KnownTypes.Add(typeof(DeeperObject_L1));
            DeeperObject_L1 result =
                parser.Parse(@"{""Name"": ""Thing"", ""Object"": {""Text"": ""AAAA"", ""Number"": 42}}") as
                    DeeperObject_L1;
            Assert.False(parser.Tainted);
            Assert.Equal("Thing", result.Name);
            Assert.Equal("AAAA", result.Object.Text);
            Assert.Equal(42, result.Object.Number);
        }

        private class DeeperObject_L2
        {
            public int Count { get; set; }
            public DeeperObject_L1 Object { get; set; }
        }

        [Fact]
        public void Parse__DeeperObject_L2()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            parser.KnownTypes.Add(typeof(DeeperObject_L1));
            parser.KnownTypes.Add(typeof(DeeperObject_L2));
            DeeperObject_L2 result =
                parser.Parse(
                        @"{""Count"": 1, ""Object"": {""Name"": ""Thing"", ""Object"": {""Text"": ""AAAA"", ""Number"": 42}}}")
                    as DeeperObject_L2;
            Assert.False(parser.Tainted);
            Assert.Equal(1, result.Count);
            Assert.Equal("Thing", result.Object.Name);
            Assert.Equal("AAAA", result.Object.Object.Text);
            Assert.Equal(42, result.Object.Object.Number);
        }

        [Fact]
        public void Parse__SwallowObjectArray()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            List<SwallowObject> result = parser.Parse(@"[{""Text"": ""AAAA"", ""Number"": 42}]") as List<SwallowObject>;
            Assert.False(parser.Tainted);
            Assert.Single(result);
            Assert.Equal("AAAA", result[0].Text);
            Assert.Equal(42, result[0].Number);
        }

        private class DeeperObjectArray_L1
        {
            public int Count { get; set; }
            public List<SwallowObject> Array { get; set; }
        }

        [Fact]
        public void Parse__DeeperObjectArray_L1()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            parser.KnownTypes.Add(typeof(DeeperObjectArray_L1));
            DeeperObjectArray_L1 result =
                parser.Parse(@"{""Count"": 1, ""Array"": [{""Text"": ""AAAA"", ""Number"": 42}]}") as
                    DeeperObjectArray_L1;
            Assert.False(parser.Tainted);
            Assert.Equal(1, result.Count);
            Assert.Equal("AAAA", result.Array[0].Text);
            Assert.Equal(42, result.Array[0].Number);
        }

        private class DeeperObjectArray_L2
        {
            public string Name { get; set; }
            public List<DeeperObjectArray_L1> Objects { get; set; }
        }

        [Fact]
        public void Parse__DeeperObjectArray_L2()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            parser.KnownTypes.Add(typeof(DeeperObjectArray_L1));
            parser.KnownTypes.Add(typeof(DeeperObjectArray_L2));
            DeeperObjectArray_L2 result =
                parser.Parse(
                        @"{""Name"": ""Thing"", ""Objects"": [{""Count"": 1, ""Array"": [{""Text"": ""AAAA"", ""Number"": 42}]}]}")
                    as DeeperObjectArray_L2;
            Assert.False(parser.Tainted);
            Assert.Equal("Thing", result.Name);
            Assert.Equal(1, result.Objects[0].Count);
            Assert.Equal("AAAA", result.Objects[0].Array[0].Text);
            Assert.Equal(42, result.Objects[0].Array[0].Number);
        }

        #endregion Parse

        #region Validity tests

        [Fact]
        public void Parse__Validity_Fail01()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"""A JSON payload should be an object or array, not a string.""");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail02()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""Unclosed array""");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail03()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{unquoted_key: ""keys must be quoted""}");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail04()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""extra comma"",]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail05()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""double extra comma"",,]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail06()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[   , ""<-- missing value""]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail07()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""Comma after the close""],");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail08()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""Extra close""]]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail09()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{""Extra comma"": true,}");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail10()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{""Extra value after close"": true} ""misplaced quoted value""");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail11()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{""Illegal expression"": 1 + 2}");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail12()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{""Illegal invocation"": alert()}");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail13()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{""Numbers cannot have leading zeroes"": 013}");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail14()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{""Numbers cannot be hex"": 0x14}");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail15()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""Illegal backslash escape: \x15""]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail16()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[\naked]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail17()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""Illegal backslash escape: \017""]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail18()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[[[[[[[[[[[[[[[[[[[[""Too deep""]]]]]]]]]]]]]]]]]]]]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail19()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{""Missing colon"" null}");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail20()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{""Double colon"":: null}");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail21()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{""Comma instead of colon"", null}");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail22()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""Colon instead of comma"": false]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail23()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""Bad value"", truth]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail24()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"['single quote']");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail25()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""	tab	character	in	string	""]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail26()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""tab\   character\   in\  string\  ""]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail27()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""line
break""]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail28()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""line\
break""]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail29()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[0e]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail30()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[0e+]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail31()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[0e+-1]");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail32()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{""Comma instead if closing brace"": true,");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Fail33()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[""mismatch""}");
            Assert.True(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Pass01()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[
    ""JSON Test Pattern pass1"",
    {""object with 1 member"":[""array with 1 element""]},
    {},
    [],
    -42,
    true,
    false,
    null,
    {
        ""integer"": 1234567890,
        ""real"": -9876.543210,
        ""e"": 0.123456789e-12,
        ""E"": 1.234567890E+34,
        """":  23456789012E66,
        ""zero"": 0,
        ""one"": 1,
        ""space"": "" "",
        ""quote"": ""\"""",
        ""backslash"": ""\\"",
        ""controls"": ""\b\f\n\r\t"",
        ""slash"": ""/ & \/"",
        ""alpha"": ""abcdefghijklmnopqrstuvwyz"",
        ""ALPHA"": ""ABCDEFGHIJKLMNOPQRSTUVWYZ"",
        ""digit"": ""0123456789"",
        ""0123456789"": ""digit"",
        ""special"": ""`1~!@#$%^&*()_+-={':[,]}|;.</>?"",
        ""hex"": ""\u0123\u4567\u89AB\uCDEF\uabcd\uef4A"",
        ""true"": true,
        ""false"": false,
        ""null"": null,
        ""array"":[  ],
        ""object"":{  },
        ""address"": ""50 St. James Street"",
        ""url"": ""http://www.JSON.org/"",
        ""comment"": ""// /* <!-- --"",
        ""# -- --> */"": "" "",
        "" s p a c e d "" :[1,2 , 3

,

4 , 5        ,          6           ,7        ],""compact"":[1,2,3,4,5,6,7],
        ""jsontext"": ""{\""object with 1 member\"":[\""array with 1 element\""]}"",
        ""quotes"": ""&#34; \u0022 %22 0x22 034 &#x22;"",
        ""\/\\\""\uCAFE\uBABE\uAB98\uFCDE\ubcda\uef4A\b\f\n\r\t`1~!@#$%^&*()_+-=[]{}|;:',./<>?""
: ""A key can be any string""
    },
    0.5 ,98.6
,
99.44
,

1066,
1e1,
0.1e1,
1e-1,
1e00,2e+00,2e-00
,""rosebud""]");
            Assert.False(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Pass02()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"[[[[[[[[[[[[[[[[[[[""Not too deep""]]]]]]]]]]]]]]]]]]]");
            Assert.False(parser.Tainted);
        }

        [Fact]
        public void Parse__Validity_Pass03()
        {
            JsonParser parser = new JsonParser();
            parser.Parse(@"{
    ""JSON Test Pattern pass3"": {
        ""The outermost value"": ""must be an object or array."",
        ""In this test"": ""It is an object.""
    }
}
");
            Assert.False(parser.Tainted);
        }

        #endregion Validity tests
    }
}