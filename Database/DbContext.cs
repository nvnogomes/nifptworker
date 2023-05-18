using Dapper;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace NIFPTWorker.Database;

public class DbContext {

    private readonly string _connectionString;
    private readonly string _workerName;

    public DbContext(IConfiguration configuration) {
        _connectionString = configuration.GetConnectionString("Base");
        _workerName = configuration.GetValue<string>("Worker:Name");
    }

    /// <summary>
    /// Check if connectionString assigned
    /// </summary>
    private void CheckConnectionString() {
        if (string.IsNullOrWhiteSpace(_connectionString)) {
            Log.Fatal("ConnectionString not found in the configuration file");
            throw new Exception("No connection found");
        }
    }


    /// <summary>
    /// Execute query and returns the given type
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="sql">SQL query</param>
    /// <returns>T object</returns>
    private async ValueTask<T> Query<T>(string sql) {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<T>(sql);
    }


    /// <summary>
    /// Execute sql without response
    /// </summary>
    /// <param name="sql">SQL query</param>
    /// <returns></returns>
    private async Task Execute(string sql) {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql);
    }


    /// <summary>
    /// Loads the profile to fetch data
    /// The selection is made by:
    /// 1. the least contacts
    /// 2. active vendor
    /// 3. last edit no done by this procedure
    /// </summary>
    /// <returns>Vendor object</returns>
    public async Task<Vendor> SelectVendor() {
        CheckConnectionString();

        var sql = "SELECT TOP 1 [GUId], [NIF], [Name] " +
                  "FROM [vendor].[GetNextVendor]";
        return await Query<Vendor>(sql);
    }


    /// <summary>
    /// Mark vendor as processed
    /// </summary>
    /// <param name="guid">Vendor GUId</param>
    /// <returns></returns>
    public async Task MarkVendor(string guid) {
        CheckConnectionString();

        var sql = "UPDATE vendor.Profile " +
                  $"SET UpdatedBy = '{_workerName}', " +
                      "UpdatedAt = GETDATE() " +
                  $"WHERE [GUId] = '{guid}'";

        await Execute(sql);
    }


    /// <summary>
    /// Gets identifier for the given contact type
    /// Allows usage in different environments
    /// </summary>
    /// <param name="type">ContactType enum</param>
    /// <returns>string</returns>
    public async Task<string> GetContactTypeGUId(ContactType type) {
        CheckConnectionString();

        var sql = "SELECT TOP 1 CONVERT(NVARCHAR(37),[GUId]) " +
                  "FROM vendor.ContactType " +
                  $"WHERE [Name] = '{type}'";

        return await Query<string>(sql);
    }


    /// <summary>
    /// Updates the name of the vendor
    /// </summary>
    /// <param name="guid">Vendor GUId</param>
    /// <param name="name">New vendor name</param>
    public async Task UpdateVendorName(string guid, string name) {
        CheckConnectionString();

        var sql = "UPDATE vendor.Profile " +
                  $"SET Name = '{name.Replace("'", "")}', " +
                  $"    UpdatedAt = GETDATE(), " +
                  $"    UpdatedBy = '{_workerName}' " +
                  $"WHERE [GUId] = '{guid}'";

        await Execute(sql);
    }




    /// <summary>
    /// Updates the specific contact in the database.
    /// The value is only updated if the value was not inserted by an user
    /// and the value inserted by this procedure is different
    /// </summary>
    /// <param name="ct">ContactType</param>
    /// <param name="vendorGUId">Vendor GUID</param>
    /// <param name="contactValue">New Contact Value</param>
    public async Task UpdateVendorContact(ContactType ct, string vendorGUId, string contactValue) {

        // get contact guid
        var typeGUIdSql = "SELECT TOP 1 CONVERT(NVARCHAR(37),[GUId]) " +
                         "FROM vendor.ContactType " +
                         $"WHERE UPPER([Name]) = '{ct}'";
        var typeGUId = await Query<string>(typeGUIdSql);


        // get current contact for the type and vendor
        // with equal value or set by this procedure
        var currentContactSql = "SELECT TOP 1 [Value], CreatedBy " +
                              "FROM vendor.Contact " +
                             $"WHERE ProfileGUId = '{vendorGUId}' " +
                             $"   AND TypeGUId = '{typeGUId}' " +
                             $"   AND ([Value] = '{contactValue}' " +
                             $"     OR [CreatedBy] = '{_workerName}');";
        var currentContact = await Query<Contact>(currentContactSql);

        // create if the contact does not exists
        if (currentContact is null) {
            var insertSql = "INSERT INTO vendor.Contact ([Value],[Active],[Default],[ProfileGUId],[TypeGUId],[CreatedBy],[CreatedAt],[UpdatedBy],[UpdatedAt]) " +
                $"VALUES ('{contactValue.Replace("'", "")}',1,0,'{vendorGUId}','{typeGUId}','{_workerName}',GETDATE(),'{_workerName}',GETDATE());";
            await Execute(insertSql);
        }
        else {
            // update value if different and set by this procedure
            if (string.Equals(currentContact.CreatedBy, _workerName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(currentContact.Value, contactValue, StringComparison.OrdinalIgnoreCase)) {
                var updateContactSql = "UPDATE vendor.Contact " +
                                      $"SET [Value] = {contactValue.Replace("'", "")}, " +
                                      $"    UpdatedAt = GETDATE(), " +
                                      $"    UpdatedBy = {_workerName} " +
                                      $"WHERE ProfileGUId = '{vendorGUId}' " +
                                      $"   AND TypeGUId = '{typeGUId}' " +
                                      $"   AND [CreatedBy] = '{_workerName}');";

                await Execute(updateContactSql);
            }

            // ignore if the contact was set by some user
        }
    }


}
