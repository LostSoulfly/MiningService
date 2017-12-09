namespace IdleService
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Load the configuration from a file, in the current directory
            Config.LoadConfigFromFile();

            //Start the TopShelf library, and begin starting the actual Service
            ConfigureService.Configure();
        }
    }
}