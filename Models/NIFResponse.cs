using Newtonsoft.Json;
using System.Collections.Generic;

namespace NIFPTWorker.Models;

public class NIFResponse {
    public string Result { get; set; }

    public string Message { get; set; }

    [JsonProperty("Records")]
    public Dictionary<string, NIFDto> Records { get; set; }
    public bool Nif_validation { get; set; }
    public bool Is_nif { get; set; }
    public Credits Credits { get; set; }
}


public class NIFDto {
    public int Nif { get; set; }
    public string Seo_url { get; set; }
    public string Title { get; set; }
    public string Address { get; set; }
    public string Pc4 { get; set; }
    public string Pc3 { get; set; }
    public string City { get; set; }
    public object Start_date { get; set; }
    public object Activity { get; set; }
    public string Status { get; set; }
    //public string Cae { get; set; }
    public Contacts Contacts { get; set; }
    public Structure Structure { get; set; }
    public Geo Geo { get; set; }
    public Place Place { get; set; }
    public string Racius { get; set; }
    public string Alias { get; set; }
    public string Portugalio { get; set; }
}

public class Contacts {
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Website { get; set; }
    public string Fax { get; set; }
}

public class Structure {
    public object Nature { get; set; }
    public object Capital { get; set; }
    public object Capital_currency { get; set; }
}

public class Geo {
    public string Region { get; set; }
    public string County { get; set; }
    public string Parish { get; set; }
}

public class Place {
    public string Address { get; set; }
    public string Pc4 { get; set; }
    public string Pc3 { get; set; }
    public string City { get; set; }
}

public class Credits {
    public string Used { get; set; }
    public Left Left { get; set; }
}

public class Left {
    public int Month { get; set; }
    public int Day { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int Paid { get; set; }
}
