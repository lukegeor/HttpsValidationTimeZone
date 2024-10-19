using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile(Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "appsettings.json"), optional: false);

var config = builder.Build();

var serverAddress = config.GetValue<string>("ServerAddress") ?? throw new InvalidOperationException("No server address specified");

var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback =  (message, cert, chain, errors) => {
    Console.WriteLine($"Not before Local {cert?.NotBefore.ToLocalTime().ToString() ?? "<no cert>"} Utc {cert?.NotBefore.ToUniversalTime().ToString() ?? "<no cert>"} /// errors = {errors}");
    if (chain is not null) Console.WriteLine($"Chain status: {string.Join(",", chain.ChainStatus.Select(x => x.Status))}");
    return errors == SslPolicyErrors.None;
};

var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.ConnectionClose = true;

while (true)
{
    try
    {
        var now = DateTime.Now;
        Console.WriteLine($"Current time Local {now} Utc {now.ToUniversalTime()}");
        var response = await httpClient.GetAsync(serverAddress);
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }

    await Task.Delay(TimeSpan.FromSeconds(5));
}
