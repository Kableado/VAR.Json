using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VAR.Json.Tests
{
    [TestClass()]
    public class JsonParser_Tests
    {
        #region Parse

        public class SwallowObject
        {
            public string Text { get; set; }
            public int Number { get; set; }
        }

        [TestMethod()]
        public void Parse__SwallowObject()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            SwallowObject result = parser.Parse(@"{""Text"": ""AAAA"", ""Number"": 42}") as SwallowObject;
            Assert.AreEqual(false, parser.Tainted);
            Assert.AreEqual("AAAA", result.Text);
            Assert.AreEqual(42, result.Number);
        }

        public class DeeperObject_L1
        {
            public string Name { get; set; }
            public SwallowObject Object { get; set; }
        }

        [TestMethod()]
        public void Parse__DeeperObject_L1()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            parser.KnownTypes.Add(typeof(DeeperObject_L1));
            DeeperObject_L1 result = parser.Parse(@"{""Name"": ""Thing"", ""Object"": {""Text"": ""AAAA"", ""Number"": 42}}") as DeeperObject_L1;
            Assert.AreEqual(false, parser.Tainted);
            Assert.AreEqual("Thing", result.Name);
            Assert.AreEqual("AAAA", result.Object.Text);
            Assert.AreEqual(42, result.Object.Number);
        }

        public class DeeperObject_L2
        {
            public int Count { get; set; }
            public DeeperObject_L1 Object { get; set; }
        }

        [TestMethod()]
        public void Parse__DeeperObject_L2()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            parser.KnownTypes.Add(typeof(DeeperObject_L1));
            parser.KnownTypes.Add(typeof(DeeperObject_L2));
            DeeperObject_L2 result = parser.Parse(@"{""Count"": 1, ""Object"": {""Name"": ""Thing"", ""Object"": {""Text"": ""AAAA"", ""Number"": 42}}}") as DeeperObject_L2;
            Assert.AreEqual(false, parser.Tainted);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Thing", result.Object.Name);
            Assert.AreEqual("AAAA", result.Object.Object.Text);
            Assert.AreEqual(42, result.Object.Object.Number);
        }

        [TestMethod()]
        public void Parse__SwallowObjectArray()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            List<SwallowObject> result = parser.Parse(@"[{""Text"": ""AAAA"", ""Number"": 42}]") as List<SwallowObject>;
            Assert.AreEqual(false, parser.Tainted);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("AAAA", result[0].Text);
            Assert.AreEqual(42, result[0].Number);
        }

        public class DeeperObjectArray_L1
        {
            public int Count { get; set; }
            public List<SwallowObject> Array { get; set; }
        }

        [TestMethod()]
        public void Parse__DeeperObjectArray_L1()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            parser.KnownTypes.Add(typeof(DeeperObjectArray_L1));
            DeeperObjectArray_L1 result = parser.Parse(@"{""Count"": 1, ""Array"": [{""Text"": ""AAAA"", ""Number"": 42}]}") as DeeperObjectArray_L1;
            Assert.AreEqual(false, parser.Tainted);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("AAAA", result.Array[0].Text);
            Assert.AreEqual(42, result.Array[0].Number);
        }

        public class DeeperObjectArray_L2
        {
            public string Name { get; set; }
            public List<DeeperObjectArray_L1> Objects { get; set; }
        }

        [TestMethod()]
        public void Parse__DeeperObjectArray_L2()
        {
            JsonParser parser = new JsonParser();
            parser.KnownTypes.Add(typeof(SwallowObject));
            parser.KnownTypes.Add(typeof(DeeperObjectArray_L1));
            parser.KnownTypes.Add(typeof(DeeperObjectArray_L2));
            DeeperObjectArray_L2 result = parser.Parse(@"{""Name"": ""Thing"", ""Objects"": [{""Count"": 1, ""Array"": [{""Text"": ""AAAA"", ""Number"": 42}]}]}") as DeeperObjectArray_L2;
            Assert.AreEqual(false, parser.Tainted);
            Assert.AreEqual("Thing", result.Name);
            Assert.AreEqual(1, result.Objects[0].Count);
            Assert.AreEqual("AAAA", result.Objects[0].Array[0].Text);
            Assert.AreEqual(42, result.Objects[0].Array[0].Number);
        }

        #endregion Parse

        #region Validity tests

        [TestMethod()]
        public void Parse__Validity_Fail01()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"""A JSON payload should be an object or array, not a string.""");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail02()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""Unclosed array""");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail03()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{unquoted_key: ""keys must be quoted""}");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail04()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""extra comma"",]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail05()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""double extra comma"",,]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail06()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[   , ""<-- missing value""]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail07()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""Comma after the close""],");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail08()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""Extra close""]]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail09()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{""Extra comma"": true,}");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail10()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{""Extra value after close"": true} ""misplaced quoted value""");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail11()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{""Illegal expression"": 1 + 2}");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail12()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{""Illegal invocation"": alert()}");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail13()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{""Numbers cannot have leading zeroes"": 013}");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail14()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{""Numbers cannot be hex"": 0x14}");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail15()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""Illegal backslash escape: \x15""]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail16()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[\naked]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail17()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""Illegal backslash escape: \017""]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail18()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[[[[[[[[[[[[[[[[[[[[""Too deep""]]]]]]]]]]]]]]]]]]]]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail19()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{""Missing colon"" null}");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail20()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{""Double colon"":: null}");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail21()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{""Comma instead of colon"", null}");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail22()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""Colon instead of comma"": false]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail23()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""Bad value"", truth]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail24()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"['single quote']");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail25()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""	tab	character	in	string	""]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail26()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""tab\   character\   in\  string\  ""]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail27()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""line
break""]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail28()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""line\
break""]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail29()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[0e]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail30()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[0e+]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail31()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[0e+-1]");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail32()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{""Comma instead if closing brace"": true,");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Fail33()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[""mismatch""}");
            Assert.AreEqual(true, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Pass01()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[
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
            Assert.AreEqual(false, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Pass02()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"[[[[[[[[[[[[[[[[[[[""Not too deep""]]]]]]]]]]]]]]]]]]]");
            Assert.AreEqual(false, parser.Tainted);
        }

        [TestMethod()]
        public void Parse__Validity_Pass03()
        {
            JsonParser parser = new JsonParser();
            object result = parser.Parse(@"{
    ""JSON Test Pattern pass3"": {
        ""The outermost value"": ""must be an object or array."",
        ""In this test"": ""It is an object.""
    }
}
");
            Assert.AreEqual(false, parser.Tainted);
        }

        #endregion Validity tests
    }
}