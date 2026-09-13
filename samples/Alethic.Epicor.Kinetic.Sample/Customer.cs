using Microsoft.OData.Client;

namespace Alethic.Epicor.Kinetic.Sample;

/// <summary>
/// Hand-written entity for the Customers entity set of Erp.BO.CustomerSvc, holding only the columns the sample shows.
/// </summary>
[Key("Company", "CustNum")]
public sealed class Customer
{

    /// <summary>
    /// Company the customer belongs to.
    /// </summary>
    public string Company { get; set; } = string.Empty;

    /// <summary>
    /// Internal customer number.
    /// </summary>
    public int CustNum { get; set; }

    /// <summary>
    /// User-facing customer ID.
    /// </summary>
    public string? CustID { get; set; }

    /// <summary>
    /// Customer name.
    /// </summary>
    public string? Name { get; set; }

}
