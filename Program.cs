using System.Diagnostics;

namespace CrudCrudCUI;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: CrudCrudCUI <endpoint> <resource>");
            return;
        }

        try
        {
            Console.CursorVisible = false;

            string startTitle = """
        CrudCrud CUI Tool
        -----------------

        """;
            string[] startMenu = { "----> Start <----", "----> Exit <----" };

            Menu.PrintMenu(startTitle, startMenu);

            string selection = Menu.NavigateMenu(startTitle, startMenu);

            if (selection == startMenu[1])
                return;

            string optionsTitle = "--- Select action (↑,↓) ---";
            string[] options = { "Fetch", "Add", "Edit", "Delete", "Exit" };

            string endpoint = args[0];
            string resource = args[1];
            string url = $"https://crudcrud.com/api/{endpoint}/{resource}";

            while (true)
            {
                Menu.PrintMenu(optionsTitle, options);

                selection = Menu.NavigateMenu(optionsTitle, options);

                switch (selection)
                {
                    case "Add":
                        try
                        {
                            await APIClient.HTTPRequest(url, "POST", null, Menu.CreatePayLoad());
                        }
                        catch (HttpRequestException e)
                        {
                            Console.Clear();
                            Console.WriteLine("Adding failed because");
                            Console.WriteLine(e.Message);
                            return;
                        }
                        Console.Clear();
                        Console.WriteLine("Adding succeeded!");
                        break;
                    case "Edit":
                        try
                        {
                            var result = await Menu.ChooseObject(url);

                            if (!result.Success)
                                continue;

                            await APIClient.HTTPRequest(url, "PUT", result.ObjectID, Menu.CreatePayLoad());
                        }
                        catch (HttpRequestException e)
                        {
                            Console.Clear();
                            Console.WriteLine("Editing failed because");
                            Console.WriteLine(e.Message);
                            return;
                        }
                        Console.WriteLine("Editing succeeded!");
                        break;
                    case "Delete":
                        try
                        {
                            var result = await Menu.ChooseObject(url);

                            if (!result.Success)
                                continue;

                            await APIClient.HTTPRequest(url, "DELETE", result.ObjectID, null);
                        }
                        catch (HttpRequestException e)
                        {
                            Console.Clear();
                            Console.WriteLine("Deletion failed because");
                            Console.WriteLine(e.Message);
                            return;
                        }
                        Console.Clear();
                        Console.WriteLine("Deletion succeeded!");
                        break;
                    case "Exit":
                        return;
                }

                long loadingDelay = 3000;

                Stopwatch timer = new();
                timer.Start();

                if (selection == "Fetch")
                    Console.Clear();
                else
                    Console.WriteLine();

                CancellationTokenSource cts = new();

                Task loading = Task.Run(() => Menu.LoadingAnimation(cts.Token, "Fetching data"));

                string response;

                try
                {
                    response = await APIClient.HTTPRequest(url, "GET");
                }
                catch (HttpRequestException e)
                {
                    Console.Clear();
                    Console.WriteLine("Fetching failed because");
                    Console.WriteLine(e.Message);
                    return;
                }

                timer.Stop();

                if (timer.ElapsedMilliseconds < loadingDelay)
                    await Task.Delay((int)(loadingDelay - timer.ElapsedMilliseconds));

                cts.Cancel();

                try
                {
                    await loading;
                }
                catch (OperationCanceledException) { }

                Console.Clear();

                if (response == "[]")
                    Console.WriteLine("No objects created yet");
                else
                    Menu.PrintMultiline(Menu.JSONPrettyPrint(response));

                Console.ForegroundColor = Menu.selectionColor;
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey(true);
                Console.ResetColor();
            }
        }
        catch (Exception e)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unhandled exception in Main method: {e.Message}");
            Console.WriteLine(e.StackTrace);
            Console.ResetColor();
        }
    }
}
