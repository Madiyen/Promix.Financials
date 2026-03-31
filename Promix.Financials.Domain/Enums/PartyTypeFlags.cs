using System;

namespace Promix.Financials.Domain.Enums;

[Flags]
public enum PartyTypeFlags
{
    None = 0,
    Customer = 1,
    Vendor = 2,
    Both = Customer | Vendor
}
