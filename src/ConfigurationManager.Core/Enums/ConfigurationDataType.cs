namespace ConfigurationManager.Core.Enums;

/// <summary>
/// The data type of a configuration key's value, used for display formatting and validation.
/// <see cref="Secret"/> keys are always encrypted at rest regardless of the IsSensitive flag.
/// </summary>
public enum ConfigurationDataType
{
    String = 0,
    Int = 1,
    Decimal = 2,
    Boolean = 3,
    DateTime = 4,
    Json = 5,
    Secret = 6
}
