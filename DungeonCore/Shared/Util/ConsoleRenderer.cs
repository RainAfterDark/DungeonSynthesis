namespace DungeonCore.Shared.Util;

using System;
using System.Text;

public class ConsoleRenderer
{
    private string _lastFrame = string.Empty;
    private readonly int _left;
    private readonly int _top;

    public ConsoleRenderer(int left = 0, int top = 0)
    {
        _left = left;
        _top = top;
        Console.CursorVisible = false;
    }

    public void Render(string newFrame)
    {
        // First run: just draw everything
        if (string.IsNullOrEmpty(_lastFrame))
        {
            Console.SetCursorPosition(_left, _top);
            Console.Write(newFrame);
            _lastFrame = newFrame;
            return;
        }

        var i = 0;
        var len = Math.Min(newFrame.Length, _lastFrame.Length);
        
        // Track screen coordinates relative to _left/_top
        var x = 0;
        var y = 0;

        while (i < len)
        {
            // If characters match, just advance our virtual cursor and continue
            if (newFrame[i] == _lastFrame[i])
            {
                if (newFrame[i] == '\n')
                {
                    x = 0;
                    y++;
                }
                else
                {
                    x++;
                }
                i++;
                continue;
            }

            // MISMATCH FOUND:
            // Move the actual console cursor to where we are, currently
            Console.SetCursorPosition(_left + x, _top + y);

            // 2. Look ahead to see how many consecutive characters are different.
            // Writing a chunk "ABC" is faster than Write(A) + Move + Write(B)...
            var sb = new StringBuilder();

            while (i < len && newFrame[i] != '\n' && newFrame[i] != _lastFrame[i])
            {
                sb.Append(newFrame[i]);
                x++;
                i++;
            }

            // 3. Write the diff chunk
            Console.Write(sb.ToString());
        }
        
        _lastFrame = newFrame;
    }
}