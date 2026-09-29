namespace POSSystem.Models
{
    /// <summary>
    /// Enum for predefined staff/user roles in the POS system.
    /// Using enum avoids typos and ensures type-safe role definitions.
    /// </summary>
    public enum UserRole
    {
        Admin,
        Cashier,
        Manager
    }

    /// <summary>
    /// Enum for supported payment methods in sales transactions.
    /// Easily binds to dropdowns in Razor using Html.GetEnumSelectList.
    /// </summary>
    public enum PaymentMethodType
    {
        Cash,
        Card,
        UPI
    }
}
