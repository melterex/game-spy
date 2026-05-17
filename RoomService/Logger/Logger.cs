namespace RoomService
{
    public static class Logger
    {
        private static List<string> _logs = new();

        public static void Log(string message) => _logs.Add(message);
        public static void PrintLogs()
        {
            foreach (var log in _logs)
                Console.WriteLine(log);
        }
    }
}