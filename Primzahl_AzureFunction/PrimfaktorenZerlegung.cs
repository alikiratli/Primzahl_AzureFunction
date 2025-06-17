using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Primzahl_AzureFunction
{
    public class PrimfaktorenZerlegung
    {
        private readonly ILogger<PrimfaktorenZerlegung> _logger;

        public PrimfaktorenZerlegung(ILogger<PrimfaktorenZerlegung> logger)
        {
            _logger = logger;
        }

        //[Function("Function1")]
        //public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        //{
        //    _logger.LogInformation("C# HTTP trigger function processed a request.");
        //    return new OkObjectResult("Welcome to Azure Functions!");
        //}
        [Function("PrimfaktorenZerlegung")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            int zahl = 0;

            var response = req.CreateResponse();
            

            if (req.Method == "GET")
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                int.TryParse(query["zahl"], out zahl);
            }
            else if (req.Method == "POST")
            {
                var body = await req.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        var data = JsonSerializer.Deserialize<RequestModel>(body, options);
                        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                        int.TryParse(query["zahl"], out zahl);

                        if (data != null)
                        {
                            Console.WriteLine($"Empfangener Wert aus POST: {data.Zahl}");
                            zahl = data.Zahl;
                        }
                        else
                        {
                            response.StatusCode = HttpStatusCode.BadRequest;
                            await response.WriteStringAsync(JsonSerializer.Serialize(new
                            {
                                fehler = "JSON konnte nicht gelesen werden."
                            }));
                            return response;
                        }
                    }
                    catch (JsonException je)
                    {
                        Console.WriteLine($"JSON Fehler: {je.Message}");
                        response.StatusCode = HttpStatusCode.BadRequest;
                        await response.WriteStringAsync(JsonSerializer.Serialize(new
                        {
                            fehler = "Ungültiges JSON-Format. Beispiel: { \"zahl\": 123456 }"
                        }));
                        return response;
                    }
                }
                else
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        fehler = "POST-Body ist leer."
                    }));
                    return response;
                }
            }

            if (zahl <= 1)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    fehler = "Bitte geben Sie eine ganze Zahl größer als 1 an."
                }));
                return response;
            }

            var faktoren = ZerlegeInPrimfaktoren(zahl);

            response.StatusCode = HttpStatusCode.OK;
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new
            {
                zahl = zahl,
                primfaktoren = faktoren
            }));

            return response;
        }

        // Primfaktorzerlegung
        static List<int> ZerlegeInPrimfaktoren(int zahl)
        {
            var faktoren = new List<int>();
            for (int teiler = 2; teiler <= zahl / teiler; teiler++)
            {
                while (zahl % teiler == 0)
                {
                    faktoren.Add(teiler);
                    zahl /= teiler;
                }
            }
            if (zahl > 1)
                faktoren.Add(zahl);
            return faktoren;
        }

        private class RequestModel
        {
            public int Zahl { get; set; }
        }
    }
}
