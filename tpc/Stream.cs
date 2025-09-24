namespace TPC;

// Character streamer. Streams characters from the input (a string) one at a
// time, including peeking. Returns -1 on end of file.
internal class Stream(string input)
{
    public string input = input;
    public int position = 0;
    public int lineNumber = 1;

    // Returns the next character, or char.MaxValue on end of file.
    public char Next()
    {
        var ch = this.Peek();
        if (ch == '\n')
        {
            this.lineNumber++;
        }
        if (ch != char.MaxValue)
        {
            this.position++;
        }
        return ch;
    }

    // Peeks at the next character, or char.MaxValue on end of file.
    public char Peek() => this.position >= this.input.Length ? char.MaxValue : this.input[this.position];

    // Inverse of "next()" method.
    public void PushBack(char ch)
    {
        if (this.position == 0)
        {
            throw new Exception("Can't push back at start of stream");
        }
        this.position--;
        // Sanity check.
        if (this.input[this.position] != ch)
        {
            throw new Exception("Pushed back character doesn't match");
        }
    }
}