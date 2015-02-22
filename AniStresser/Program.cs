using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AniStresser
{
    class Program
    {
        private static string hostName;

        private static string requestUrl;

        private static string appKey;

        private static string protocol = "https";

        private static StreamWriter outWriter = null;

        private static HttpClient workerClient;

        private static int errorsCount = 0;

        private static string requestContent;

        static void Main(string[] args)
        {
            if (args.Length != 6)
            {
                Console.WriteLine("Usage: AntiStresser [host name] [url] [app key] [output file] [input file] [concurrency start:end:repeats]");
                return;
            }

            int w1, w2;
            ThreadPool.GetMinThreads(out w1, out w2);
            Console.WriteLine(w1);
            Console.WriteLine(w2);

            ThreadPool.GetMaxThreads(out w1, out w2);
            Console.WriteLine(w1);
            Console.WriteLine(w2);

            ThreadPool.SetMinThreads(12, 12);

            ThreadPool.GetMinThreads(out w1, out w2);
            Console.WriteLine(w1);
            Console.WriteLine(w2);

            ServicePointManager.DefaultConnectionLimit = 1000;

            workerClient = new HttpClient();

            workerClient.DefaultRequestHeaders.Accept.Clear();
            workerClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // ignore SSL errors
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            if (args.Length > 0)
            {
                hostName = args[0];
            }

            if (args.Length > 1)
            {
                requestUrl = args[1];
            }

            if (args.Length > 2)
            {
                appKey = args[2];
            }

            string outFileName = string.Empty;

            if (args.Length > 3)
            {
                outFileName = args[3];

                if (File.Exists(outFileName))
                {
                    File.Delete(outFileName);
                }

                outWriter = File.CreateText(outFileName);
            }

            if (args.Length > 4)
            {
                string inFileName = args[4];

                requestContent = File.ReadAllText(inFileName);

                Console.WriteLine("Using request:\n{0}\n", requestContent);
            }

            int concurrentStart = 1;
            int concurrentEnd = 8;
            int repeats = 100;

            if (args.Length > 5)
            {
                string testMetrics = args[5];

                // format: start:end:count
                string[] m = testMetrics.Split(':');
                if (m.Length != 3)
                {
                    Console.WriteLine("Metrics format- start:end:count");
                    return;
                }

                concurrentStart = int.Parse(m[0]);
                concurrentEnd = int.Parse(m[1]);
                repeats = int.Parse(m[2]);
            }

            if (!string.IsNullOrWhiteSpace(appKey))
            {
                workerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appKey);
            }

            Console.WriteLine("AZURE ML RRS API WARMER");
            Console.WriteLine("STARTING:");
            Console.WriteLine("Using {0}://{1}/{2}", protocol, hostName, requestUrl);
            Console.WriteLine("Output to {0}", outFileName);
            Console.WriteLine("Start from {0} concurrent requests, to {1}, repeat {2} times.", concurrentStart, concurrentEnd, repeats);
            Console.WriteLine("---------------------------------------------------------------");

            var p = new Program();
            Console.WriteLine();

            Dictionary<int, List<long>> allLatencies = new Dictionary<int, List<long>>();

            Console.WriteLine("Initial warm with with {0} requests.", concurrentEnd * 2);
            p.RunParallel(concurrentEnd * 2);
            Console.WriteLine("Initial warm up done");

            long totalReqs = 0;

            Stopwatch sw = Stopwatch.StartNew();

            for (int concurrentReqs = concurrentStart; concurrentReqs <= concurrentEnd; ++concurrentReqs)
            {
                var cl = new List<long>();
                allLatencies.Add(concurrentReqs, cl);

                for (int repeat = 0; repeat < repeats; ++repeat)
                {
                    Console.WriteLine("{0}> {1}({2}): ", DateTimeOffset.Now, concurrentReqs, repeat + 1);

                    var latencies = p.RunParallel(concurrentReqs);

                    totalReqs += concurrentReqs;

                    long avg = 0;
                    long min = long.MaxValue;
                    long max = long.MinValue;
                    long count = 0;
                    foreach (var latency in latencies)
                    {
                        Console.Write("{0}, ", latency);

                        avg = ((avg * count) + latency) / (count + 1);
                        count++;

                        min = Math.Min(min, latency);
                        max = Math.Max(max, latency);

                        cl.Add(latency);
                    }

                    Console.WriteLine("| AVG = {0}", avg);

                    string logLine = string.Format("{0}\t{1}\t{2}\t{3}\t{4}", DateTimeOffset.Now, concurrentReqs, avg, min, max);
                    Log(logLine);
                }
            }

            sw.Stop();

            Console.WriteLine("\nOVERALL STATS:");
            foreach (var c in allLatencies.Keys)
            {
                var l = allLatencies[c];
                Console.WriteLine("\t{0} -> AVG = {1}", c, l.Average());
            }

            Console.WriteLine("\n TOTAL {0} requests, {1} rps", totalReqs, ((double)totalReqs / sw.Elapsed.TotalSeconds));

            if (outWriter != null)
            {
                outWriter.Flush();
                outWriter.Close();
                outWriter = null;
            }

            Console.WriteLine(" ERRORS: {0}", errorsCount);

            //workerClient.Dispose();

            Console.WriteLine("\n\nDONE.");
        }

        public IEnumerable<long> RunParallel(int tasksCount)
        {
            List<Task<long>> tt = new List<Task<long>>();

            for (int i = 0; i < tasksCount; ++i)
            {
                tt.Add(this.RRSTest(i.ToString()));
            }

            var tasks = tt.ToArray();

            Task.WaitAll(tasks);

            return tasks.Select(t => t.Result);
        }

        public async Task<long> RRSTest(string reqId)
        {
            {
                var postBody = new StringContent(
                    Program.requestContent.Replace("[ID]", reqId),
                    Encoding.UTF8,
                    "application/json");

                Guid req = Guid.NewGuid();
                postBody.Headers.TryAddWithoutValidation("x-ms-request-id", req.ToString());

                Uri postUri = new Uri(string.Format("{0}://{1}/{2}", protocol, hostName, requestUrl));

                Stopwatch sw = Stopwatch.StartNew();
                HttpResponseMessage response = await workerClient.PostAsync(postUri, postBody);
                string returnString = await response.Content.ReadAsStringAsync();
                sw.Stop();

                IEnumerable<string> reqs;
                string rrsId = null;
                if (response.Headers.TryGetValues("x-ms-request-id", out reqs))
                {
                    rrsId = reqs.FirstOrDefault();
                }

                Console.WriteLine("{0}->{1} {2}", req, rrsId, sw.ElapsedMilliseconds);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error {0}, {1}.", response.StatusCode, returnString);
                    Console.WriteLine(returnString);
                    errorsCount++;
                    return 0;
                }

                response.Dispose();
                postBody.Dispose();


                return sw.ElapsedMilliseconds;
            }
        }

        static void Log(string msg)
        {
            if (outWriter != null)
            {
                outWriter.WriteLine(msg);
            }
        }
    }
}
