using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrudCrudCUI;

class Menu
{
    static readonly ConsoleColor titleColor = ConsoleColor.DarkYellow;
    public static readonly ConsoleColor selectionColor = ConsoleColor.Green;

    public static void PrintMenu(string title, string[] items)
    {
        int itemCount = items.Count();

        if (itemCount == 0)
            return;

        Console.Clear();
        Console.ForegroundColor = titleColor;
        Console.WriteLine(title);
        Console.ResetColor();

        Console.ForegroundColor = selectionColor;
        Console.WriteLine(items[0]);
        Console.ResetColor();

        for (int i = 1; i < itemCount; i++)
        {
            Console.WriteLine(items[i]);
        }
    }

    public static string NavigateMenu(string title, string[] items)
    {
        int count = items.Count();

        if (count == 0)
            return "";

        string selection = "";
        int index = 0;
        int maxIndex = count - 1;
        int indexDelta = 0;
        int cursorPos = title.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Count();
        int cursorPosDelta = 0;

        while (true)
        {
            ConsoleKey key = Console.ReadKey(true).Key;

            if ((key == ConsoleKey.S ||
                key == ConsoleKey.DownArrow) && index < maxIndex)
            {
                cursorPosDelta = items[index].Split(Environment.NewLine).Count();
                indexDelta = 1;
            }
            else if ((key == ConsoleKey.W ||
                     key == ConsoleKey.UpArrow) && index > 0)
            {
                cursorPosDelta = -items[index - 1].Split(Environment.NewLine).Count();
                indexDelta = -1;
            }
            else if (key == ConsoleKey.Enter)
            {
                selection = items[index];
                break;
            }

            if (indexDelta != 0)
            {
                Console.SetCursorPosition(0, cursorPos);
                Console.WriteLine(items[index]);
                index += indexDelta;
                cursorPos += cursorPosDelta;
                Console.SetCursorPosition(0, cursorPos);
                Console.ForegroundColor = selectionColor;
                Console.WriteLine(items[index]);
                Console.ResetColor();
                indexDelta = 0;
                cursorPosDelta = 0;
            }
        }

        return selection;
    }

    public static string CreatePayLoad()
    {
        void ClearValidationFeedback(int errMsgHeight)
        {
            int errorLine = Console.CursorTop - errMsgHeight - 1;

            for (int i = Console.CursorTop - 1; i >= errorLine; i--)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(new string(' ', Console.WindowWidth));
            }

            Console.SetCursorPosition(0, errorLine);
        }

        List<(string, string?)> keyValuePairs = new();

        string? attribName;
        string? attribValue;

        while (true)
        {
            Console.Clear();

            while (true)
            {
                Console.CursorVisible = true;
                Console.Write("Syötä ominaisuuden nimi: ");
                attribName = Console.ReadLine();
                Console.CursorVisible = false;

                if (!string.IsNullOrWhiteSpace(attribName) && attribName.All(char.IsLetter))
                    break;

                Console.WriteLine("\nVirheellinen nimi:");
                Console.WriteLine("Saa sisältää vain kirjaimia");
                Console.ReadKey(true);
                ClearValidationFeedback(3);
            }

            while (true)
            {
                Console.CursorVisible = true;
                Console.Write("Syötä ominaisuuden arvo: ");
                attribValue = Console.ReadLine();
                Console.CursorVisible = false;

                if (attribValue == "" || (!string.IsNullOrWhiteSpace(attribValue) && attribValue.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))))
                    break;

