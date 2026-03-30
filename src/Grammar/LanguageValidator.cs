using Antlr4.Runtime;

using Grammar.Grammars;

namespace Grammar;

public class LanguageValidator
{
    public List<string> Errors { get; } = [];

    public bool Validate(string sourceCode)
    {
        Errors.Clear();

        AntlrInputStream inputStream = new(sourceCode);

        LanguageLexer lexer = new(inputStream);
        ValidationErrorListener lexerListener = new("Lexer", Errors);

        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(lexerListener);

        CommonTokenStream tokens = new(lexer);

        LanguageParser parser = new(tokens);
        ValidationErrorListener parserListener = new("Parser", Errors);

        parser.RemoveErrorListeners();
        parser.AddErrorListener(parserListener);

        try
        {
            parser.program();
        }
        catch (Exception ex)
        {
            Errors.Add($"CRITICAL EXCEPTION: {ex.Message}");
            return false;
        }

        return Errors.Count == 0;
    }
}

public class ValidationErrorListener(string sourceName, List<string> errorList)
    : BaseErrorListener, IAntlrErrorListener<int>
{
    public override void SyntaxError(IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        AddError(line, charPositionInLine, msg);
    }

    public void SyntaxError(IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        AddError(line, charPositionInLine, msg);
    }

    private void AddError(int line, int charPositionInLine, string msg)
    {
        errorList.Add($"[{sourceName} Error] Line {line}:{charPositionInLine} -> {msg}");
    }
}