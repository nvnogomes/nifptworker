# NIF.PT Worker

This worker gathers information from public APIs of vendors registered in the vendors application.
Key used for the match is the NIF.

## APIs used
- www.nif.pt


## Information gathered
- **Name of the vendor**
- Contacts of types
    - ADDRESS
    - EMAIL
    - MOBILEPHONE
    - TELEPHONE
    - WEBSITE

## Vendors selection
This view will get the vendor with the least contacts not changed by this service.


## Configuration

Type|Location|Data
-|-|-
Configuration|appsettings.json|Serilog, API info, ConnectionString



## Logs
Type|Location|Information
-|-|-
Application|ServiceLog_<YEARMONTH>.log|Information and above

---