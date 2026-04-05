namespace GlobalDataCreatorETL.Core.Models;

public sealed record CountryMeta(
    int Id,
    string Name,
    string Shortcode,
    string ImportView,
    string ExportView,
    string ImportSP,
    string ExportSP,
    string TableName
);
