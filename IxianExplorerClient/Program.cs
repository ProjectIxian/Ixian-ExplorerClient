using IxianExplorerClient.Meta;
using IXICore;
using IXICore.Meta;
using IXICore.Utils;
using System.Reflection;

namespace IxianExplorerClient
{
    class Program
    {
        private static Thread? mainLoopThread;

        public static bool noStart = false;

        public static bool running = false;

        private static Node? node = null;

        static void Main(string[] args)
        {
            if (!Console.IsOutputRedirected)
            {
                // There are probably more problematic Console operations if we're working in stdout redirected mode, but 
                // this one is blocking automated testing.
                Console.Clear();
            }

            // Start logging
            if (!Logging.start(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), Config.logVerbosity))
            {
                IxianHandler.forceShutdown = true;
                Logging.info("Press ENTER to exit.");
                Console.ReadLine();
                return;
            }

            Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
                ConsoleHelpers.verboseConsoleOutput = true;
                Logging.consoleOutput = ConsoleHelpers.verboseConsoleOutput;
                e.Cancel = true;
                IxianHandler.forceShutdown = true;
            };

            if (!onStart(args))
            {
                return;
            }

            if (Node.apiServer != null)
            {
                while (IxianHandler.forceShutdown == false)
                {
                    Thread.Sleep(1000);
                }
            }

            onStop();
        }

        static bool onStart(string[] args)
        {
            Console.WriteLine("Ixian Explorer Client {0} ({1})", Config.version, CoreConfig.version);

            // Read configuration from command line
            if(!Config.init(args))
            {
                Environment.Exit(2);
                return false;
            }

            // Set the logging options
            Logging.setOptions(Config.maxLogSize, Config.maxLogCount);
            Logging.flush();

            Logging.info("Starting Ixian Explorer Lite Client {0} ({1})", Config.version, CoreConfig.version);
            Logging.info("Operating System is {0}", Platform.getOSNameAndVersion());

            // Log the parameters to notice any changes
            Logging.info("API Port: {0}", Config.apiPort);
            Logging.info("Wallet File: {0}", Config.walletFile);

            // Initialize the node
            node = new Node();

            if (noStart)
            {
                Thread.Sleep(1000);
                return false;
            }

            // Start the node
            node.start();
            
            running = true;

            if (mainLoopThread != null)
            {
                mainLoopThread.Interrupt();
                mainLoopThread.Join();
                mainLoopThread = null;
            }

            mainLoopThread = new Thread(mainLoop);
            mainLoopThread.Name = "Main_Loop_Thread";
            mainLoopThread.Start();

            if (ConsoleHelpers.verboseConsoleOutput)
                Console.WriteLine("-----------\nPress Ctrl-C or use the /shutdown API to stop the S2 process at any time.\n");

            return true;
        }

        static void mainLoop()
        {
            while (running)
            {
                try
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey();

                        if (key.Key == ConsoleKey.V)
                        {
                            ConsoleHelpers.verboseConsoleOutput = !ConsoleHelpers.verboseConsoleOutput;
                            Logging.consoleOutput = ConsoleHelpers.verboseConsoleOutput;
                            Console.CursorVisible = ConsoleHelpers.verboseConsoleOutput;
                            if (ConsoleHelpers.verboseConsoleOutput == false)
                                Node.statsConsoleScreen.clearScreen();
                        }
                        else if (key.Key == ConsoleKey.Escape)
                        {
                            ConsoleHelpers.verboseConsoleOutput = true;
                            Logging.consoleOutput = ConsoleHelpers.verboseConsoleOutput;
                            IxianHandler.forceShutdown = true;
                        }

                    }

                }
                catch (Exception e)
                {
                    Logging.error("Exception occured in mainLoop: " + e);
                }
                Thread.Sleep(1000);
            }
        }

        static void onStop()
        {
            running = false;

            if (noStart == false)
            {
                // Stop the node
                Node.stop();
            }

            // Stop logging
            Logging.flush();
            Logging.stop();

            if (noStart == false)
            {
                Console.WriteLine("");
                Console.WriteLine("Ixian Explorer Lite Client stopped.");
            }
        }

        public static void stop()
        {
            running = false;
        }
    }
}
