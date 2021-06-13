# .Net library for JSON parsing

## Usage

### VAR.Json
Add the resulting assembly as reference in your projects, and this line on code:

```csharp
using VAR.Json;
```

Parse any string with JSON content:
```csharp
object result = JsonParser.ParseText("{\"Test\": 1}");
```

Serialize any object to JSON:
```csharp
string jsonText = JsonWriter.WriteObject(new List<int>{1, 2, 3, 4});
```

### VAR.Json.JsonParser
This object can be invoked with a list of types used to cast the json objects.


```csharp

class Person
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public DateTime DateOfBirth { get; set; }
}

JsonParser jsonParser = new JsonParser();
jsonParser.KnownTypes.Add(typeof(Person));
Person jsonText = jsonParser.Parse("{ \"Name\": \"John", \"Surname\": \"Doe\", \"DateOfBirth\": \"1970-01-01\"}") as Person;
```


## Building
A Visual Studio solution is provided. Simply, click build on the IDE.

The build generates a DLL and a Nuget package.

## Contributing
1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request :D

## Credits
* Valeriano Alfonso Rodriguez.

## License

    The MIT License (MIT)

    Copyright (c) 2016-2021 Valeriano Alfonso Rodriguez

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
