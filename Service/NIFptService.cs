using Microsoft.Extensions.Options;
using RestSharp;
using System;
using System.Net;
using System.Threading.Tasks;

namespace NIFPTWorker.Service;

public class NIFptService : IDisposable {

    private readonly RestClient _restClient;

    public NIFptService(IOptions<ServiceConfiguration> options) {
        ServiceConfiguration configuration = options.Value;

        // build url
        var url = $"{configuration.Url}?json=1&key={configuration.Key}";

        var restOptions = new RestClientOptions(url) {
            ThrowOnAnyError = false,
            MaxTimeout = -1,
            Expect100Continue = false,
        };
        _restClient = new RestClient(restOptions);
    }

    public void Dispose() {
        _restClient?.Dispose();
        GC.SuppressFinalize(this);
    }


    /// <summary>
    /// Get information of the NIF
    /// </summary>
    /// <param name="nif">valid NIF</param>
    /// <returns>NIFPT response</returns>
    public async Task<RestResponse> GetNIFInfo(string nif) {

        // setup proxy with current user
        var proxy = WebRequest.DefaultWebProxy;
        proxy.Credentials = CredentialCache.DefaultCredentials;

        // setup request
        var request = new RestRequest()
            .AddParameter("q", nif);

        // make request
        var response = await _restClient.GetAsync(request);

        // return object
        return response;
    }



}
