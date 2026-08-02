namespace Careersity.Infrastructure.Initialization;
public sealed class SeedDataOptions
{
 public const string SectionName="SeedData";public bool Enabled{get;set;}public bool AllowInProduction{get;set;}public bool IncludeDemoAdministrator{get;set;}public bool IncludeDemoLearner{get;set;}public bool IncludeSampleCurriculum{get;set;}
 public string AdministratorEmail{get;set;}=string.Empty;public string AdministratorPassword{get;set;}=string.Empty;public string LearnerEmail{get;set;}=string.Empty;public string LearnerPassword{get;set;}=string.Empty;
}
