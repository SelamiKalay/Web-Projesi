namespace WorkFlowBasic.Models;

public enum RequestStatus
{
    Draft = 0,      // Taslak
    Pending = 1,    // Onay Bekliyor
    Approved = 2,   // Onaylandı
    Rejected = 3,   // Reddedildi
    Cancelled = 4   // İptal Edildi
}

public enum LogAction
{
    Created = 0,
    Approved = 1,
    Rejected = 2,
    RequestRevision = 3
}