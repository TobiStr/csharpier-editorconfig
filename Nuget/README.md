This package is based on [belav/csharpier](https://github.com/belav/csharpier), an opinionated code formatter for C#. It uses Roslyn to parse your code and re-prints it using its own rules. The printing process was ported from [prettier](https://github.com/prettier/prettier) but has evolved over time.

This fork adds extended configuration via `.editorconfig`, allowing you to map more of the standard C# formatting options into CSharpier’s behavior (for example brace placement, new-line behavior around `else`/`catch`/`finally`, query clauses, and optional trailing commas), while keeping the core formatting logic compatible with the original project.

### Quick Start
Install CSharpier globally using the following command.
```bash
dotnet tool install csharpier -g
```
Then format the contents of a directory and its children with the following command.
```bash
csharpier .
```

CSharpier can also format [on save in your editor](https://csharpier.com/docs/Editors), as a [pre-commit hook](https://csharpier.com/docs/Pre-commit), as [part of your build](https://csharpier.com/docs/MSBuild) or even [programatically](https://csharpier.com/docs/API). Then you can ensure code was formatted with a [CI/CD tool](https://csharpier.com/docs/ContinuousIntegration).

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
