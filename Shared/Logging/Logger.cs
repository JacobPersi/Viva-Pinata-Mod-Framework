namespace PinataParty.Internal;

public static class Logger
{
	public static void Warn(string message)
	{
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write($"{DateTime.Now}: ");
		Console.ForegroundColor = ConsoleColor.DarkYellow;
		Console.Write("[Warning] ");
		Console.ResetColor();
		Console.WriteLine(message);
	}

	public static void Error(Exception ex) 
		=> Error(ex.ToString());

	public static void Error(string message)
	{
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write($"{DateTime.Now}: ");
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Write("[Error] ");
		Console.ResetColor();
		Console.WriteLine(message);
	}
}
