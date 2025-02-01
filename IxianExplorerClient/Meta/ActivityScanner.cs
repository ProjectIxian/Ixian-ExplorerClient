using IxianExplorerClient.API;
using IXICore;
using IXICore.Meta;

namespace IxianExplorerClient.Meta
{
    class ActivityScanner
    {
        private static bool shouldStop = false; // flag to signal shutdown of threads
        private static bool active = false;
        private static bool syncing = false;
        private static ulong lastBlockNum = 0;

        // Starts the activity scanner thread
        public static bool start()
        {
            // Check if activity scanner is enabled and already active
            if (Config.enableActivityScanner == false || isActive())
            {
                return false;
            }

            //ActivityStorage.clearStorage();

            active = true;
            shouldStop = false;

            try
            {
                Thread scanner_thread = new Thread(threadLoop);
                scanner_thread.Name = "Activity_Scanner_Thread";
                scanner_thread.Start();
            }
            catch
            {
                active = false;
                Logging.error("Cannot start ActivityScanner");
                return false;
            }

            return true;
        }

        // Signals the activity scanner thread to stop
        public static bool stop()
        {
            shouldStop = true;
            return true;
        }

        public static bool clearStorage()
        {
            if (!ActivityStorage.clearStorage(0))
            {
                Logging.error("Cannot clear activity storage");
                shouldStop = true;
                active = false;
                return true;
            }
            return false;
        }

        private static void fetchAllTransactionsForAddress(string address)
        {
            int txCount = APIClient.getTransactionCountByAddressAsync(address);
            Logging.info($"Fetching {txCount} transactions for {address}");

            int totalPages = (int)Math.Ceiling(txCount / (double)Config.explorerAPITransactionPaginationLimit);

            for (int page = 1; page <= totalPages; page++)
            {
                bool result = APIClient.getTransactionsByAddressAsync(address, page);
                if (!result)
                {
                    Logging.error("Error fetching transactions for " + address);
                }
            }
        }

        public static void fetchAll()
        {
            syncing = true;
            try
            {
                List<Address> address_list = IxianHandler.getWalletStorage().getMyAddresses();
                foreach (Address addr in address_list)
                {
                    fetchAllTransactionsForAddress(addr.ToString());
                }

            }
            catch (Exception e)
            {
                Logging.error("ActivityScanner Fetch All error: " + e);
            }
            syncing = false;
        }

        private static void fetchUpdates()
        {

            try
            {
                List<Address> address_list = IxianHandler.getWalletStorage().getMyAddresses();
                foreach (Address addr in address_list)
                {
                    List<Activity> res = ActivityStorage.getActivitiesByAddress(addr.ToString(), 0, 1, true);
                    if(res == null)
                    {
                        continue;
                    }
                    if (res.Count == 0)
                    {
                        fetchAllTransactionsForAddress(addr.ToString());
                        continue;
                    }

                    Activity latest_activity = res.First();
                    APIClient.getTransactionUpdatesByAddressAsync(addr.ToString(), latest_activity.txid);
                }
            }
            catch (Exception e)
            {
                Logging.error("ActivityScanner Fetch error: " + e);
            }
        }

        private static void threadLoop(object data)
        {
            
            while (shouldStop == false)
            {
                if (!syncing)
                {
                    fetchUpdates();
                }

                Thread.Sleep(30000);
            }

            active = false;
        }

        // Check if the activity scanner is already running
        public static bool isActive()
        {
            return active;
        }

        // Check if the activity scanner is synchronizing
        public static bool isSyncing()
        {
            return syncing;
        }

        // Return activity scanner processed block number
        public static ulong getLastBlockNum()
        {
            return lastBlockNum;
        }
    }
}
