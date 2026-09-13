/*
##########################################
#           TikTok Downloader            #
#           Made by Jettcodey            #
#                © 2024                  #
#           DO NOT REMOVE THIS           #
##########################################
*/
using System.Text;

namespace TikTok_Downloader
{
    internal static class Program
    {
        [STAThread]
        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            if (args.Any(argument => argument.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                                     argument.Equals("-h", StringComparison.OrdinalIgnoreCase)))
            {
                KeywordDownloader.PrintHelp();
                return 0;
            }

            using var cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
                Console.WriteLine("\nMembatalkan proses...");
            };

            try
            {
                using var downloader = new KeywordDownloader();
                return await downloader.RunInteractiveAsync(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Proses dibatalkan.");
                return 130;
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Gagal: {exception.Message}");
                Console.ResetColor();
                return 1;
            }
        }
    }
}
