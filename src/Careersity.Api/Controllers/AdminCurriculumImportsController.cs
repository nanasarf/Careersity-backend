using Careersity.Api.Infrastructure; using Careersity.Application.CurriculumImports.Requests; using Careersity.Application.CurriculumImports.Services; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.RateLimiting;
namespace Careersity.Api.Controllers;
[ApiController,Authorize(Policy=SecurityPolicies.AdministratorOnly),Route("api/admin/curriculum-imports"),Tags("Admin Curriculum Imports")]
[RequestSizeLimit(5*1024*1024)]
[EnableRateLimiting(SecurityPolicies.MutationRateLimit)]
public sealed class AdminCurriculumImportsController(ICurriculumImportService imports):ControllerBase
{
 [HttpPost("validate")]public async Task<IActionResult>Validate(CurriculumImportRequest request,CancellationToken token)=>Ok(await imports.ValidateAsync(request,token));
 [HttpPost]public async Task<IActionResult>Import(CurriculumImportRequest request,CancellationToken token)=>StatusCode(StatusCodes.Status201Created,await imports.ImportAsync(request,token));
}
