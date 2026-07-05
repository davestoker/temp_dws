namespace ConfigurationManager.Core.Enums;

/// <summary>
/// Who owns / should see a configuration key in the management UI. Used to filter
/// the UI by audience; it is not itself an access-control enforcement mechanism.
/// </summary>
public enum VisibilityGroup
{
    Admin = 0,
    ITSupport = 1,
    Owner = 2
}
