using Fclp;
using IXICore.Meta;

namespace IxianExplorerClient.Meta
{
    class Config
    {
        public static int apiPort = 8001;
        public static Dictionary<string, string> apiUsers = new Dictionary<string, string>();
        public static List<string> apiAllowedIps = new List<string>();
        public static List<string> apiBinds = new List<string>();

        public static string walletFile = "ixian.wal";
        public static bool onlyShowAddresses = false;

        public static int maxLogSize = 50; // MB
        public static int maxLogCount = 10;

        public static int logVerbosity = (int)LogSeverity.info + (int)LogSeverity.warn + (int)LogSeverity.error;

        public static readonly string version = "xexc-0.9.3"; // ExplorerClient version

        public static string explorerAPIBaseUrl = "https://explorer.ixian.io/api/v1";
        public static string explorerAPIKey = ""; // Set the API KEY here or supply it via commandline

        private static string outputHelp()
        {
            Program.noStart = true;

            Console.WriteLine("Starts a new instance of IxianExplorerClient");
            Console.WriteLine("");
            Console.WriteLine(" IxianExplorerClient.exe [-h] [-v] [-w ixian.wal]");
            Console.WriteLine("");
            Console.WriteLine("    -h\t\t\t Displays this help");
            Console.WriteLine("    -v\t\t\t Displays version");
            Console.WriteLine("    -w\t\t\t Specify name of the wallet file");
            Console.WriteLine("    --apiurl\t\t Specify the explorer API URL (default https://explorer.ixian.io/api/v1)");
            Console.WriteLine("    --apikey\t\t Specify the explorer API key");
            
            return "";
        }
        private static string outputVersion()
        {
            Program.noStart = true;

            // Do nothing since version is the first thing displayed

            return "";
        }
        public static bool init(string[] args)
        {
            var cmd_parser = new FluentCommandLineParser();

            cmd_parser.SetupHelp("h", "help").Callback(text => outputHelp());
            cmd_parser.Setup<bool>('v', "version").Callback(text => outputVersion());
            cmd_parser.Setup<string>('w', "wallet").Callback(value => walletFile = value).Required();
            cmd_parser.Setup<string>("apiurl").Callback(value => explorerAPIBaseUrl = value).Required();
            cmd_parser.Setup<string>("apikey").Callback(value => explorerAPIKey = value).Required();
            cmd_parser.Parse(args);

            if (explorerAPIKey == "")
            {
                Console.WriteLine("ERROR: Ixian Explorer API KEY is missing!\nAdd it using the --apikey commandline parameter");
                return false;
            }

            return true;
        }
    }
}
