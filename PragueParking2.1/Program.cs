using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
using Dapper;
using System.Threading;

namespace PragueParking2._1
{
    class Program
    {
        //auto-maximize window
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        private static IntPtr ThisConsole = GetConsoleWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int HIDE = 0;
        private const int MAXIMIZE = 3;
        private const int MINIMIZE = 6;
        private const int RESTORE = 9;

        //initiate parkinglot
        public static List<ParkingSpot> ParkingLot = new List<ParkingSpot>();

        //Load config file
        public static Config config = LoadConfigFile();
        static void Main(string[] args)
        {
            //auto-maximize window
            Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
            ShowWindow(ThisConsole, MAXIMIZE);

            //Create the accurate amount of parkingspots
            for (int i = 0; i < config.ParkingSpotsAmount; i++)
            {
                ParkingLot.Add(new ParkingSpot(i, config.ParkingSpotSize));
            }

            //Load storagefile with stored vehicles
            LoadVehicles(config);

            //Login
            bool loggedIn = true;
            while (!loggedIn)
            {
                loggedIn = LogIn();
            }

            //Main menu
            bool menuIsActive = true;
            while (menuIsActive)
            {
                Console.Clear();
                var appHeading = new Rule("PRAGUE PARKING 2.0");
                AnsiConsole.Render(appHeading);
                var menuOption = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("What do you want to do?")
                        .PageSize(10)
                        .AddChoices(new[] {
                            "Park vehicle",
                            "Repark vehicle",
                            "Depark vehicle",
                            "Reload configuration file",
                            "Show pricelist",
                            "Show list of occupied parkingspots",
                            "Show map",
                            "Generate vehicles",
                            "Exit program"
                        }));
                switch (menuOption)
                {
                    case "Park vehicle":
                        ParkVehicle(config);
                        break;
                    case "Repark vehicle":
                        ReparkVehicle();
                        break;
                    case "Depark vehicle":
                        DeparkVehicle();
                        break;
                    case "Reload configuration file":
                        config = LoadConfigFile();
                        Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.WindowHeight / 2 - 2);
                        AnsiConsole.Markup("[springgreen4]Reloaded the configuration file.. [/]");
                        Console.WriteLine();
                        Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                        AnsiConsole.MarkupLine("Press [springgreen4]any key[/] to go back to the main menu..");
                        Console.ReadKey();
                        break;
                    case "Show pricelist":
                        ShowPriceList();
                        break;
                    case "Show list of occupied parkingspots":
                        ShowParkingLotList();
                        break;
                    case "Show map":
                        renderParkingOverview(ParkingLot);
                        break;
                    case "Generate vehicles":
                        GenerateVehicles();
                        break;
                    case "Exit program":
                        ExitProgram();
                        break;
                    default:
                        Console.WriteLine("Faulty choice");
                        Console.ReadLine();
                        break;
                }
            }
        }

        public static void ExitProgram()
        {
            Console.SetCursorPosition((Console.WindowWidth - 33) / 2, Console.WindowHeight / 2);
            if (!AnsiConsole.Confirm("Are you sure you want to exit?"))
            {
                return;
            }
            else
            {
                WriteToStorage();
                Console.SetCursorPosition((Console.WindowWidth - 33) / 2, Console.CursorTop);
                AnsiConsole.Markup("[springgreen3]All data saved[/], you may now exit the program..");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }
        public static void ShowParkingLotList()
        {
            Console.WriteLine();
            Console.WriteLine();

            // Create a table and colums
            var table = new Table();
            table.Border = TableBorder.HeavyEdge;
            table.Centered();
            table.AddColumn(new TableColumn(new Markup("[bold yellow]Type: [/]")));
            table.AddColumn(new TableColumn(new Markup("[bold yellow]Registration Number: [/]")));
            table.AddColumn(new TableColumn(new Markup("[bold yellow]Token: [/]")));
            table.AddColumn(new TableColumn(new Markup("[bold yellow]Parking Spot: [/]")));
            table.AddColumn(new TableColumn(new Markup("[bold yellow]Parked Since: [/]")));
            table.AddColumn(new TableColumn(new Markup("[bold yellow]Accumulated Cost: [/]")));

            //Loop through parkingspots in parkinglot

            for(int j = 0; j < ParkingLot.Count; j++)
            {
                    //Check which parkingspots have vehicles parked on them
                    if (ParkingLot[j].FreeSpace != Program.config.ParkingSpotSize)
                    {
                        foreach (var vehicle in ParkingLot[j].ListParkedVehicles())
                        {
                            float charge = ParkingLot[j].calculateCharge(ParkingLot[j].findIndexOfVehicle(vehicle.Token));
                            if (vehicle is Bicycle)
                            {
                                table.AddRow($"[hotpink2]Bicycle [/]", $"", $"[hotpink2]{vehicle.Token} [/]", $"[hotpink2]{vehicle.ParkedAt} [/]", $"[hotpink2]{vehicle.ParkedSince} [/]", $"[indianred_1]CZK: {charge} [/]");
                            }
                            else if (vehicle is Mc)
                            {
                                table.AddRow($"[orchid]Mc [/]", $"[orchid]{vehicle.RegistrationNumber} [/]", $"[orchid]{vehicle.Token} [/]", $"[orchid]{vehicle.ParkedAt} [/]", $"[orchid]{vehicle.ParkedSince} [/]", $"[indianred_1]CZK: {charge} [/]");
                            }
                            else if (vehicle is Car)
                            {
                                table.AddRow($"[mediumorchid1]Car [/]", $"[mediumorchid1]{vehicle.RegistrationNumber} [/]", $"[mediumorchid1]{vehicle.Token} [/]", $"[mediumorchid1]{vehicle.ParkedAt} [/]", $"[mediumorchid1]{vehicle.ParkedSince} [/]", $"[indianred_1]CZK: {charge} [/]");
                            }
                            else if (vehicle is Bus)
                            {
                                j = j + (config.BusSize / config.ParkingSpotSize);
                                table.AddRow($"[cadetblue_1]Bus [/]", $"[cadetblue_1]{vehicle.RegistrationNumber} [/]", $"[cadetblue_1]{vehicle.Token} [/]", $"[cadetblue_1]{vehicle.ParkedAtSeveral[0]} [/]", $"[cadetblue_1]{vehicle.ParkedSince} [/]", $"[indianred_1]CZK: {charge} [/]");
                                for (int i = 1; i < config.BusSize / config.ParkingSpotSize; i++)
                                {
                                    table.AddRow($"", $"", $"", $"[cadetblue_1]{vehicle.ParkedAtSeveral[i]} [/]", $"", $"");
                                }
                            }
                        }
                    }
                }
            AnsiConsole.Render(table);
            Console.WriteLine();
            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
            AnsiConsole.MarkupLine("Press [springgreen4]any key[/] to go back to the main menu..");
            Console.ReadLine();
        }

        //Show pricelist
        public static void ShowPriceList()
        {
            //Create a table and colums
            var table = new Table();
            table.Border = TableBorder.HeavyEdge;
            table.Centered();
            table.AddColumn(new TableColumn(new Markup("[bold yellow]Type: [/]")));
            table.AddColumn(new TableColumn(new Markup("[bold yellow]Price: [/]")));
            foreach (var vehicleType in Program.config.VehicleTypes)
            {
                table.AddRow($"[hotpink2]{vehicleType.Name} [/]", $"[hotpink2]{vehicleType.Price} [/]");
            }
            //center the chart diagonally
            Console.SetCursorPosition(0, (Console.WindowHeight - Program.config.VehicleTypes.Count) / 2);
            AnsiConsole.Render(table);
            Console.ReadKey();
        }
        //Silly login script
        public static bool LogIn()
        {
            Console.Clear();
            var loginHeading = new Rule("Please login - Prague Parking 2.0");
            AnsiConsole.Render(loginHeading);
            string s = "Hello|World";
            Console.SetCursorPosition((Console.WindowWidth - s.Length) / 2, Console.CursorTop);
            Console.SetCursorPosition((Console.WindowWidth - 9) / 2, Console.WindowHeight / 2 - 2);
            string username = AnsiConsole.Ask<string>("[green]Username[/]: ");
            Console.SetCursorPosition((Console.WindowWidth - 9) / 2, Console.CursorTop);
            var password = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Password[/]: ")
            .PromptStyle("red")
            .Secret());
            if (username == "admin" && password == "password")
            {
                Console.WriteLine();
                Console.SetCursorPosition((Console.WindowWidth - 4) / 2, Console.CursorTop);
                Console.WriteLine("Success!");
                Console.ReadKey();
                return true;
            }
            else
            {
                Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                Console.WriteLine("Wrong username/password, try again please.");
                Console.ReadKey();
                return false;
            }
        }
        //Repark vehicle
        public static void ReparkVehicle()
        {
            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.WindowHeight / 2 - 2);
            var regOrToken = AnsiConsole.Ask<string>("What's the vehicle [green]registration number or token?[/]");
            int[] indexOfVehicle = FindVehicle(regOrToken);
            if (indexOfVehicle[0] == -1)
            {
                Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                AnsiConsole.Markup("[underline darkred_1]No vehicle found..[/] ");
                Console.ReadKey();
            }
            else
            {
                int newParkingSpot;
                bool parseSuccess;
                var vehicle = ParkingLot[indexOfVehicle[0]].ParkedVehiclesOnSpot[indexOfVehicle[1]];
                Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                var newParkingSpotString = AnsiConsole.Ask<string>("Where do you want to park it? (Starting if you're parking a bus)");
                parseSuccess = int.TryParse(newParkingSpotString, out newParkingSpot);
                if (parseSuccess)
                {
                    if(newParkingSpot > 50 || newParkingSpot > config.ParkingSpotsAmount)
                    {
                        Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                        AnsiConsole.MarkupLine("[red3]This parking spot is not available for a bus...[/]");
                        Console.ReadKey();
                        return;
                    }
                    if(vehicle is Bus)
                    {
                        int[] newParkingSpots = new int[config.BusSize / config.ParkingSpotSize];
                        newParkingSpots = FindFreeBusSpots(newParkingSpot);
                        if(newParkingSpots.Contains(-1))
                        {
                            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                            AnsiConsole.MarkupLine("[red3]This parking spot is not available for a bus...[/]");
                            Console.ReadKey();
                        } else
                        {
                            for(int i = 0; i < newParkingSpots.Length; i++)
                            {
                                ParkingLot[indexOfVehicle[0] +i].FreeSpace = config.ParkingSpotSize;
                            }
                            vehicle.ParkedAtSeveral = newParkingSpots;
                            ParkBus((Bus)vehicle, newParkingSpots, config);
                            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                            AnsiConsole.MarkupLine($"Successfully reparked [green]{vehicle.RegistrationNumber}[/]");
                            WriteToStorage();
                            Console.ReadKey();
                        }
                        

                    } else
                    {
                        int vehicleSize = ParkingLot[newParkingSpot].getVehicleSize(vehicle);
                        bool availableSpace = ParkingLot[newParkingSpot].checkForFreeSpace(vehicleSize);
                        if (availableSpace)
                        {
                            ParkingLot[indexOfVehicle[0]].removeVehicle(indexOfVehicle[1]);
                            vehicle.ParkedAt = newParkingSpot;
                            if (vehicle is Bicycle)
                            {
                                ParkBicycle((Bicycle)vehicle, newParkingSpot, config);
                            }
                            if (vehicle is Mc)
                            {
                                ParkMc((Mc)vehicle, newParkingSpot, config);
                            }
                            if (vehicle is Car)
                            {
                                ParkCar((Car)vehicle, newParkingSpot, config);
                            }
                            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                            AnsiConsole.Markup($"Successfully reparked the vehicle on parkingspot: [underline springgreen3]{newParkingSpot}[/]");
                            Console.ReadKey();
                        }
                        else
                        {
                            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                            AnsiConsole.Markup("[underline darkred_1]No available space here, sorry!..[/]");
                            Console.ReadKey();
                        }
                    }           
                }
                else
                {
                    Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                    AnsiConsole.Markup("Please enter a [underline darkred_1]valid number..[/]");
                    Console.ReadKey();
                }
                Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                AnsiConsole.MarkupLine("Press [springgreen4]any key[/] to go back to the main menu..");
                WriteToStorage();
            }
        }

        //Write stored vehicles to the storage-file
        public static void WriteToStorage()
        {
            string vehicleJsonData = JsonConvert.SerializeObject(ParkingLot);
            string filePath = @"../../../storedvehicles2.1.json";
            StreamWriter sw = new StreamWriter(filePath);
            sw.Write(vehicleJsonData);
            sw.Close();
        }

        //Validate registration info according to czech standards
        public static string validateRegInfo(string regInfo)
        {
            if (regInfo.Length < 7)
            {
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.WindowHeight / 2 - 3);
                AnsiConsole.Markup($"Please enter a valid registration number - [underline darkred_1]Min 7 characters[/]");
                return "error";
            }
            else if (regInfo.Length > 7)
            {
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.WindowHeight / 2 - 3);
                AnsiConsole.Markup($"Please enter a valid registration number - [underline darkred_1]Max 7 characters[/]");
                return "error";
            }
            else if (!Regex.IsMatch(regInfo, "^[a-zA-Z0-9À-ž]*$"))
            {
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.WindowHeight / 2 - 3);
                AnsiConsole.Markup($"Your registration number can [underline darkred_1]only contain Letters and numbers[/]");
                return "error";
            }
            else
            {
                return regInfo;
            }
        }

        //Load the stored vehicles from storagefile
        public static void LoadVehicles(Config config)
        {
            string storagePath = @"../../../storedvehicles2.1.json";
            using (StreamReader reader = new StreamReader(storagePath))
            {
                //read file and deserialize the data
                string json = reader.ReadToEnd();
                if (json.Length > 1)
                {
                    dynamic dynJson = JsonConvert.DeserializeObject(json);
                    //Populate the parkinglot-list accordingly
                    for (int i = 0; i < dynJson.Count; i++)
                    {
                        if (dynJson[i].ParkedVehiclesOnSpot.Count > 0)
                        {
                            foreach (var vehicle in dynJson[i].ParkedVehiclesOnSpot)
                            {
                                if (vehicle.identifier == "bicycle")
                                {
                                    string owner = vehicle.Owner;
                                    string color = vehicle.Color;
                                    int parkedAt = vehicle.ParkedAt;
                                    DateTime parkedSince = vehicle.ParkedSince;
                                    string token = vehicle.Token;
                                    ParkBicycle(new Bicycle(owner, color, parkedAt, parkedSince, token), parkedAt, config);
                                }
                                if (vehicle.identifier == "mc")
                                {
                                    string brand = vehicle.Brand;
                                    string model = vehicle.Model;
                                    string registrationNumber = vehicle.RegistrationNumber;
                                    string owner = vehicle.Owner;
                                    int parkedAt = vehicle.ParkedAt;
                                    DateTime parkedSince = vehicle.ParkedSince;
                                    string token = vehicle.Token;
                                    ParkMc(new Mc(registrationNumber, owner, brand, model, parkedAt, parkedSince, token), parkedAt, config);
                                }
                                if (vehicle.identifier == "car")
                                {
                                    string brand = vehicle.Brand;
                                    string model = vehicle.Model;
                                    string registrationNumber = vehicle.RegistrationNumber;
                                    string owner = vehicle.Owner;
                                    int parkedAt = vehicle.ParkedAt;
                                    DateTime parkedSince = vehicle.ParkedSince;
                                    string token = vehicle.Token;
                                    ParkCar(new Car(registrationNumber, owner, brand, model, parkedAt, parkedSince, token), parkedAt, config);
                                }
                                if (vehicle.identifier == "bus")
                                {
                                    string registrationNumber = vehicle.RegistrationNumber;
                                    int[] parkedAtSeveral = new int[vehicle.ParkedAtSeveral.Count];
                                    for (int k = 0; k < vehicle.ParkedAtSeveral.Count; k++)
                                    {
                                        parkedAtSeveral[k] = vehicle.ParkedAtSeveral[k];
                                    }
                                    DateTime parkedSince = vehicle.ParkedSince;
                                    string token = vehicle.Token;
                                    ParkBus(new Bus(registrationNumber, parkedAtSeveral, parkedSince, token), parkedAtSeveral, config);
                                }
                            }
                        }
                    }
                    Console.WriteLine("Storage loaded...");
                }
                else
                {
                    Console.WriteLine("No/invalid storagefile");
                }
            }

        }
        //Find a vehicle
        public static int[] FindVehicle(string input)
        {
            int[] indexes = new int[2];
            foreach (ParkingSpot pspot in ParkingLot)
            {
                foreach (Vehicle vehicle in pspot.ListParkedVehicles())
                {
                    if (vehicle.Token == input || vehicle.RegistrationNumber == input)
                    {
                        int vehicleIndex = pspot.findIndexOfVehicle(input);
                        indexes[0] = pspot.ParkingSpotNumber;
                        indexes[1] = vehicleIndex;
                        return indexes;
                    }
                }
            }
            indexes[0] = -1;
            indexes[1] = -1;
            return indexes;
        }
        //Depark a vehicle
        public static void DeparkVehicle()
        {
            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.WindowHeight / 2 - 2);
            var regInfoOrvehicleToken = AnsiConsole.Ask<string>("What's the vehicle [green]registration number or token?[/]");
            //if (regInfoOrvehicleToken.Length == 10)
            //{
                foreach (ParkingSpot pspot in ParkingLot)
                {
                foreach (Vehicle vehicle in pspot.ListParkedVehicles())
                {
                    if (vehicle.Token == regInfoOrvehicleToken | vehicle.RegistrationNumber == regInfoOrvehicleToken)
                    {
                        float charge;
                        if (vehicle is Bus)
                        {
                            charge = pspot.deParkVehicleFromSpot(pspot.findIndexOfVehicle(regInfoOrvehicleToken), config);
                            for (int i = 1; i < vehicle.ParkedAtSeveral.Length; i++)
                            {
                                ParkingLot[vehicle.ParkedAtSeveral[i]].deParkVehicleFromSpot(0, config);
                            }

                        }
                        else
                        {
                            charge = pspot.deParkVehicleFromSpot(pspot.findIndexOfVehicle(regInfoOrvehicleToken), config);
                        }
                        Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                        AnsiConsole.Markup($"Please charge the customer: [green]{charge}[/] CZK");
                        WriteToStorage();
                        Console.WriteLine();
                        Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
                        AnsiConsole.MarkupLine("Press [springgreen4]any key[/] to go back to the main menu..");
                        Console.ReadKey();
                        return;
                    }
                }
            }
            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
            AnsiConsole.Markup($"[underline darkred_1]No vehicle found..[/]");
            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
            AnsiConsole.MarkupLine("Press [springgreen4]any key[/] to go back to the main menu..");
            Console.ReadKey();
            //}
            //else if (regInfoOrvehicleToken.Length == 7)
            //{
            //    foreach (ParkingSpot pspot in ParkingLot)
            //    {
            //        foreach (Vehicle vehicle in pspot.ListParkedVehicles())
            //        {
            //            if (vehicle.RegistrationNumber == regInfoOrvehicleToken)
            //            {
            //                float charge = pspot.deParkVehicleFromSpot(pspot.findIndexOfVehicle(regInfoOrvehicleToken), config);
            //                Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
            //                AnsiConsole.Markup($"Please charge the customer: [green]{charge}[/] CZK");
            //                Console.ReadKey();
            //                WriteToStorage();
            //                return;
            //            }
            //        }
            //    }

            //}

        }
        //Load and deserialize the config file, create a new object of the class Configuration
        public static Config LoadConfigFile()
        {

            string storagePath = @"../../../config2.1.json";
            using (StreamReader reader = new StreamReader(storagePath))
            {
                string json = reader.ReadToEnd();
                Config config = JsonConvert.DeserializeObject<Config>(json);
                return config;
            }
        }
        //Park a vehicle
        public static void ParkVehicle(Config config)
        {
            //RENDER MENU
            var table = new Table();
            table.AddColumn("Menu");
            var menuOption = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("What type of vehicle?")
                        .PageSize(10)
                        .AddChoices(new[] {
                            "Bicycle",
                            "Mc",
                            "Car",
                            "Bus"
                        })); ;
            //Collect information from user
            if (menuOption == "Bicycle")
            {
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.WindowHeight / 2 - 3);
                var bikeOwner = AnsiConsole.Prompt(
                    new TextPrompt<string>("[grey][[Optional]][/] [dodgerblue3]What's the owners name?[/]")
                    .AllowEmpty());
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                var bikeColor = AnsiConsole.Prompt(
                new TextPrompt<string>("[grey][[Optional]][/] [dodgerblue3]What's the color of the bicycle?[/]")
                .AllowEmpty());
                int bikeSpot = findFreeSpot("bicycle", config);
                string token = ParkBicycle(new Bicycle(bikeOwner, bikeColor, bikeSpot, DateTime.Now), bikeSpot, config);
                Console.WriteLine();
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                Console.WriteLine($"Successfully parked the bicycle at: {bikeSpot}");
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                AnsiConsole.Markup($"Token for deparking: [underline springgreen3]{token}[/]");
                Console.ReadKey();
            }
            else if(menuOption == "Bus")
            {
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.WindowHeight / 2 - 5);
                string registrationNumber = validateRegInfo(AnsiConsole.Ask<string>("What's the vehicle [green]registration number?[/]"));
                if (registrationNumber == "error")
                {
                    Console.ReadKey();
                    return;
                }
                int[] freeSpots = FindFreeBusSpots();
                if(freeSpots.Last() > 50 || freeSpots.Last() > config.ParkingSpotsAmount || freeSpots.Any(num => num == -1))
                {
                    Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                    AnsiConsole.MarkupLine($"[red]Sorry, there is no free parking spots for buses.[/]");
                    Console.WriteLine();
                    Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                    AnsiConsole.MarkupLine("Press [springgreen4]any key[/] to go back to the main menu..");
                    Console.ReadKey();
                    return;
                }
                if(freeSpots.Length == config.BusSize / config.ParkingSpotSize)

                {
                    Bus bus = new Bus(registrationNumber, freeSpots, DateTime.Now);
                    string busToken =  ParkBus(bus, freeSpots, config);
                    WriteToStorage();
                    Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                    AnsiConsole.Markup($"[springgreen3]Successfully[/] parked the bus at parking spots:");
                    for(int i = 0; i < freeSpots.Length; i++)
                    {
                        if(i == freeSpots.Length)
                        {
                            AnsiConsole.Markup($"[springgreen3]{freeSpots[i]} [/]");
                        } else
                        {
                            AnsiConsole.Markup($"[springgreen3]{freeSpots[i]}, [/]");
                        }
                        
                    }
                    Console.WriteLine();
                    Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                    AnsiConsole.MarkupLine($"Token for deparking: [underline springgreen3]{busToken}[/]");
                }
                Console.WriteLine();
                Console.WriteLine();
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                AnsiConsole.MarkupLine("Press [springgreen4]any key[/] to go back to the main menu..");
                Console.ReadKey();
            }
            else
            {
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.WindowHeight / 2 - 5);
                string registrationNumber = validateRegInfo(AnsiConsole.Ask<string>("What's the vehicle [green]registration number?[/]"));
                if (registrationNumber == "error")
                {
                    Console.ReadKey();
                    return;
                }
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                var owner = AnsiConsole.Prompt(
                    new TextPrompt<string>("[grey][[Optional]][/] [dodgerblue3]What's the owners name?[/]")
                    .AllowEmpty());
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                var brand = AnsiConsole.Prompt(
                    new TextPrompt<string>("[grey][[Optional]][/] [dodgerblue3]What's the brand of the vehicle?[/]")
                    .AllowEmpty());
                Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                var model = AnsiConsole.Prompt(
                    new TextPrompt<string>("[grey][[Optional]][/] [dodgerblue3]Of what model is the vehicle?[/]")
                    .AllowEmpty());
                Console.WriteLine();

                switch (menuOption)
                {
                    case "Mc":
                        int mcSpot = findFreeSpot("mc", config);
                        string mcToken = ParkMc(new Mc(registrationNumber, owner, brand, model, mcSpot, DateTime.Now), mcSpot, config);

                        Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                        Console.WriteLine($"Successfully parked the mc at parking spot: {mcSpot}");
                        Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                        AnsiConsole.Markup($"Token for deparking: [underline springgreen3]{mcToken}[/]");
                        Console.ReadKey();
                        break;
                    case "Car":
                        int carSpot = findFreeSpot("car", config);
                        string carToken = ParkCar(new Car(registrationNumber, owner, brand, model, carSpot, DateTime.Now), carSpot, config);
                        Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                        Console.WriteLine($"Successfully parked the mc at parking spot: {carSpot}");
                        Console.SetCursorPosition((Console.WindowWidth - 40) / 2, Console.CursorTop);
                        AnsiConsole.Markup($"Token for deparking: [underline springgreen3]{carToken}[/]");
                        Console.ReadKey();
                        break;
                }
            }
            //Write to the storage file
            WriteToStorage();
        }
        public static string ParkBus(Bus bus, int[] spots, Config config)
        {
            for(int i = 0; i < spots.Length; i++)
            {
                ParkingLot[spots[i]].UseParking(config.BusSize / config.ParkingSpotSize, bus);
            }
            return bus.Token;
        }
        //Populate parkingspot objects:
        public static string ParkBicycle(Bicycle bicycle, int spot, Config config)
        {
            if (spot > ParkingLot.Count)
            {
                Console.WriteLine("No more space!");
                return "error";
            }
            ParkingLot[spot].UseParking(config.BicycleSize, bicycle);
            return bicycle.Token;
        }
        public static string ParkCar(Car car, int spot, Config config)
        {
            if (spot > ParkingLot.Count)
            {
                Console.WriteLine("No more space!");
                return "error";
            }
            ParkingLot[spot].UseParking(config.CarSize, car);
            return car.Token;
        }
        public static string ParkMc(Mc mc, int spot, Config config)
        {
            if (spot > ParkingLot.Count)
            {
                Console.WriteLine("No more space!");
                return "error";
            }
            ParkingLot[spot].UseParking(config.McSize, mc);
            return mc.Token;
        }
        public static int[] FindFreeBusSpots(int space = -1)
        {
            int spacesForBus;
            int spotsNeeded = Program.config.BusSize / Program.config.ParkingSpotSize;
            int[] freeSpaces = new int[spotsNeeded];
            for (int i = 0; i < spotsNeeded; i++)
            {
                freeSpaces[i] = -1;
            }
            if (config.ParkingSpotsAmount < 50)
            {
                spacesForBus = Program.config.ParkingSpotsAmount;
            }
            else
            {
                spacesForBus = 50;
            }
            if (space == -1)
            {
                for (int i = 0; i < spacesForBus; i++)
                {
                    if (ParkingLot[i].FreeSpace == config.ParkingSpotSize)
                    {
                        for (int k = 0; k < spotsNeeded; k++)
                        {
                            if (ParkingLot[i + k].FreeSpace == config.ParkingSpotSize)
                            {
                                freeSpaces[k] = i + k;
                                if (!freeSpaces.Contains(-1))
                                {
                                    return freeSpaces;
                                }
                            } else
                            {
                                break;
                            }
                        }
                    }
                }
                return freeSpaces;

            } else
            {
                for(int i = 0; i < spotsNeeded; i++)
                {
                    if(ParkingLot[space + i].FreeSpace == 4)
                    {
                        freeSpaces[i] = space + i;
                    }
                }
                return freeSpaces;
            }

        }
        //Helper method to find a free spot where the vehicle in question will fit.
        public static int findFreeSpot(string vehicleType, Config config)
        {

            int size = 0;
            if (vehicleType == "bicycle")
            {
                size = config.BicycleSize;
            }
            if (vehicleType == "mc")
            {
                size = config.McSize;
            }
            if (vehicleType == "car")
            {
                size = config.CarSize;
            }
           
            ParkingSpot res = ParkingLot.Find(spot => spot.FreeSpace >= size);
            if (res == null)
            {
                return 999;
            }
            else
            {
                return res.ParkingSpotNumber;
            }
        }

        //Count amount of vehicles of each type
        public static int[] CountVehicles(List<ParkingSpot> parkingLot)
        {
            int[] amountOfVehicles = { 0, 0, 0, 0 };
            foreach (ParkingSpot pspot in parkingLot)
            {
                if (pspot.FreeSpace < 4)
                {
                    foreach (var vehicle in pspot.ParkedVehiclesOnSpot)
                    {
                        if (vehicle is Bicycle) amountOfVehicles[0]++;
                        if (vehicle is Mc) amountOfVehicles[1]++;
                        if (vehicle is Car) amountOfVehicles[2]++;
                        if (vehicle is Bus) amountOfVehicles[3]++;
                    }
                }
            }
            if(amountOfVehicles[3] > 0)
            {
                amountOfVehicles[3] = (config.BusSize / config.ParkingSpotSize) / amountOfVehicles[3];
            }

            return amountOfVehicles;
        }
        //Render a map
        public static void renderParkingOverview(List<ParkingSpot> parkingLot)
        {
            int[] amountOfVehicles = CountVehicles(parkingLot);
            int totalVehicles = amountOfVehicles[0] + amountOfVehicles[1] + amountOfVehicles[2];
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.SetCursorPosition((Console.WindowWidth -20) / 2, Console.CursorTop);
            AnsiConsole.MarkupLine("Map over parkinglot: ");
            Console.WriteLine();
             Console.WriteLine();
            Console.SetCursorPosition((Console.WindowWidth -69) / 2, Console.CursorTop);
            var infoCellOne = new System.Text.StringBuilder();
            infoCellOne.Append(String.Format("[white on green]"));
            infoCellOne.Append(String.Format("{0,13} {1,10}", "Free", ""));
            infoCellOne.Append(String.Format("[/]"));
            AnsiConsole.Markup(infoCellOne.ToString());
            var infoCellTwo = new System.Text.StringBuilder();
            infoCellTwo.Append(String.Format("[white on darkseagreen]"));
            infoCellTwo.Append(String.Format("{0,18} {1,5}", "One bicycle", ""));
            infoCellTwo.Append(String.Format("[/]"));
            AnsiConsole.Markup(infoCellTwo.ToString());
            var infoCellThree = new System.Text.StringBuilder();
            infoCellThree.Append(String.Format("[white on lightskyblue3]"));
            infoCellThree.Append(String.Format("{0,21} {1,2}", "1 mc / 2 bicycles", ""));
            infoCellThree.Append(String.Format("[/]"));
            AnsiConsole.Markup(infoCellThree.ToString());

            Console.WriteLine();
            Console.SetCursorPosition((Console.WindowWidth - 69) / 2, Console.CursorTop);
            var infoCellFour = new System.Text.StringBuilder();
            infoCellFour.Append(String.Format("[white on steelblue]"));
            infoCellFour.Append(String.Format("{0,22} {1,1}", "1mc & 2bikes / 3bikes", ""));
            infoCellFour.Append(String.Format("[/]"));
            AnsiConsole.Markup(infoCellFour.ToString());
            var infoCellFive = new System.Text.StringBuilder();
            infoCellFive.Append(String.Format("[white on deeppink4_2]"));
            infoCellFive.Append(String.Format("{0,17} {1,6}", "Full / car", ""));
            infoCellFive.Append(String.Format("[/]"));
            AnsiConsole.Markup(infoCellFive.ToString());
            var infoCellSix = new System.Text.StringBuilder();
            infoCellSix.Append(String.Format("[white on deepskyblue4_2]"));
            infoCellSix.Append(String.Format("{0,13} {1,10}", "Bus", ""));
            infoCellSix.Append(String.Format("[/]"));
            AnsiConsole.Markup(infoCellSix.ToString());
            Console.WriteLine();
            Console.WriteLine();
            for (int i = 0; i < parkingLot.Count; i = i + 4)
            {
                int k = 3;
                Console.SetCursorPosition((Console.WindowWidth - 108) / 2, Console.CursorTop);
                for (int x = 0; x < 4; x++)
                {
                    int tot = i + x;
                    string cellNmbr;
                    if (tot < 10)
                    {
                        cellNmbr = "00" + tot;
                    }
                    else if (tot < 100)
                    {
                        cellNmbr = "0" + tot;
                    }
                    else
                    {
                        cellNmbr = "" + tot;
                    }

                    var parkingCell = new System.Text.StringBuilder();
                    
                    string cell;
                    if (parkingLot[tot].FreeSpace == 4)
                    {
                        parkingCell.Append(String.Format("[white on green]"));
                        cell = "Parking-spot: " + cellNmbr + " - ";
                        parkingCell.Append(String.Format("{0,-27}", cell));
                        parkingCell.Append(String.Format("[/]"));
                        AnsiConsole.Markup(parkingCell.ToString());
                    }
                    else if (parkingLot[tot].FreeSpace == 3)
                    {
                        parkingCell.Append(String.Format("[white on darkseagreen]"));
                        cell = "Parking-spot: " + cellNmbr + " - ";
                        parkingCell.Append(String.Format("{0,-27}", cell));
                        parkingCell.Append(String.Format("[/]"));
                        AnsiConsole.Markup(parkingCell.ToString());
                    }
                    else if (parkingLot[tot].FreeSpace == 2)
                    {
                        parkingCell.Append(String.Format("[white on lightskyblue3]"));
                        cell = "Parking-spot: " + cellNmbr + " - ";
                        parkingCell.Append(String.Format("{0,-27}", cell));
                        parkingCell.Append(String.Format("[/]"));
                        AnsiConsole.Markup(parkingCell.ToString());
                    }
                    else if (parkingLot[tot].FreeSpace == 1)
                    {
                        parkingCell.Append(String.Format("[white on steelblue]"));
                        cell = "Parking-spot: " + cellNmbr + " - ";
                        parkingCell.Append(String.Format("{0,-27}", cell));
                        parkingCell.Append(String.Format("[/]"));
                        AnsiConsole.Markup(parkingCell.ToString());
                    }
                    else if (parkingLot[tot].FreeSpace == 0 && parkingLot[tot].ParkedVehiclesOnSpot[0] is Bus)
                    {
                        parkingCell.Append(String.Format("[white on deepskyblue4_2]"));
                        cell = "Parking-spot: " + cellNmbr + " - ";
                        parkingCell.Append(String.Format("{0,-27}", cell));
                        parkingCell.Append(String.Format("[/]"));
                        AnsiConsole.Markup(parkingCell.ToString());
                    }
                    else if (parkingLot[tot].FreeSpace == 0)
                    {
                        parkingCell.Append(String.Format("[white on deeppink4_2]"));
                        cell = "Parking-spot: " + cellNmbr + " - ";
                        parkingCell.Append(String.Format("{0,-27}", cell));
                        parkingCell.Append(String.Format("[/]"));
                        AnsiConsole.Markup(parkingCell.ToString());
                    }
                }
                Console.WriteLine();
            };
            Console.WriteLine();
            Console.WriteLine();
            Console.ResetColor();
            if (amountOfVehicles.All(num => num >= 1))
            {
                var table = new Table();
                table.Width(100);
                table.Centered();
                table.Border = TableBorder.DoubleEdge;
                table.AddColumn("Number of vehicles: ");
                table.AddRow(
                new BarChart()
                    .Width(parkingLot.Count)
                    .CenterLabel()
                    .AddItem("Bicycles", amountOfVehicles[0], Color.SteelBlue).CenterLabel()
                    .AddItem("MCs", amountOfVehicles[1], Color.CadetBlue_1)
                    .AddItem("Cars", amountOfVehicles[2], Color.MediumPurple)
                    .AddItem("Buses", amountOfVehicles[3], Color.MediumPurple)
                );
                table.Columns[0].Centered();
                AnsiConsole.Render(table);
            }

            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
            AnsiConsole.MarkupLine("Press [springgreen4]any key[/] to go back to the main menu..");
            Console.ReadKey();
        }
        //Simple script to generate 10 random vehicles
        public static void GenerateVehicles()
        {
            var config = Program.config;
            string characters = "ABCDEFGHIJKLMNOPQRSTUVXYZ";
            string[] firstNames = { "Aaran", "Aaren", "Aarez", "Aarman", "Aaron", "Aaron-James", "Aarron", "Aaryan", "Aaryn", "Aayan", "Aazaan", "Abaan", "Abbas", "Abdallah", "Abdalroof", "Abdihakim", "Abdirahman", "Abdisalam", "Abdul", "Abdul-Aziz", "Abdulbasir", "Abdulkadir", "Abdulkarem", "Abdulkhader", "Abdullah", "Abdul-Majeed", "Abdulmalik", "Abdul-Rehman", "Abdur", "Abdurraheem", "Abdur-Rahman", "Abdur-Rehmaan", "Abel", "Abhinav", "Abhisumant", "Abid", "Abir", "Abraham", "Abu", "Abubakar", "Ace", "Adain", "Adam", "Adam-James", "Addison", "Addisson", "Adegbola", "Adegbolahan", "Aden", "Adenn", "Adie", "Adil", "Aditya", "Adnan", "Adrian", "Adrien", "Aedan", "Aedin", "Aedyn", "Aeron", "Afonso", "Ahmad", "Ahmed", "Ahmed-Aziz", "Ahoua", "Ahtasham", "Aiadan", "Aidan", "Aiden", "Aiden-Jack", "Aiden-Vee", "Aidian", "Aidy", "Ailin", "Aiman", "Ainsley", "Ainslie", "Airen", "Airidas", "Airlie", "AJ", "Ajay", "A-Jay", "Ajayraj", "Akan", "Akram", "Al", "Ala", "Alan", "Alanas", "Alasdair", "Alastair", "Alber", "Albert", "Albie", "Aldred", "Alec", "Aled", "Aleem", "Aleksandar", "Aleksander", "Aleksandr", "Aleksandrs", "Alekzander", "Alessandro", "Alessio", "Alex", "Alexander", "Alexei", "Alexx", "Alexzander", "Alf", "Alfee", "Alfie", "Alfred", "Alfy", "Alhaji", "Al-Hassan", "Ali", "Aliekber", "Alieu", "Alihaider", "Alisdair", "Alishan", "Alistair", "Alistar", "Alister", "Aliyaan", "Allan", "Allan-Laiton", "Allen", "Allesandro", "Allister", "Ally", "Alphonse", "Altyiab", "Alum", "Alvern", "Alvin", "Alyas", "Amaan", "Aman", "Amani", "Ambanimoh", "Ameer", "Amgad", "Ami", "Amin", "Amir", "Ammaar", "Ammar", "Ammer", "Amolpreet", "Amos", "Amrinder", "Amrit", "Amro", "Anay", "Andrea", "Andreas", "Andrei", "Andrejs", "Andrew", "Andy", "Anees", "Anesu", "Angel", "Angelo", "Angus", "Anir", "Anis", "Anish", "Anmolpreet", "Annan", "Anndra", "Anselm", "Anthony", "Anthony-John", "Antoine", "Anton", "Antoni", "Antonio", "Antony", "Antonyo", "Anubhav", "Aodhan", "Aon", "Aonghus", "Apisai", "Arafat", "Aran", "Arandeep", "Arann", "Aray", "Arayan", "Archibald", "Archie", "Arda", "Ardal", "Ardeshir", "Areeb", "Areez", "Aref", "Arfin", "Argyle", "Argyll", "Ari", "Aria", "Arian", "Arihant", "Aristomenis", "Aristotelis", "Arjuna", "Arlo", "Armaan", "Arman", "Armen", "Arnab", "Arnav", "Arnold", "Aron", "Aronas", "Arran", "Arrham", "Arron", "Arryn", "Arsalan", "Artem", "Arthur", "Artur", "Arturo", "Arun", "Arunas", "Arved", "Arya", "Aryan", "Aryankhan", "Aryian", "Aryn", "Asa", "Asfhan", "Ash", "Ashlee-jay", "Ashley", "Ashton", "Ashton-Lloyd", "Ashtyn", "Ashwin", "Asif", "Asim", "Aslam", "Asrar", "Ata", "Atal", "Atapattu", "Ateeq", "Athol", "Athon", "Athos-Carlos", "Atli", "Atom", "Attila", "Aulay", "Aun", "Austen", "Austin", "Avani", "Averon", "Avi", "Avinash", "Avraham", "Awais", "Awwal", "Axel", "Ayaan", "Ayan", "Aydan", "Ayden", "Aydin", "Aydon", "Ayman", "Ayomide", "Ayren", "Ayrton", "Aytug", "Ayub", "Ayyub", "Azaan", "Azedine", "Azeem", "Azim", "Aziz", "Azlan", "Azzam", "Azzedine", "Babatunmise", "Babur", "Bader", "Badr", "Badsha", "Bailee", "Bailey", "Bailie", "Bailley", "Baillie", "Baley", "Balian", "Banan", "Barath", "Barkley", "Barney", "Baron", "Barrie", "Barry", "Bartlomiej", "Bartosz", "Basher", "Basile", "Baxter", "Baye", "Bayley", "Beau", "Beinn", "Bekim", "Believe", "Ben", "Bendeguz", "Benedict", "Benjamin", "Benjamyn", "Benji", "Benn", "Bennett", "Benny", "Benoit", "Bentley", "Berkay", "Bernard", "Bertie", "Bevin", "Bezalel", "Bhaaldeen", "Bharath", "Bilal", "Bill", "Billy", "Binod", "Bjorn", "Blaike", "Blaine", "Blair", "Blaire", "Blake", "Blazej", "Blazey", "Blessing", "Blue", "Blyth", "Bo", "Boab", "Bob", "Bobby", "Bobby-Lee", "Bodhan", "Boedyn", "Bogdan", "Bohbi", "Bony", "Bowen", "Bowie", "Boyd", "Bracken", "Brad", "Bradan", "Braden", "Bradley", "Bradlie", "Bradly", "Brady", "Bradyn", "Braeden", "Braiden", "Brajan", "Brandan", "Branden", "Brandon", "Brandonlee", "Brandon-Lee", "Brandyn", "Brannan", "Brayden", "Braydon", "Braydyn", "Breandan", "Brehme", "Brendan", "Brendon", "Brendyn", "Breogan", "Bret", "Brett", "Briaddon", "Brian", "Brodi", "Brodie", "Brody", "Brogan", "Broghan", "Brooke", "Brooklin", "Brooklyn", "Bruce", "Bruin", "Bruno", "Brunon", "Bryan", "Bryce", "Bryden", "Brydon", "Brydon-Craig", "Bryn", "Brynmor", "Bryson", "Buddy", "Bully", "Burak", "Burhan", "Butali", "Butchi", "Byron", "Cabhan", "Cadan", "Cade", "Caden", "Cadon", "Cadyn", "Caedan", "Caedyn", "Cael", "Caelan", "Caelen", "Caethan", "Cahl", "Cahlum", "Cai", "Caidan", "Caiden", "Caiden-Paul", "Caidyn", "Caie", "Cailaen", "Cailean", "Caileb-John", "Cailin", "Cain", "Caine", "Cairn", "Cal", "Calan", "Calder", "Cale", "Calean", "Caleb", "Calen", "Caley", "Calib", "Calin", "Callahan", "Callan", "Callan-Adam", "Calley", "Callie", "Callin", "Callum", "Callun", "Callyn", "Calum", "Calum-James", "Calvin", "Cambell", "Camerin", "Cameron", "Campbel", "Campbell", "Camron", "Caolain", "Caolan", "Carl", "Carlo", "Carlos", "Carrich", "Carrick", "Carson", "Carter", "Carwyn", "Casey", "Casper", "Cassy", "Cathal", "Cator", "Cavan", "Cayden", "Cayden-Robert", "Cayden-Tiamo", "Ceejay", "Ceilan", "Ceiran", "Ceirin", "Ceiron", "Cejay", "Celik", "Cephas", "Cesar", "Cesare", "Chad", "Chaitanya", "Chang-Ha", "Charles", "Charley", "Charlie", "Charly", "Chase", "Che", "Chester", "Chevy", "Chi", "Chibudom", "Chidera", "Chimsom", "Chin", "Chintu", "Chiqal", "Chiron", "Chris", "Chris-Daniel", "Chrismedi", "Christian", "Christie", "Christoph", "Christopher", "Christopher-Lee", "Christy", "Chu", "Chukwuemeka", "Cian", "Ciann", "Ciar", "Ciaran", "Ciarian", "Cieran", "Cillian", "Cillin", "Cinar", "CJ", "C-Jay", "Clark", "Clarke", "Clayton", "Clement", "Clifford", "Clyde", "Cobain", "Coban", "Coben", "Cobi", "Cobie", "Coby", "Codey", "Codi", "Codie", "Cody", "Cody-Lee", "Coel", "Cohan", "Cohen", "Colby", "Cole", "Colin", "Coll", "Colm", "Colt", "Colton", "Colum", "Colvin", "Comghan", "Conal", "Conall", "Conan", "Conar", "Conghaile", "Conlan", "Conley", "Conli", "Conlin", "Conlly", "Conlon", "Conlyn", "Connal", "Connall", "Connan", "Connar", "Connel", "Connell", "Conner", "Connolly", "Connor", "Connor-David", "Conor", "Conrad", "Cooper", "Copeland", "Coray", "Corben", "Corbin", "Corey", "Corey-James", "Corey-Jay", "Cori", "Corie", "Corin", "Cormac", "Cormack", "Cormak", "Corran", "Corrie", "Cory", "Cosmo", "Coupar", "Craig", "Craig-James", "Crawford", "Creag", "Crispin", "Cristian", "Crombie", "Cruiz", "Cruz", "Cuillin", "Cullen", "Cullin", "Curtis", "Cyrus", "Daanyaal", "Daegan", "Daegyu", "Dafydd", "Dagon", "Dailey", "Daimhin", "Daithi", "Dakota", "Daksh", "Dale", "Dalong", "Dalton", "Damian", "Damien", "Damon", "Dan", "Danar", "Dane", "Danial", "Daniel", "Daniele", "Daniel-James", "Daniels", "Daniil", "Danish", "Daniyal", "Danniel", "Danny", "Dante", "Danyal", "Danyil", "Danys", "Daood", "Dara", "Darach", "Daragh", "Darcy", "D'arcy", "Dareh", "Daren", "Darien", "Darius", "Darl", "Darn", "Darrach", "Darragh", "Darrel", "Darrell", "Darren", "Darrie", "Darrius", "Darroch", "Darryl", "Darryn", "Darwyn", "Daryl", "Daryn", "Daud", "Daumantas", "Davi", "David", "David-Jay", "David-Lee", "Davie", "Davis", "Davy", "Dawid", "Dawson", "Dawud", "Dayem", "Daymian", "Deacon", "Deagan", "Dean", "Deano", "Decklan", "Declain", "Declan", "Declyan", "Declyn", "Dedeniseoluwa", "Deecan", "Deegan", "Deelan", "Deklain-Jaimes", "Del", "Demetrius", "Denis", "Deniss", "Dennan", "Dennin", "Dennis", "Denny", "Dennys", "Denon", "Denton", "Denver", "Denzel", "Deon", "Derek", "Derick", "Derin", "Dermot", "Derren", "Derrie", "Derrin", "Derron", "Derry", "Derryn", "Deryn", "Deshawn", "Desmond", "Dev", "Devan", "Devin", "Devlin", "Devlyn", "Devon", "Devrin", "Devyn", "Dex", "Dexter", "Dhani", "Dharam", "Dhavid", "Dhyia", "Diarmaid", "Diarmid", "Diarmuid", "Didier", "Diego", "Diesel", "Diesil", "Digby", "Dilan", "Dilano", "Dillan", "Dillon", "Dilraj", "Dimitri", "Dinaras", "Dion", "Dissanayake", "Dmitri", "Doire", "Dolan", "Domanic", "Domenico", "Domhnall", "Dominic", "Dominick", "Dominik", "Donald", "Donnacha", "Donnie", "Dorian", "Dougal", "Douglas", "Dougray", "Drakeo", "Dre", "Dregan", "Drew", "Dugald", "Duncan", "Duriel", "Dustin", "Dylan", "Dylan-Jack", "Dylan-James", "Dylan-John", "Dylan-Patrick", "Dylin", "Dyllan", "Dyllan-James", "Dyllon", "Eadie", "Eagann", "Eamon", "Eamonn", "Eason", "Eassan", "Easton", "Ebow", "Ed", "Eddie", "Eden", "Ediomi", "Edison", "Eduardo", "Eduards", "Edward", "Edwin", "Edwyn", "Eesa", "Efan", "Efe", "Ege", "Ehsan", "Ehsen", "Eiddon", "Eidhan", "Eihli", "Eimantas", "Eisa", "Eli", "Elias", "Elijah", "Eliot", "Elisau", "Eljay", "Eljon", "Elliot", "Elliott", "Ellis", "Ellisandro", "Elshan", "Elvin", "Elyan", "Emanuel", "Emerson", "Emil", "Emile", "Emir", "Emlyn", "Emmanuel", "Emmet", "Eng", "Eniola", "Enis", "Ennis", "Enrico", "Enrique", "Enzo", "Eoghain", "Eoghan", "Eoin", "Eonan", "Erdehan", "Eren", "Erencem", "Eric", "Ericlee", "Erik", "Eriz", "Ernie-Jacks", "Eroni", "Eryk", "Eshan", "Essa", "Esteban", "Ethan", "Etienne", "Etinosa", "Euan", "Eugene", "Evan", "Evann", "Ewan", "Ewen", "Ewing", "Exodi", "Ezekiel", "Ezra", "Fabian", "Fahad", "Faheem", "Faisal", "Faizaan", "Famara", "Fares", "Farhaan", "Farhan", "Farren", "Farzad", "Fauzaan", "Favour", "Fawaz", "Fawkes", "Faysal", "Fearghus", "Feden", "Felix", "Fergal", "Fergie", "Fergus", "Ferre", "Fezaan", "Fiachra", "Fikret", "Filip", "Filippo", "Finan", "Findlay", "Findlay-James", "Findlie", "Finlay", "Finley", "Finn", "Finnan", "Finnean", "Finnen", "Finnlay", "Finnley", "Fintan", "Fionn", "Firaaz", "Fletcher", "Flint", "Florin", "Flyn", "Flynn", "Fodeba", "Folarinwa", "Forbes", "Forgan", "Forrest", "Fox", "Francesco", "Francis", "Francisco", "Franciszek", "Franco", "Frank", "Frankie", "Franklin", "Franko", "Fraser", "Frazer", "Fred", "Freddie", "Frederick", "Fruin", "Fyfe", "Fyn", "Fynlay", "Fynn", "Gabriel", "Gallagher", "Gareth", "Garren", "Garrett", "Garry", "Gary", "Gavin", "Gavin-Lee", "Gene", "Geoff", "Geoffrey", "Geomer", "Geordan", "Geordie", "George", "Georgia", "Georgy", "Gerard", "Ghyll", "Giacomo", "Gian", "Giancarlo", "Gianluca", "Gianmarco", "Gideon", "Gil", "Gio", "Girijan", "Girius", "Gjan", "Glascott", "Glen", "Glenn", "Gordon", "Grady", "Graeme", "Graham", "Grahame", "Grant", "Grayson", "Greg", "Gregor", "Gregory", "Greig", "Griffin", "Griffyn", "Grzegorz", "Guang", "Guerin", "Guillaume", "Gurardass", "Gurdeep", "Gursees", "Gurthar", "Gurveer", "Gurwinder", "Gus", "Gustav", "Guthrie", "Guy", "Gytis", "Habeeb", "Hadji", "Hadyn", "Hagun", "Haiden", "Haider", "Hamad", "Hamid", "Hamish", "Hamza", "Hamzah", "Han", "Hansen", "Hao", "Hareem", "Hari", "Harikrishna", "Haris", "Harish", "Harjeevan", "Harjyot", "Harlee", "Harleigh", "Harley", "Harman", "Harnek", "Harold", "Haroon", "Harper", "Harri", "Harrington", "Harris", "Harrison", "Harry", "Harvey", "Harvie", "Harvinder", "Hasan", "Haseeb", "Hashem", "Hashim", "Hassan", "Hassanali", "Hately", "Havila", "Hayden", "Haydn", "Haydon", "Haydyn", "Hcen", "Hector", "Heddle", "Heidar", "Heini", "Hendri", "Henri", "Henry", "Herbert", "Heyden", "Hiro", "Hirvaansh", "Hishaam", "Hogan", "Honey", "Hong", "Hope", "Hopkin", "Hosea", "Howard", "Howie", "Hristomir", "Hubert", "Hugh", "Hugo", "Humza", "Hunter", "Husnain", "Hussain", "Hussan", "Hussnain", "Hussnan", "Hyden", "I", "Iagan", "Iain", "Ian", "Ibraheem", "Ibrahim", "Idahosa", "Idrees", "Idris", "Iestyn", "Ieuan", "Igor", "Ihtisham", "Ijay", "Ikechukwu", "Ikemsinachukwu", "Ilyaas", "Ilyas", "Iman", "Immanuel", "Inan", "Indy", "Ines", "Innes", "Ioannis", "Ireayomide", "Ireoluwa", "Irvin", "Irvine", "Isa", "Isaa", "Isaac", "Isaiah", "Isak", "Isher", "Ishwar", "Isimeli", "Isira", "Ismaeel", "Ismail", "Israel", "Issiaka", "Ivan", "Ivar", "Izaak", "J", "Jaay", "Jac", "Jace", "Jack", "Jacki", "Jackie", "Jack-James", "Jackson", "Jacky", "Jacob", "Jacques", "Jad", "Jaden", "Jadon", "Jadyn", "Jae", "Jagat", "Jago", "Jaheim", "Jahid", "Jahy", "Jai", "Jaida", "Jaiden", "Jaidyn", "Jaii", "Jaime", "Jai-Rajaram", "Jaise", "Jak", "Jake", "Jakey", "Jakob", "Jaksyn", "Jakub", "Jamaal", "Jamal", "Jameel", "Jameil", "James", "James-Paul", "Jamey", "Jamie", "Jan", "Jaosha", "Jardine", "Jared", "Jarell", "Jarl", "Jarno", "Jarred", "Jarvi", "Jasey-Jay", "Jasim", "Jaskaran", "Jason", "Jasper", "Jaxon", "Jaxson", "Jay", "Jaydan", "Jayden", "Jayden-James", "Jayden-Lee", "Jayden-Paul", "Jayden-Thomas", "Jaydn", "Jaydon", "Jaydyn", "Jayhan", "Jay-Jay", "Jayke", "Jaymie", "Jayse", "Jayson", "Jaz", "Jazeb", "Jazib", "Jazz", "Jean", "Jean-Lewis", "Jean-Pierre", "Jebadiah", "Jed", "Jedd", "Jedidiah", "Jeemie", "Jeevan", "Jeffrey", "Jensen", "Jenson", "Jensyn", "Jeremy", "Jerome", "Jeronimo", "Jerrick", "Jerry", "Jesse", "Jesuseun", "Jeswin", "Jevan", "Jeyun", "Jez", "Jia", "Jian", "Jiao", "Jimmy", "Jincheng", "JJ", "Joaquin", "Joash", "Jock", "Jody", "Joe", "Joeddy", "Joel", "Joey", "Joey-Jack", "Johann", "Johannes", "Johansson", "John", "Johnathan", "Johndean", "Johnjay", "John-Michael", "Johnnie", "Johnny", "Johnpaul", "John-Paul", "John-Scott", "Johnson", "Jole", "Jomuel", "Jon", "Jonah", "Jonatan", "Jonathan", "Jonathon", "Jonny", "Jonothan", "Jon-Paul", "Jonson", "Joojo", "Jordan", "Jordi", "Jordon", "Jordy", "Jordyn", "Jorge", "Joris", "Jorryn", "Josan", "Josef", "Joseph", "Josese", "Josh", "Joshiah", "Joshua", "Josiah", "Joss", "Jostelle", "Joynul", "Juan", "Jubin", "Judah", "Jude", "Jules", "Julian", "Julien", "Jun", "Junior", "Jura", "Justan", "Justin", "Justinas", "Kaan", "Kabeer", "Kabir", "Kacey", "Kacper", "Kade", "Kaden", "Kadin", "Kadyn", "Kaeden", "Kael", "Kaelan", "Kaelin", "Kaelum", "Kai", "Kaid", "Kaidan", "Kaiden", "Kaidinn", "Kaidyn", "Kaileb", "Kailin", "Kain", "Kaine", "Kainin", "Kainui", "Kairn", "Kaison", "Kaiwen", "Kajally", "Kajetan", "Kalani", "Kale", "Kaleb", "Kaleem", "Kal-el", "Kalen", "Kalin", "Kallan", "Kallin", "Kalum", "Kalvin", "Kalvyn", "Kameron", "Kames", "Kamil", "Kamran", "Kamron", "Kane", "Karam", "Karamvir", "Karandeep", "Kareem", "Karim", "Karimas", "Karl", "Karol", "Karson", "Karsyn", "Karthikeya", "Kasey", "Kash", "Kashif", "Kasim", "Kasper", "Kasra", "Kavin", "Kayam", "Kaydan", "Kayden", "Kaydin", "Kaydn", "Kaydyn", "Kaydyne", "Kayleb", "Kaylem", "Kaylum", "Kayne", "Kaywan", "Kealan", "Kealon", "Kean", "Keane", "Kearney", "Keatin", "Keaton", "Keavan", "Keayn", "Kedrick", "Keegan", "Keelan", "Keelin", "Keeman", "Keenan", "Keenan-Lee", "Keeton", "Kehinde", "Keigan", "Keilan", "Keir", "Keiran", "Keiren", "Keiron", "Keiryn", "Keison", "Keith", "Keivlin", "Kelam", "Kelan", "Kellan", "Kellen", "Kelso", "Kelum", "Kelvan", "Kelvin", "Ken", "Kenan", "Kendall", "Kendyn", "Kenlin", "Kenneth", "Kensey", "Kenton", "Kenyon", "Kenzeigh", "Kenzi", "Kenzie", "Kenzo", "Kenzy", "Keo", "Ker", "Kern", "Kerr", "Kevan", "Kevin", "Kevyn", "Kez", "Khai", "Khalan", "Khaleel", "Khaya", "Khevien", "Khizar", "Khizer", "Kia", "Kian", "Kian-James", "Kiaran", "Kiarash", "Kie", "Kiefer", "Kiegan", "Kienan", "Kier", "Kieran", "Kieran-Scott", "Kieren", "Kierin", "Kiern", "Kieron", "Kieryn", "Kile", "Killian", "Kimi", "Kingston", "Kinneil", "Kinnon", "Kinsey", "Kiran", "Kirk", "Kirwin", "Kit", "Kiya", "Kiyonari", "Kjae", "Klein", "Klevis", "Kobe", "Kobi", "Koby", "Koddi", "Koden", "Kodi", "Kodie", "Kody", "Kofi", "Kogan", "Kohen", "Kole", "Konan", "Konar", "Konnor", "Konrad", "Koray", "Korben", "Korbyn", "Korey", "Kori", "Korrin", "Kory", "Koushik", "Kris", "Krish", "Krishan", "Kriss", "Kristian", "Kristin", "Kristofer", "Kristoffer", "Kristopher", "Kruz", "Krzysiek", "Krzysztof", "Ksawery", "Ksawier", "Kuba", "Kurt", "Kurtis", "Kurtis-Jae", "Kyaan", "Kyan", "Kyde", "Kyden", "Kye", "Kyel", "Kyhran", "Kyie", "Kylan", "Kylar", "Kyle", "Kyle-Derek", "Kylian", "Kym", "Kynan", "Kyral", "Kyran", "Kyren", "Kyrillos", "Kyro", "Kyron", "Kyrran", "Lachlainn", "Lachlan", "Lachlann", "Lael", "Lagan", "Laird", "Laison", "Lakshya", "Lance", "Lancelot", "Landon", "Lang", "Lasse", "Latif", "Lauchlan", "Lauchlin", "Laughlan", "Lauren", "Laurence", "Laurie", "Lawlyn", "Lawrence", "Lawrie", "Lawson", "Layne", "Layton", "Lee", "Leigh", "Leigham", "Leighton", "Leilan", "Leiten", "Leithen", "Leland", "Lenin", "Lennan", "Lennen", "Lennex", "Lennon", "Lennox", "Lenny", "Leno", "Lenon", "Lenyn", "Leo", "Leon", "Leonard", "Leonardas", "Leonardo", "Lepeng", "Leroy", "Leven", "Levi", "Levon", "Levy", "Lewie", "Lewin", "Lewis", "Lex", "Leydon", "Leyland", "Leylann", "Leyton", "Liall", "Liam", "Liam-Stephen", "Limo", "Lincoln", "Lincoln-John", "Lincon", "Linden", "Linton", "Lionel", "Lisandro", "Litrell", "Liyonela-Elam", "LLeyton", "Lliam", "Lloyd", "Lloyde", "Loche", "Lochlan", "Lochlann", "Lochlan-Oliver", "Lock", "Lockey", "Logan", "Logann", "Logan-Rhys", "Loghan", "Lokesh", "Loki", "Lomond", "Lorcan", "Lorenz", "Lorenzo", "Lorne", "Loudon", "Loui", "Louie", "Louis", "Loukas", "Lovell", "Luc", "Luca", "Lucais", "Lucas", "Lucca", "Lucian", "Luciano", "Lucien", "Lucus", "Luic", "Luis", "Luk", "Luka", "Lukas", "Lukasz", "Luke", "Lukmaan", "Luqman", "Lyall", "Lyle", "Lyndsay", "Lysander", "Maanav", "Maaz", "Mac", "Macallum", "Macaulay", "Macauley", "Macaully", "Machlan", "Maciej", "Mack", "Mackenzie", "Mackenzy", "Mackie", "Macsen", "Macy", "Madaki", "Maddison", "Maddox", "Madison", "Madison-Jake", "Madox", "Mael", "Magnus", "Mahan", "Mahdi", "Mahmoud", "Maias", "Maison", "Maisum", "Maitlind", "Majid", "Makensie", "Makenzie", "Makin", "Maksim", "Maksymilian", "Malachai", "Malachi", "Malachy", "Malakai", "Malakhy", "Malcolm", "Malik", "Malikye", "Malo", "Ma'moon", "Manas", "Maneet", "Manmohan", "Manolo", "Manson", "Mantej", "Manuel", "Manus", "Marc", "Marc-Anthony", "Marcel", "Marcello", "Marcin", "Marco", "Marcos", "Marcous", "Marcquis", "Marcus", "Mario", "Marios", "Marius", "Mark", "Marko", "Markus", "Marley", "Marlin", "Marlon", "Maros", "Marshall", "Martin", "Marty", "Martyn", "Marvellous", "Marvin", "Marwan", "Maryk", "Marzuq", "Mashhood", "Mason", "Mason-Jay", "Masood", "Masson", "Matas", "Matej", "Mateusz", "Mathew", "Mathias", "Mathu", "Mathuyan", "Mati", "Matt", "Matteo", "Matthew", "Matthew-William", "Matthias", "Max", "Maxim", "Maximilian", "Maximillian", "Maximus", "Maxwell", "Maxx", "Mayeul", "Mayson", "Mazin", "Mcbride", "McCaulley", "McKade", "McKauley", "McKay", "McKenzie", "McLay", "Meftah", "Mehmet", "Mehraz", "Meko", "Melville", "Meshach", "Meyzhward", "Micah", "Michael", "Michael-Alexander", "Michael-James", "Michal", "Michat", "Micheal", "Michee", "Mickey", "Miguel", "Mika", "Mikael", "Mikee", "Mikey", "Mikhail", "Mikolaj", "Miles", "Millar", "Miller", "Milo", "Milos", "Milosz", "Mir", "Mirza", "Mitch", "Mitchel", "Mitchell", "Moad", "Moayd", "Mobeen", "Modoulamin", "Modu", "Mohamad", "Mohamed", "Mohammad", "Mohammad-Bilal", "Mohammed", "Mohanad", "Mohd", "Momin", "Momooreoluwa", "Montague", "Montgomery", "Monty", "Moore", "Moosa", "Moray", "Morgan", "Morgyn", "Morris", "Morton", "Moshy", "Motade", "Moyes", "Msughter", "Mueez", "Muhamadjavad", "Muhammad", "Muhammed", "Muhsin", "Muir", "Munachi", "Muneeb", "Mungo", "Munir", "Munmair", "Munro", "Murdo", "Murray", "Murrough", "Murry", "Musa", "Musse", "Mustafa", "Mustapha", "Muzammil", "Muzzammil", "Mykie", "Myles", "Mylo", "Nabeel", "Nadeem", "Nader", "Nagib", "Naif", "Nairn", "Narvic", "Nash", "Nasser", "Nassir", "Natan", "Nate", "Nathan", "Nathanael", "Nathanial", "Nathaniel", "Nathan-Rae", "Nawfal", "Nayan", "Neco", "Neil", "Nelson", "Neo", "Neshawn", "Nevan", "Nevin", "Ngonidzashe", "Nial", "Niall", "Nicholas", "Nick", "Nickhill", "Nicki", "Nickson", "Nicky", "Nico", "Nicodemus", "Nicol", "Nicolae", "Nicolas", "Nidhish", "Nihaal", "Nihal", "Nikash", "Nikhil", "Niki", "Nikita", "Nikodem", "Nikolai", "Nikos", "Nilav", "Niraj", "Niro", "Niven", "Noah", "Noel", "Nolan", "Noor", "Norman", "Norrie", "Nuada", "Nyah", "Oakley", "Oban", "Obieluem", "Obosa", "Odhran", "Odin", "Odynn", "Ogheneochuko", "Ogheneruno", "Ohran", "Oilibhear", "Oisin", "Ojima-Ojo", "Okeoghene", "Olaf", "Ola-Oluwa", "Olaoluwapolorimi", "Ole", "Olie", "Oliver", "Olivier", "Oliwier", "Ollie", "Olurotimi", "Oluwadamilare", "Oluwadamiloju", "Oluwafemi", "Oluwafikunayomi", "Oluwalayomi", "Oluwatobiloba", "Oluwatoni", "Omar", "Omri", "Oran", "Orin", "Orlando", "Orley", "Orran", "Orrick", "Orrin", "Orson", "Oryn", "Oscar", "Osesenagha", "Oskar", "Ossian", "Oswald", "Otto", "Owain", "Owais", "Owen", "Owyn", "Oz", "Ozzy", "Pablo", "Pacey", "Padraig", "Paolo", "Pardeepraj", "Parkash", "Parker", "Pascoe", "Pasquale", "Patrick", "Patrick-John", "Patrikas", "Patryk", "Paul", "Pavit", "Pawel", "Pawlo", "Pearce", "Pearse", "Pearsen", "Pedram", "Pedro", "Peirce", "Peiyan", "Pele", "Peni", "Peregrine", "Peter", "Phani", "Philip", "Philippos", "Phinehas", "Phoenix", "Phoevos", "Pierce", "Pierre-Antoine", "Pieter", "Pietro", "Piotr", "Porter", "Prabhjoit", "Prabodhan", "Praise", "Pranav", "Pravin", "Precious", "Prentice", "Presley", "Preston", "Preston-Jay", "Prinay", "Prince", "Prithvi", "Promise", "Puneetpaul", "Pushkar", "Qasim", "Qirui", "Quinlan", "Quinn", "Radmiras", "Raees", "Raegan", "Rafael", "Rafal", "Rafferty", "Rafi", "Raheem", "Rahil", "Rahim", "Rahman", "Raith", "Raithin", "Raja", "Rajab-Ali", "Rajan", "Ralfs", "Ralph", "Ramanas", "Ramit", "Ramone", "Ramsay", "Ramsey", "Rana", "Ranolph", "Raphael", "Rasmus", "Rasul", "Raul", "Raunaq", "Ravin", "Ray", "Rayaan", "Rayan", "Rayane", "Rayden", "Rayhan", "Raymond", "Rayne", "Rayyan", "Raza", "Reace", "Reagan", "Reean", "Reece", "Reed", "Reegan", "Rees", "Reese", "Reeve", "Regan", "Regean", "Reggie", "Rehaan", "Rehan", "Reice", "Reid", "Reigan", "Reilly", "Reily", "Reis", "Reiss", "Remigiusz", "Remo", "Remy", "Ren", "Renars", "Reng", "Rennie", "Reno", "Reo", "Reuben", "Rexford", "Reynold", "Rhein", "Rheo", "Rhett", "Rheyden", "Rhian", "Rhoan", "Rholmark", "Rhoridh", "Rhuairidh", "Rhuan", "Rhuaridh", "Rhudi", "Rhy", "Rhyan", "Rhyley", "Rhyon", "Rhys", "Rhys-Bernard", "Rhyse", "Riach", "Rian", "Ricards", "Riccardo", "Ricco", "Rice", "Richard", "Richey", "Richie", "Ricky", "Rico", "Ridley", "Ridwan", "Rihab", "Rihan", "Rihards", "Rihonn", "Rikki", "Riley", "Rio", "Rioden", "Rishi", "Ritchie", "Rivan", "Riyadh", "Riyaj", "Roan", "Roark", "Roary", "Rob", "Robbi", "Robbie", "Robbie-lee", "Robby", "Robert", "Robert-Gordon", "Robertjohn", "Robi", "Robin", "Rocco", "Roddy", "Roderick", "Rodrigo", "Roen", "Rogan", "Roger", "Rohaan", "Rohan", "Rohin", "Rohit", "Rokas", "Roman", "Ronald", "Ronan", "Ronan-Benedict", "Ronin", "Ronnie", "Rooke", "Roray", "Rori", "Rorie", "Rory", "Roshan", "Ross", "Ross-Andrew", "Rossi", "Rowan", "Rowen", "Roy", "Ruadhan", "Ruaidhri", "Ruairi", "Ruairidh", "Ruan", "Ruaraidh", "Ruari", "Ruaridh", "Ruben", "Rubhan", "Rubin", "Rubyn", "Rudi", "Rudy", "Rufus", "Rui", "Ruo", "Rupert", "Ruslan", "Russel", "Russell", "Ryaan", "Ryan", "Ryan-Lee", "Ryden", "Ryder", "Ryese", "Ryhs", "Rylan", "Rylay", "Rylee", "Ryleigh", "Ryley", "Rylie", "Ryo", "Ryszard", "Saad", "Sabeen", "Sachkirat", "Saffi", "Saghun", "Sahaib", "Sahbian", "Sahil", "Saif", "Saifaddine", "Saim", "Sajid", "Sajjad", "Salahudin", "Salman", "Salter", "Salvador", "Sam", "Saman", "Samar", "Samarjit", "Samatar", "Sambrid", "Sameer", "Sami", "Samir", "Sami-Ullah", "Samual", "Samuel", "Samuela", "Samy", "Sanaullah", "Sandro", "Sandy", "Sanfur", "Sanjay", "Santiago", "Santino", "Satveer", "Saul", "Saunders", "Savin", "Sayad", "Sayeed", "Sayf", "Scot", "Scott", "Scott-Alexander", "Seaan", "Seamas", "Seamus", "Sean", "Seane", "Sean-James", "Sean-Paul", "Sean-Ray", "Seb", "Sebastian", "Sebastien", "Selasi", "Seonaidh", "Sephiroth", "Sergei", "Sergio", "Seth", "Sethu", "Seumas", "Shaarvin", "Shadow", "Shae", "Shahmir", "Shai", "Shane", "Shannon", "Sharland", "Sharoz", "Shaughn", "Shaun", "Shaunpaul", "Shaun-Paul", "Shaun-Thomas", "Shaurya", "Shaw", "Shawn", "Shawnpaul", "Shay", "Shayaan", "Shayan", "Shaye", "Shayne", "Shazil", "Shea", "Sheafan", "Sheigh", "Shenuk", "Sher", "Shergo", "Sheriff", "Sherwyn", "Shiloh", "Shiraz", "Shreeram", "Shreyas", "Shyam", "Siddhant", "Siddharth", "Sidharth", "Sidney", "Siergiej", "Silas", "Simon", "Sinai", "Skye", "Sofian", "Sohaib", "Sohail", "Soham", "Sohan", "Sol", "Solomon", "Sonneey", "Sonni", "Sonny", "Sorley", "Soul", "Spencer", "Spondon", "Stanislaw", "Stanley", "Stefan", "Stefano", "Stefin", "Stephen", "Stephenjunior", "Steve", "Steven", "Steven-lee", "Stevie", "Stewart", "Stewarty", "Strachan", "Struan", "Stuart", "Su", "Subhaan", "Sudais", "Suheyb", "Suilven", "Sukhi", "Sukhpal", "Sukhvir", "Sulayman", "Sullivan", "Sultan", "Sung", "Sunny", "Suraj", "Surien", "Sweyn", "Syed", "Sylvain", "Symon", "Szymon", "Tadd", "Taddy", "Tadhg", "Taegan", "Taegen", "Tai", "Tait", "Taiwo", "Talha", "Taliesin", "Talon", "Talorcan", "Tamar", "Tamiem", "Tammam", "Tanay", "Tane", "Tanner", "Tanvir", "Tanzeel", "Taonga", "Tarik", "Tariq-Jay", "Tate", "Taylan", "Taylar", "Tayler", "Taylor", "Taylor-Jay", "Taylor-Lee", "Tayo", "Tayyab", "Tayye", "Tayyib", "Teagan", "Tee", "Teejay", "Tee-jay", "Tegan", "Teighen", "Teiyib", "Te-Jay", "Temba", "Teo", "Teodor", "Teos", "Terry", "Teydren", "Theo", "Theodore", "Thiago", "Thierry", "Thom", "Thomas", "Thomas-Jay", "Thomson", "Thorben", "Thorfinn", "Thrinei", "Thumbiko", "Tiago", "Tian", "Tiarnan", "Tibet", "Tieran", "Tiernan", "Timothy", "Timucin", "Tiree", "Tisloh", "Titi", "Titus", "Tiylar", "TJ", "Tjay", "T-Jay", "Tobey", "Tobi", "Tobias", "Tobie", "Toby", "Todd", "Tokinaga", "Toluwalase", "Tom", "Tomas", "Tomasz", "Tommi-Lee", "Tommy", "Tomson", "Tony", "Torin", "Torquil", "Torran", "Torrin", "Torsten", "Trafford", "Trai", "Travis", "Tre", "Trent", "Trey", "Tristain", "Tristan", "Troy", "Tubagus", "Turki", "Turner", "Ty", "Ty-Alexander", "Tye", "Tyelor", "Tylar", "Tyler", "Tyler-James", "Tyler-Jay", "Tyllor", "Tylor", "Tymom", "Tymon", "Tymoteusz", "Tyra", "Tyree", "Tyrnan", "Tyrone", "Tyson", "Ubaid", "Ubayd", "Uchenna", "Uilleam", "Umair", "Umar", "Umer", "Umut", "Urban", "Uri", "Usman", "Uzair", "Uzayr", "Valen", "Valentin", "Valentino", "Valery", "Valo", "Vasyl", "Vedantsinh", "Veeran", "Victor", "Victory", "Vinay", "Vince", "Vincent", "Vincenzo", "Vinh", "Vinnie", "Vithujan", "Vladimir", "Vladislav", "Vrishin", "Vuyolwethu", "Wabuya", "Wai", "Walid", "Wallace", "Walter", "Waqaas", "Warkhas", "Warren", "Warrick", "Wasif", "Wayde", "Wayne", "Wei", "Wen", "Wesley", "Wesley-Scott", "Wiktor", "Wilkie", "Will", "William", "William-John", "Willum", "Wilson", "Windsor", "Wojciech", "Woyenbrakemi", "Wyatt", "Wylie", "Wynn", "Xabier", "Xander", "Xavier", "Xiao", "Xida", "Xin", "Xue", "Yadgor", "Yago", "Yahya", "Yakup", "Yang", "Yanick", "Yann", "Yannick", "Yaseen", "Yasin", "Yasir", "Yassin", "Yoji", "Yong", "Yoolgeun", "Yorgos", "Youcef", "Yousif", "Youssef", "Yu", "Yuanyu", "Yuri", "Yusef", "Yusuf", "Yves", "Zaaine", "Zaak", "Zac", "Zach", "Zachariah", "Zacharias", "Zacharie", "Zacharius", "Zachariya", "Zachary", "Zachary-Marc", "Zachery", "Zack", "Zackary", "Zaid", "Zain", "Zaine", "Zaineddine", "Zainedin", "Zak", "Zakaria", "Zakariya", "Zakary", "Zaki", "Zakir", "Zakk", "Zamaar", "Zander", "Zane", "Zarran", "Zayd", "Zayn", "Zayne", "Ze", "Zechariah", "Zeek", "Zeeshan", "Zeid", "Zein", "Zen", "Zendel", "Zenith", "Zennon", "Zeph", "Zerah", "Zhen", "Zhi", "Zhong", "Zhuo", "Zi", "Zidane", "Zijie", "Zinedine", "Zion", "Zishan", "Ziya", "Ziyaan", "Zohaib", "Zohair", "Zoubaeir", "Zubair", "Zubayr", "Zuriel" };
            string[] lastNames = { "Nyman", "Larsson", "Smith", "Flanders", "Svensson", "Bilic", "Thuresson", "Nulsson", "Hellquist", "Sjogren", "Stensson", "Gunnarsson", "Alfredsson", "Hillerhag", "Johnsson", "Johansson", "Putin", "Trump", "Bunton" };
            string[] colors = { "Yellow", "Green", "Blue", "Silver", "Golden", "Pink", "Brown", "Black", "White", "Turqoise", "Cyan", "Purple" };
            string[] carBrands = { "Avia", "Bureko", "Jawa", "Volvo", "Saab", "Porsche", "Skoda", "Mitsubishi", "Peugot", "Mazda", "Stelka", "Ferarri" };
            string[] mcBrands = { "Yamaha", "Suzuki", "Kawasaki", "Baotian", "Hinseng", "Aprilia", "Peugot", "Jawa", "Honda" };

            Random random = new Random();
            for (int i = 0; i < 10; i++)
            {
                if (i == 0)
                {
                    Console.SetCursorPosition((Console.WindowWidth - 42) / 2, (Console.WindowHeight - 20) / 2);
                }
                else
                {
                    Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop + 1);
                }

                int randomVehicleType = random.Next(0, 3);
                if (randomVehicleType == 1)
                {
                    string owner = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
                    string color = $"{colors[random.Next(colors.Length)]}";
                    int freeSpot = findFreeSpot("bicycle", config);
                    string token = ParkBicycle(new Bicycle(owner, color, freeSpot, DateTime.Now), freeSpot, config);
                    Console.WriteLine($"Bike with token: {token} generated.");
                }
                else if (randomVehicleType == 2)
                {
                    string owner = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
                    string brand = $"{mcBrands[random.Next(mcBrands.Length)]}";
                    string registrationNumber = $"{characters[random.Next(characters.Length)].ToString()}{characters[random.Next(characters.Length)].ToString()}{characters[random.Next(characters.Length)].ToString()}{characters[random.Next(characters.Length)].ToString()}{random.Next(100, 999)}";
                    int freeSpot = findFreeSpot("mc", config);
                    ParkMc(new Mc(registrationNumber, owner, brand, " ", freeSpot, DateTime.Now), freeSpot, config);
                    Console.WriteLine($"Mc with registration number: {registrationNumber} generated.");
                }
                else
                {
                    string owner = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
                    string brand = $"{carBrands[random.Next(carBrands.Length)]}";
                    string registrationNumber = $"{characters[random.Next(characters.Length)].ToString()}{characters[random.Next(characters.Length)].ToString()}{characters[random.Next(characters.Length)].ToString()}{characters[random.Next(characters.Length)].ToString()}{random.Next(100, 999)}";
                    int freeSpot = findFreeSpot("car", config);
                    ParkCar(new Car(registrationNumber, owner, brand, " ", freeSpot, DateTime.Now), freeSpot, config);
                    Console.WriteLine($"Car with registration number: {registrationNumber} generated.");
                }
                Thread.Sleep(500);
            }
            Console.WriteLine();
            Console.SetCursorPosition((Console.WindowWidth - 42) / 2, Console.CursorTop);
            AnsiConsole.Markup("Press [springgreen3]any key[/] to go back to the main menu..");
            Console.ReadKey();
            WriteToStorage();
        }
    }
}

