![CSharpier](./banner.svg)

This repository is a fork of [belav/csharpier](https://github.com/belav/csharpier) that keeps up to date with the original while adding additional configuration via `.editorconfig`.

The upstream CSharpier project is an opinionated code formatter for C# and XML. It parses your code and re-prints it using its own rules. The printing process was ported from [prettier](https://github.com/prettier/prettier) but has evolved over time.

This fork keeps the same core behavior, but extends configuration so that more of the existing C# formatting options from `.editorconfig` can be honored. For example:

- `csharp_new_line_before_open_brace` (for types, methods, control blocks)
- `csharp_new_line_before_else`, `csharp_new_line_before_catch`, `csharp_new_line_before_finally`
- `csharp_new_line_before_members_in_object_initializers`
- `csharp_new_line_before_members_in_anonymous_types`
- `csharp_new_line_between_query_expression_clauses`
- `csharpier_use_prettier_style_trailing_commas`
- `csharpier_include_generated`, `csharpier_trim_initial_lines`

See `docs/Configuration.md` for details.

### Quick Start
Install CSharpier globally using the following command.
```bash
dotnet tool install csharpier -g
```
Then format the contents of a directory and its children with the following command.
```bash
csharpier format .
```

CSharpier can also format [on save in your editor](https://csharpier.com/docs/Editors) or as a [pre-commit hook](https://csharpier.com/docs/Pre-commit). Then you can ensure code was formatted with a [CI/CD tool](https://csharpier.com/docs/ContinuousIntegration).

---

[Read the documentation](https://csharpier.com)    
  
[Try it out](https://playground.csharpier.com)

---

### Before
```c#
public class ClassName {
    public void CallMethod() { 
        this.LongUglyMethod("1234567890", "abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
    }
}
```

### After
```c#
public class ClassName
{
    public void CallMethod()
    {
        this.LongUglyMethod(
            "1234567890",
            "abcdefghijklmnopqrstuvwxyz",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        );
    }
}
```

## Contributing
See [Development Readme](CONTRIBUTING.md)  

Join Us [![Discord](https://img.shields.io/badge/Discord-chat?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/HfAKGEZQcX)

## Sponsors

Thanks to the following companies for sponsoring the ongoing development of CSharpier.

[.NET on AWS Open Source Software Fund](https://github.com/aws/dotnet-foss) \
 \
[<img src="./Src/Website/static/img/aws.png" />](https://github.com/aws/dotnet-foss)

[Fern](https://buildwithfern.com/) \
 \
[<img src="./fern.svg" />]((https://buildwithfern.com/))

And a huge thanks to all the others who sponsor the project through [Github sponsors](https://github.com/sponsors/belav)
