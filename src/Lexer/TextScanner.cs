namespace Lexer;

/// <summary>
///  Сканирует текст, предоставляя три операции: Peek(N), Advance() и IsEnd().
/// </summary>
public class TextScanner(string text)
{
    private int position;

    /// <summary>
    ///  Читает на N символов вперёд текущей позиции (по умолчанию N=0).
    /// </summary>
    public char Peek(int n = 0)
    {
        int position = this.position + n;
        return position >= text.Length ? '\0' : text[position];
    }

    /// <summary>
    ///  Сдвигает текущую позицию на один символ.
    /// </summary>
    public void Advance()
    {
        position++;
    }

    public bool IsEnd()
    {
        return position >= text.Length;
    }
}