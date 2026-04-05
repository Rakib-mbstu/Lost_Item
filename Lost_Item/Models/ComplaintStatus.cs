namespace Lost_Item.Models;

public enum ComplaintStatus
{
    Pending  = 0,   // newly filed, awaiting admin review
    Approved = 1,   // admin approved — visible in public search
    Resolved = 2,   // item recovered
    Rejected = 3,   // admin dismissed
}