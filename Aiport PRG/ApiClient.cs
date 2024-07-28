using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class ApiClient
{
    private static readonly HttpClient client = new HttpClient();
    private string apiUrl;

    public ApiClient(string apiUrl)
    {
        this.apiUrl = apiUrl;
    }

    public async Task<JArray> GetDeparturesAsync()
    {
        var response = await client.GetStringAsync(apiUrl);
        return JArray.Parse(response);
    }
}
