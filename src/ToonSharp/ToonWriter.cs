using System.Text;

namespace ToonSharp;

internal sealed class ToonWriter
{
    private readonly StringBuilder _sb;

    public ToonWriter(int initialCapacity = 2048)
    {
        _sb = new StringBuilder(initialCapacity);
    }

    public StringBuilder Buffer => _sb;

    public int Length => _sb.Length;

    public void EnsureCapacity(int additionalChars)
    {
        int needed = _sb.Length + additionalChars;
        if (_sb.Capacity < needed)
        {
            _sb.Capacity = needed;
        }
    }

    public void NewLine()
    {
        if (_sb.Length > 0 && _sb[_sb.Length - 1] != '\n')
        {
            _sb.Append('\n');
        }
    }

    public void Append(string text) => _sb.Append(text);

    public void Append(char ch) => _sb.Append(ch);

    public void AddLine(string line)
    {
        NewLine();
        _sb.Append(line);
    }

    public void AddLines(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            AddLine(lines[i]);
        }
    }

    public override string ToString()
    {
        if (_sb.Length == 0)
        {
            return "\n";
        }

        if (_sb[_sb.Length - 1] != '\n')
        {
            _sb.Append('\n');
        }

        return _sb.ToString();
    }
}
