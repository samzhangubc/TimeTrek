using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ThymeMe.Launcher;

internal static class Program
{
    private const string PayloadRelativePath = @"app\thymeme.exe";

    [STAThread]
    private static int Main()
    {
        try
        {
            string releaseRoot = AppDomain.CurrentDomain.BaseDirectory;
            string payloadPath = Path.GetFullPath(Path.Combine(releaseRoot, PayloadRelativePath));
            string payloadDirectory = Path.GetDirectoryName(payloadPath)!;

            if (!File.Exists(payloadPath))
            {
                ShowError("The Thyme-Me application files are incomplete. Extract the entire portable ZIP before running thymeme.exe.");
                return 2;
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = payloadPath,
                WorkingDirectory = payloadDirectory,
                UseShellExecute = false,
                Arguments = BuildArguments(Environment.GetCommandLineArgs().Skip(1)),
            };

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                ShowError("Windows could not start Thyme-Me.");
                return 3;
            }

            return 0;
        }
        catch (Exception exception)
        {
            ShowError($"Thyme-Me could not start.\n\n{exception.Message}");
            return 1;
        }
    }

    private static void ShowError(string message) =>
        _ = MessageBox(IntPtr.Zero, message, "Thyme-Me", 0x00000010);

    private static string BuildArguments(IEnumerable<string> arguments) =>
        string.Join(" ", arguments.Select(QuoteArgument));

    private static string QuoteArgument(string value)
    {
        if (value.Length > 0 && !value.Any(character => char.IsWhiteSpace(character) || character == '"'))
        {
            return value;
        }

        StringBuilder result = new StringBuilder(value.Length + 2).Append('"');
        int backslashes = 0;
        foreach (char character in value)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
            {
                result.Append('\\', (backslashes * 2) + 1).Append('"');
                backslashes = 0;
                continue;
            }

            result.Append('\\', backslashes).Append(character);
            backslashes = 0;
        }

        return result.Append('\\', backslashes * 2).Append('"').ToString();
    }

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(nint windowHandle, string text, string caption, uint type);
}
