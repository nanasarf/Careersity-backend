using Careersity.Application.ExternalLearning.Services; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace Careersity.Api.Controllers;
/// <summary>Self-confirmed progress for third-party URL resources; ExternalAssessment resources are not graded by Careersity.</summary>
[ApiController,Authorize,Route("api/me/career-enrollments/{enrollmentId:guid}/courses/{courseId:guid}/external-resources"),Tags("Learner External Resources")]
public sealed class LearnerExternalResourcesController(ILearnerExternalResourceProgressService service):ControllerBase
{
 [HttpGet]public async Task<IActionResult>List(Guid enrollmentId,Guid courseId,CancellationToken t)=>Ok(await service.ListAsync(enrollmentId,courseId,t));[HttpPost("{assignmentId:guid}/start")]public async Task<IActionResult>Start(Guid enrollmentId,Guid courseId,Guid assignmentId,CancellationToken t)=>Ok(await service.StartAsync(enrollmentId,courseId,assignmentId,t));[HttpPost("{assignmentId:guid}/complete")]public async Task<IActionResult>Complete(Guid enrollmentId,Guid courseId,Guid assignmentId,CancellationToken t)=>Ok(await service.CompleteAsync(enrollmentId,courseId,assignmentId,t));
}
