using System;
using System.Data.SQLite;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json.Linq;

class Program
{
    private static DatabaseManager dbManager;
    private static ApiClient apiClient;
    private static System.Timers.Timer timer;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        ConsoleHelper.SetConsoleFontSize(8); // Zmenšení písma

        string databasePath = Path.Combine(Environment.CurrentDirectory, "departures.db");
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath); // Odstranit starou databázi, aby se vytvořila nová
        }

        string apiUrl = "https://aviation-edge.com/v2/public/timetable?key=c95708-e9efa6&iataCode=PRG&type=departure";

        dbManager = new DatabaseManager(databasePath);
        apiClient = new ApiClient(apiUrl);

        timer = new System.Timers.Timer(600000); // 600000 ms = 10 minutes
        timer.Elapsed += async (sender, e) => await FetchAndStoreDepartures();
        timer.Start();

        // Initial fetch
        await FetchAndStoreDepartures();

        // Print filtered departures
        dbManager.PrintFilteredDepartures();

        // Keep the application running
        Console.ReadLine();
    }

    private static async Task FetchAndStoreDepartures()
    {
        try
        {
            var departures = await apiClient.GetDeparturesAsync();

            foreach (var departure in departures)
            {
                string flightNumber = departure["flight"]["iataNumber"]?.ToString();
                string destination = departure["arrival"]["iataCode"]?.ToString();
                string city = DatabaseManager.IATAToCityMap.ContainsKey(destination) ? DatabaseManager.IATAToCityMap[destination] : destination;
                string gate = departure["departure"]["gate"]?.ToString();
                string boardingTime = departure["departure"]["estimatedTime"]?.ToString();
                string scheduledTime = departure["departure"]["scheduledTime"]?.ToString();
                string actualTime = departure["departure"]["actualTime"]?.ToString();
                string status = departure["departure"]["status"]?.ToString();
                string terminal = departure["departure"]["terminal"]?.ToString();

                dbManager.InsertDeparture(flightNumber, destination, city, gate, boardingTime, scheduledTime, actualTime, status, terminal);
            }

            Console.WriteLine("Data successfully fetched and stored.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data: {ex.Message}");
        }
    }
}

public static class ConsoleHelper
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetCurrentConsoleFontEx(IntPtr consoleOutput, bool maximumWindow, ref CONSOLE_FONT_INFO_EX consoleCurrentFontEx);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CONSOLE_FONT_INFO_EX
    {
        internal uint cbSize;
        internal uint nFont;
        internal COORD dwFontSize;
        internal int FontFamily;
        internal int FontWeight;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        internal string FaceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        internal short X;
        internal short Y;

        internal COORD(short x, short y)
        {
            X = x;
            Y = y;
        }
    }

    public static void SetConsoleFontSize(int fontSize)
    {
        IntPtr hnd = GetStdHandle(-11);
        CONSOLE_FONT_INFO_EX info = new CONSOLE_FONT_INFO_EX();
        info.cbSize = (uint)Marshal.SizeOf(info);
        info.FaceName = "Consolas";
        info.dwFontSize = new COORD((short)fontSize, (short)fontSize);
        SetCurrentConsoleFontEx(hnd, false, ref info);
    }
}
