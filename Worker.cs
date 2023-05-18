using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NIFPTWorker; 
public class Worker : BackgroundService {

    private readonly DbContext _dbContext;
    private readonly NIFptService _nifService;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;


    public Worker(DbContext dbContext,
                  NIFptService nifService,
                  IHostApplicationLifetime hostApplicationLifetime) {
        _dbContext = dbContext;
        _nifService = nifService;
        _hostApplicationLifetime = hostApplicationLifetime;
    }


    /// <summary>
    /// Main process
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {

        Vendor vendor = null;
        try {
            // get vendor 
            vendor = await _dbContext.SelectVendor();

            // process vendor
            if (vendor is not null) {
                Log.Information($"Selected vendor: {vendor.NIF}");

                // get data from service
                var apiResponse = await _nifService.GetNIFInfo(vendor.NIF);
                Log.Debug($"Service response: {apiResponse.StatusCode}.");

                if (apiResponse.StatusCode == HttpStatusCode.OK) {
                    var nifInfo = JsonConvert.DeserializeObject<NIFResponse>(apiResponse.Content);

                    if (nifInfo.Result.Equals("success")) {

                        // update vendor contacts
                        await ProcessServiceResponse(vendor, nifInfo);
                        Log.Debug("Finish processing service response.");
                    }
                    else {
                        Log.Warning($"Unsuccessful request response. {nifInfo.Message}");

                        // register vendor as processed since this vendor does not have info
                        if (nifInfo.Message == "No records found") {
                            // mark vendor as being processed
                            Log.Debug("Vendor marked as processed.");
                            await _dbContext.MarkVendor(vendor.GUID);
                        }
                    }

                    // check credits
                    CheckServiceCredits(nifInfo);
                }
                else {
                    Log.Warning($"Externar service communication unsuccessful. {apiResponse.StatusCode}:{apiResponse.Content}");
                }

            }
            else {
                Log.Information("No vendor selected.");
            }
            Log.Information("Import finished.");


        } catch (Exception ex) {
            Log.Fatal(ex, "Exception thrown");
            throw;
        } finally {
            // mark vendor as being processed
            if(vendor?.GUID is not null) {
                await _dbContext.MarkVendor(vendor.GUID);
                Log.Debug("Vendor marked as processed.");
            }

            Log.Information("Exiting application.");
        }
    }


    /// <summary>
    /// Load service information to VMS
    /// </summary>
    /// <param name="apiResponse">NIFResponse object</param>
    /// <returns></returns>
    private async Task ProcessServiceResponse(Vendor vendor, NIFResponse apiResponse) {


        if (apiResponse.Records.TryGetValue(vendor.NIF, out var nifDto)) {

            // title
            if (!string.Equals(nifDto.Title, vendor.Name, StringComparison.OrdinalIgnoreCase)) {
                await _dbContext.UpdateVendorName(vendor.GUID, nifDto.Title);
            }

            // address
            if (!string.IsNullOrWhiteSpace(nifDto.Address)) {
                var fullAddress = $"{nifDto.Address} {nifDto.Pc4}-{nifDto.Pc3} {nifDto.City}";
                await _dbContext.UpdateVendorContact(ContactType.ADDRESS, vendor.GUID, fullAddress);
            }

            // other contacts
            if (nifDto.Contacts is not null) {
                // email
                if (!string.IsNullOrWhiteSpace(nifDto.Contacts.Email)) {
                    await _dbContext.UpdateVendorContact(ContactType.EMAIL, vendor.GUID, nifDto.Contacts.Email);
                }

                // phone
                if (!string.IsNullOrWhiteSpace(nifDto.Contacts.Phone)) {
                    if (nifDto.Contacts.Phone.StartsWith("9")) {
                        await _dbContext.UpdateVendorContact(ContactType.MOBILEPHONE, vendor.GUID, nifDto.Contacts.Phone);
                    }
                    else {
                        await _dbContext.UpdateVendorContact(ContactType.TELEPHONE, vendor.GUID, nifDto.Contacts.Phone);
                    }
                }

                // website
                if (!string.IsNullOrWhiteSpace(nifDto.Contacts.Website)) {
                    await _dbContext.UpdateVendorContact(ContactType.WEBSITE, vendor.GUID, nifDto.Contacts.Website);
                }
            }
        }
    }


    /// <summary>
    /// Check credits left 
    /// </summary>
    /// <param name="apiResponse">Service response</param>
    private static void CheckServiceCredits(NIFResponse apiResponse) {
        try {
            var credits = apiResponse.Credits.Left;
            if (credits.Hour == 1 || credits.Day == 1) {
                Log.Warning("LOW CREDITS! Check service frequency!");
            }
            else {
                Log.Information($"Credits left: Month({credits.Month}) Day({credits.Day}) Hour({credits.Hour})");
            }
        } catch (Exception ex) {
            Log.Error(ex, "Could not get credits information.");
        }
    }


}
