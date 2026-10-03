namespace Hateoas;

/// <summary>
/// IANA-registered link relation types (https://www.iana.org/assignments/link-relations). Use these as-is;
/// anything application-specific is an extension relation and goes through the CURIE prefix instead.
/// </summary>
public static class LinkRelations
{
    public const string Self = "self";
    public const string Edit = "edit";
    public const string Collection = "collection";
    public const string Item = "item";
    public const string Related = "related";
    public const string Up = "up";
    public const string Alternate = "alternate";
    public const string First = "first";
    public const string Prev = "prev";
    public const string Next = "next";
    public const string Last = "last";
    public const string Curies = "curies";
}
