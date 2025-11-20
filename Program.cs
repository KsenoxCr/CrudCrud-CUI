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
        CrudCrud CUI Työkalu
        --------------------

        """;
            string[] startMenu = { "----> Aloita <----", "----> Lopeta <----" };

            Menu.PrintMenu(startTitle, startMenu);

            string selection = Menu.NavigateMenu(startTitle, startMenu);

            if (selection == startMenu[1])
                return;

            string optionsTitle = "--- Valitse toiminto (↑,↓) ---";
            string[] options = { "Hae", "Lisää", "Muokkaa", "Poista", "Lopeta" };

            string endpoint = args[0];
            string resource = args[1];
            string url = $"https://crudcrud.com/api/{endpoint}/{resource}";

            while (true)
            {
                Menu.PrintMenu(optionsTitle, options);

                selection = Menu.NavigateMenu(optionsTitle, options);

                switch (selection)
                {
                    case "Lisää":
                        try
                        {
                            await APIClient.HTTPRequest(url, "POST", null, Menu.CreatePayLoad());
                        }
                        catch (HttpRequestException e)
                        {
                            Console.Clear();
                            Console.WriteLine("Lisääminen epäonnistui sillä");
                            Console.WriteLine(e.Message);
                            return;
                        }
                        Console.Clear();
                        Console.WriteLine("Lisääminen onnistui!");
                        break;
                    case "Muokkaa":
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
                            Console.WriteLine("Muokkaaminen epäonnistui sillä");
                            Console.WriteLine(e.Message);
                            return;
                        }
                        Console.WriteLine("Muokkaaminen onnistui!");
                        break;
                    case "Poista":
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
                            Console.WriteLine("Poistaminen epäonnistui sillä");
                            Console.WriteLine(e.Message);
                            return;
                        }
                        Console.Clear();
                        Console.WriteLine("Poistaminen onnistui!");
                        break;
                    case "Lopeta":
                        return;
                }

                long loadingDelay = 3000;

                Stopwatch timer = new();
                timer.Start();

                if (selection == "Hae")
                    Console.Clear();
                else
                    Console.WriteLine();

                CancellationTokenSource cts = new();

                Task loading = Task.Run(() => Menu.LoadingAnimation(cts.Token, "Haetaan tietoja"));

                string response;

                try
                {
                    response = await APIClient.HTTPRequest(url, "GET");
                }
                catch (HttpRequestException e)
                {
                    Console.Clear();
                    Console.WriteLine("Hakeminen epäonnistui sillä");
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
                    Console.WriteLine("Yhtään oliota ei ole vielä luotu");
                else
                    Menu.PrintMultiline(Menu.JSONPrettyPrint(response));

                Console.ForegroundColor = Menu.selectionColor;
                Console.WriteLine("\nPaina mitä tahansa näppäintä jatkaaksesi...");
                Console.ReadKey(true);
                Console.ResetColor();
            }
        }
        catch (Exception e)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Käsittelemätön poikkeus metodissa Main: {e.Message}");
            Console.WriteLine(e.StackTrace);
            Console.ResetColor();
        }
    }
}
