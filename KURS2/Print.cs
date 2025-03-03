internal class Print
{
    string input;

    public Print(string input)
    {
        this.input = input;
    }

    public void PrintText()
    {
        List<string> result = new Formatting().Output(input);
        result = new Formatting().StringChanger(result);

        // Вывод результата
        Console.WriteLine("После форматирования:\n");

        foreach (var line in result)
        {
            Console.WriteLine(line);
        }
    }
}