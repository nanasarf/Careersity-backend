namespace Careersity.Application.CurriculumImports.Dtos;

public sealed record CurriculumImportIssue(string Path, string Code, string Message);
public sealed record CurriculumImportCounts(int Categories, int Careers, int Skills, int Providers, int Instructors,
    int Resources, int Courses, int Lessons, int Pathways, int Levels, int Assignments);
public sealed record CurriculumImportValidationDto(bool IsValid, IReadOnlyCollection<CurriculumImportIssue> Errors,
    IReadOnlyCollection<CurriculumImportIssue> Warnings, CurriculumImportCounts Counts);
public sealed record CurriculumImportSummaryDto(Guid ImportId, CurriculumImportCounts Created, Guid CategoryId,
    string CategorySlug, Guid CareerId, string CareerSlug, Guid PathwayId);
