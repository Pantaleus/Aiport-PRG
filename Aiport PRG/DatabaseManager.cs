using System;
using System.Collections.Generic;
using System.Data.SQLite;
using ConsoleTables;

public class DatabaseManager
{
    private string connectionString;
    public static Dictionary<string, string> IATAToCityMap = new Dictionary<string, string>()
    {
        {"PRG", "Prague"},
        {"LTN", "London Luton"},
        {"FRA", "Frankfurt"},
        {"BOJ", "Burgas"},
        {"BOD", "Bordeaux"},
        {"SOF", "Sofia"},
        {"LHR", "London Heathrow"},
        {"AGP", "Malaga"},
        {"ARN", "Stockholm Arlanda"},
        {"CDG", "Paris Charles de Gaulle"},
        {"IST", "Istanbul"},
        {"CGN", "Cologne"},
        {"STN", "London Stansted"},
        {"MXP", "Milan Malpensa"},
        {"LEI", "Almeria"},
        {"FCO", "Rome Fiumicino"},
        {"PMI", "Palma de Mallorca"},
        {"OSL", "Oslo"},
        {"ORY", "Paris Orly"},
        {"BWE", "Braunschweig"},
        {"RHO", "Rhodes"},
        {"AMS", "Amsterdam"},
        {"CFU", "Corfu"},
        {"BGY", "Bergamo"},
        {"VLC", "Valencia"},
        {"ATH", "Athens"},
        {"DLM", "Dalaman"},
        {"BRU", "Brussels"},
        {"LBG", "Le Bourget"},
        {"OLB", "Olbia"},
        {"GVA", "Geneva"},
        {"NCE", "Nice"},
        {"MIR", "Monastir"},
        {"ZRH", "Zurich"},
        {"AYT", "Antalya"},
        {"WAW", "Warsaw"},
        {"DOH", "Doha"},
        {"BEG", "Belgrade"},
        {"DUS", "Dusseldorf"},
        {"SVQ", "Seville"},
        {"EIN", "Eindhoven"},
        {"BCN", "Barcelona"},
        {"DXB", "Dubai"},
        {"LGW", "London Gatwick"},
        {"ADB", "Izmir"},
        {"BRS", "Bristol"},
        {"TLV", "Tel Aviv"},
        {"KEF", "Reykjavik Keflavik"},
        {"MAN", "Manchester"},
        {"LCA", "Larnaca"},
        {"PVK", "Preveza"},
        {"BJV", "Bodrum"},
        {"BUD", "Budapest"},
        {"HER", "Heraklion"},
        {"VAR", "Varna"},
        {"ZTH", "Zakynthos"},
        {"BRI", "Bari"},
        {"SSH", "Sharm El Sheikh"},
        {"RMF", "Marsa Alam"},
        {"HRG", "Hurghada"},
        {"KGS", "Kos"},
        {"RMU", "Murcia"},
        {"PSR", "Pescara"},
        {"MUH", "Marsa Matruh"},
        {"TIA", "Tirana"},
        {"TLL", "Tallinn"}
    };

    public DatabaseManager(string databasePath)
    {
        connectionString = $"Data Source={databasePath};Version=3;";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Departures (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FlightNumber TEXT,
                    Destination TEXT,
                    City TEXT,
                    Gate TEXT,
                    BoardingTime TEXT,
                    ScheduledTime TEXT,
                    ActualTime TEXT,
                    Status TEXT,
                    Terminal TEXT
                )";
            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    public void InsertDeparture(string flightNumber, string destination, string city, string gate, string boardingTime, string scheduledTime, string actualTime, string status, string terminal)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            string insertQuery = @"
                INSERT INTO Departures (FlightNumber, Destination, City, Gate, BoardingTime, ScheduledTime, ActualTime, Status, Terminal)
                VALUES (@FlightNumber, @Destination, @City, @Gate, @BoardingTime, @ScheduledTime, @ActualTime, @Status, @Terminal)";
            using (var command = new SQLiteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@FlightNumber", flightNumber);
                command.Parameters.AddWithValue("@Destination", destination);
                command.Parameters.AddWithValue("@City", city);
                command.Parameters.AddWithValue("@Gate", gate);
                command.Parameters.AddWithValue("@BoardingTime", boardingTime);
                command.Parameters.AddWithValue("@ScheduledTime", scheduledTime);
                command.Parameters.AddWithValue("@ActualTime", actualTime);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Terminal", terminal);
                command.ExecuteNonQuery();
            }
        }
    }

    public void PrintFilteredDepartures()
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            string selectQuery = "SELECT * FROM Departures";
            using (var command = new SQLiteCommand(selectQuery, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    var table = new ConsoleTable("FlightNumber", "Destination", "City", "Gate", "BoardingTime", "ScheduledTime", "ActualTime", "Status", "Terminal");
                    DateTime now = DateTime.UtcNow;

                    while (reader.Read())
                    {
                        string scheduledTimeStr = reader["ScheduledTime"].ToString();
                        DateTime scheduledTime;
                        if (!DateTime.TryParse(scheduledTimeStr, out scheduledTime))
                        {
                            continue; // Skip invalid datetime
                        }

                        string actualTimeStr = reader["ActualTime"].ToString();
                        DateTime? actualTime = null;
                        if (!string.IsNullOrEmpty(actualTimeStr))
                        {
                            DateTime tempActualTime;
                            if (DateTime.TryParse(actualTimeStr, out tempActualTime))
                            {
                                actualTime = tempActualTime;
                            }
                        }

                        string status = reader["Status"].ToString();
                        string statusUpdate = status;

                        if (scheduledTime <= now.AddHours(24) && scheduledTime >= now.AddHours(-1))
                        {
                            if (actualTime.HasValue && actualTime.Value > scheduledTime)
                            {
                                TimeSpan delay = actualTime.Value - scheduledTime;
                                statusUpdate = $"Opožděn ({delay.TotalMinutes} min)";
                            }
                            else if (status == "cancelled")
                            {
                                statusUpdate = "Zrušen";
                            }
                            else if (status == "boarding")
                            {
                                statusUpdate = "Boarding";
                            }

                            table.AddRow(
                                reader["FlightNumber"],
                                reader["Destination"],
                                reader["City"],
                                reader["Gate"],
                                reader["BoardingTime"],
                                reader["ScheduledTime"],
                                actualTime.HasValue ? actualTime.ToString() : "",
                                statusUpdate,
                                reader["Terminal"]
                            );
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    table.Write();
                    Console.ResetColor();
                }
            }
        }
    }
}
