using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace SnickersBot
{
    class Program
    {
        static TelegramBotClient Bot;
        static bool Working = true;

        static void Beep()
            => Console.Beep(382, 1500);

        const int TG_OWN_ID = 138289291;

        static void Main(string[] args)
        {
            //Beep(); // test beep

            if (args.Count() < 1)
                return;

            var proxy = new MihaZupan.HttpToSocks5Proxy("192.168.1.9", 9050);

            Bot = new TelegramBotClient(args[0], proxy);

            Task.Run(async () =>
            {
                while (Working)
                {
                    try
                    {
                        await MainAsync(args);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERR: {ex}");
                    }
                }
            }).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var ownChat = new Telegram.Bot.Types.ChatId(TG_OWN_ID);

            try
            {
                var info = await Bot.GetMeAsync();
                Console.Title = $"@{info.Username}";
                Console.WriteLine("Noice, tg is working!");

                await Bot.SendTextMessageAsync(ownChat, "Tg working...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GET ME ERR: {ex.Message}");
                Working = false;
                return;
            }

            var httpCli = new RestClient("https://snickers.ru/")
            {
                CookieContainer = new System.Net.CookieContainer(),
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:69.0) Gecko/20100101 Firefox/69.0",
                FollowRedirects = true,
            };
            httpCli.AddDefaultHeader("Sec-Fetch-Mode", "cors");
            httpCli.AddDefaultHeader("Accept", "*/*");
            httpCli.AddDefaultHeader("Sec-Fetch-Site", "same-origin");
            httpCli.AddDefaultHeader("Accept-Encoding", "gzip, deflate, br");
            httpCli.AddDefaultHeader("Accept-Language", "en-US,en;q=0.9");

            var baseReq = new RestRequest(Method.GET);
            var baseRes = httpCli.Execute(baseReq);

            if (!baseRes.IsSuccessful)
            {
                Console.WriteLine($"BASE REQ ERR: {baseRes.ErrorException?.Message ?? "Something went wrong"}");
                Working = false;
                return;
            }

            httpCli.AddDefaultHeader("Referer", "https://snickers.ru/");

            var quotesReq = new RestRequest("/api/getQuotes", Method.GET);
            quotesReq.AddParameter("start", 0);

            var availProds = new RestRequest("/api/getAvailableProducts", Method.GET);
            availProds.AddHeader("X-Requested-With", "XMLHttpRequest");

            while (Working)
            {
                var quotesRes = httpCli.Execute(quotesReq);
                if (!quotesRes.IsSuccessful)
                {
                    Console.WriteLine($"REQ QUOTE FAIL: {quotesRes.ErrorException?.Message ?? "Unknown..."}");
                    Thread.Sleep(1000);
                    continue;
                }

                Thread.Sleep(1000);

                var prodsRes = httpCli.Execute(availProds);

                var prodsAvail = default(ReadOnlyCollection<Models.ProductItem>);
                try
                {
                    var pData = JsonConvert.DeserializeObject<Models.ApiResponse<ReadOnlyCollection<Models.ProductItem>>>(prodsRes.Content);
                    if (pData.Error.HasValue && pData.Error != 0)
                        Console.WriteLine($"PRODS Error: {pData.Error}");
                    else
                        prodsAvail = pData.Result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"REQ PRODS FAIL: {ex.Message}");
                }

                try
                {
                    var qData = JsonConvert.DeserializeObject<Models.ApiResponse<ReadOnlyCollection<Models.Quote.QuoteItem>>>(quotesRes.Content);
                    if (qData.Error.HasValue && qData.Error != 0)
                    {
                        Console.WriteLine($"QUOTE Error: {qData.Error}");
                        Thread.Sleep(5000);
                        continue;
                    }

                    var lastq = qData.Result.FirstOrDefault();
                    if (lastq.Value >= 40) // 40% off
                    {
                        try
                        {
                            if ((prodsAvail?.Count ?? 0) > 0)
                            {
                                var prodNum = 0;
                                var prodsList = prodsAvail.Select(x => $"{++prodNum}. {x.Name} | Вес: {x.Weight}");
                                await Bot.SendTextMessageAsync(ownChat,
                                    $"Там это. 50% https://snickers.ru/\n\n{string.Join("\n", prodsList)}");

                                // try to get some
                                if ((prodsAvail?.Count ?? 0) > 0)
                                {
                                    try
                                    {
                                        var couponeReq = new RestRequest("/api/getCoupon", Method.GET);
                                        couponeReq.AddHeader("X-Requested-With", "XMLHttpRequest");

                                        var prodWanted = prodsAvail.FirstOrDefault(x => Regex.IsMatch(x?.Name ?? "", "марс", RegexOptions.IgnoreCase))
                                            ?? prodsAvail.FirstOrDefault(x => Regex.IsMatch(x?.Name ?? "", "crisper", RegexOptions.IgnoreCase))
                                            ?? prodsAvail.First();

                                        if (prodWanted != null)
                                        {
                                            couponeReq.AddParameter("product_id", prodWanted.Id);

                                            Console.WriteLine($"Getting coupone for: {prodWanted.Name}");

                                            var couponeRes = httpCli.Execute(couponeReq);
                                            if (couponeRes.IsSuccessful)
                                            {
                                                var cData = JsonConvert.DeserializeObject<Models.ApiResponse<Models.Coupon>>(couponeRes.Content);
                                                var code = cData.Result.Id;
                                                Console.WriteLine($"Your coupone is: https://snickers.ru/#coupon/{code}");

                                                try
                                                {
                                                    var useTil = cData.Result.Time.Value.LocalDateTime + TimeSpan.FromDays(1);

                                                    await Bot.SendTextMessageAsync(ownChat,
                                                        $"Купон для {prodWanted.Name} | Вес {prodWanted.Weight}\n" +
                                                        $"\n" +
                                                        $"https://snickers.ru/#coupon/{code}\n" +
                                                        $"\n" +
                                                        $"Код можно активировать до {useTil.ToString("dd.MM.yyyy HH:mm:ss")}");
                                                }
                                                catch(Exception ex)
                                                {
                                                    Console.WriteLine("Failed to send coupon into telegram");
                                                }
                                            }
                                            else
                                                Console.WriteLine("Failed to request coupon");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"REQ COUPONE FAIL: {ex.Message}");
                                    }
                                }
                            }
                            else
                                await Bot.SendTextMessageAsync(ownChat, $"Там это. 50% https://snickers.ru/");
                        }
                        catch (Exception ex)
                        {
                            Beep();
                            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss dd.MM.yyyy")} / failed to send notify!");
                            Console.WriteLine($"MESAGE: {ex.Message}");
                        }
                    }

                    var sleepTill = lastq.Time.LocalDateTime + TimeSpan.FromMinutes(11);
                    var sleepTime = sleepTill - DateTime.Now; // calc how log to sleep
                    Console.WriteLine($"[{lastq.Value}% off] Will sleep for {sleepTime.TotalSeconds} seconds...");
                    Thread.Sleep(sleepTime);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"REQ QUOTE FAIL: {ex.Message}");
                    Thread.Sleep(1000);
                    continue;
                }
            }
        }
    }
}