                Console.WriteLine("\nVirheellinen arvo:");
                Console.WriteLine("Saa sisältää vain kirjaimia, numeroita tai välilyöntejä");
                Console.ReadKey(true);
                ClearValidationFeedback(3);
            }

            if (attribValue == "")
            {
                attribValue = null;
            }

            keyValuePairs.Add((attribName.Trim(), attribValue?.Trim()));

            Console.Clear();

            string question = ("Lisätäänkö vielä ominaisuuksia?");
            string[] choices = { "Kyllä", "Ei" };

            PrintMenu(question, choices);
            string choice = NavigateMenu(question, choices);

            if (choice == "Ei")
            {
                Console.Clear();
                break;
            }

            Console.WriteLine();
        }

        StringBuilder sb = new(50); // TODO: Find out how to efficiently calculate List<ValueTuple<string,string> size in characters

        sb.Append("{");

        foreach ((string Key, string? Value) keyValuePair in keyValuePairs)
        {
            string? value = keyValuePair.Value;

            string quoteWrapper = (value is null || double.TryParse(value, out _)) ? "" : "\"";

            sb.Append($"\"{keyValuePair.Key}\":{quoteWrapper}{value}{quoteWrapper},");
        }

        sb.Append("}");

        return sb.ToString();
    }

    public static async Task<(bool Success, string? ObjectID)> ChooseObject(string url)
    {
        string StripJsonSyntaxChars(string json)
        {
            HashSet<char> JsonSyntaxChars = new() {
                ',', '"', '{', '}', '[', ']'
            };

            StringBuilder sb = new(json.Length);

            foreach (char c in json)
            {
                if (!JsonSyntaxChars.Contains(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }

        string[] CreateMenuFromObjects(string strippedJson)
        {
            List<string> items = new();

            StringBuilder sb = new(strippedJson.Length);
            bool prevLineWasEmpty = true;

            foreach (string line in strippedJson.Split(Environment.NewLine, StringSplitOptions.None))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine(line.Trim());
                    prevLineWasEmpty = false;
                }
                else
                {
                    if (!prevLineWasEmpty)
                    {
                        sb.Length = sb.ToString().LastIndexOf(Environment.NewLine);
                        items.Add(sb.ToString());
                        sb.Clear();
                    }

                    prevLineWasEmpty = true;
                }
            }

            items.Add("Palaa");

            return items.ToArray();
        }

        string json;

        try
        {
            json = await APIClient.HTTPRequest(url, "GET");
        }
        catch (HttpRequestException e)
        {
            throw new HttpRequestException($"Hakeminen epäonnistui:\n{e.Message}");
        }

        string prettyJson = JSONPrettyPrint(json);
        string strippedJson = StripJsonSyntaxChars(prettyJson);

        string title = "---- Valitse olio (↑,↓) ----";
        string[] objects = CreateMenuFromObjects(strippedJson);

        PrintMenu(title, objects);
        string selection = NavigateMenu(title, objects);

        if (selection == "Palaa")
            return (false, null);

        if (!selection.Contains("_id"))
            throw new InvalidDataException("Olion resurssitunnusta (_id) ei löytynyt");

        int idStart = selection.IndexOf("_id") + 5;

        int idEnd = selection.Contains(Environment.NewLine) ? selection.Substring(idStart - 5).IndexOf(Environment.NewLine) : selection.Length;

        int idLength = idEnd - idStart;
        string id = selection.Substring(idStart, idLength);

        return (true, id);
    }

    public static string JSONPrettyPrint(string json)
    {
        JArray parsedJson;

        try
        {
            parsedJson = JArray.Parse(json);
            return parsedJson.ToString(Newtonsoft.Json.Formatting.Indented);
        }
        catch (JsonSerializationException) // Or take into account null exception with base class JsonException?
        {
            throw new JsonSerializationException("JSON merkkijonon jäsentämisessä muotoon JArray tapahtui virhe");
        }
    }

    public static void PrintMultiline(string multilineStr)
    {
        foreach (string line in multilineStr.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
            Console.WriteLine(line);
    }

    public static async Task LoadingAnimation(CancellationToken token, string loadingTitle)
    {
        int maxLength = loadingTitle.Length + 3;
        StringBuilder sb = new(maxLength);
        sb.Append(loadingTitle);

        while (!token.IsCancellationRequested)
        {
            if (sb.Length > maxLength)
            {
                sb.Remove(sb.Length - 4, 4);
                Console.Write($"{loadingTitle}{new string(' ', Console.WindowWidth - loadingTitle.Length)}");
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            else
            {
                Console.Write(sb.ToString());
                Console.SetCursorPosition(0, Console.CursorTop);
                sb.Append('.');
            }

            await Task.Delay(250, token);
        }
    }
}
