namespace Temple.Domain.Taxonomy;

public class ReligionTaxonomy
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Type { get; set; } = string.Empty; // religion / sect
    public string DisplayName { get; set; } = string.Empty;
    public string[] CanonicalTexts { get; set; } = System.Array.Empty<string>();
    public string? DefaultTerminologyJson { get; set; }
}
