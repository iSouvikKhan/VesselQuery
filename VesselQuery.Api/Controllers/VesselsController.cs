using Microsoft.AspNetCore.Mvc;
using VesselQuery.Api.Contracts;
using VesselQuery.Core.Services;

namespace VesselQuery.Api.Controllers;

[ApiController]
[Route("api/vessels")]
[Produces("application/json")]
public sealed class VesselsController(IVesselQueryService queryService) : ControllerBase
{
    [HttpPost("query")]
    [ProducesResponseType<QueryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<QueryResponse> Query([FromBody] QueryRequest request)
    {
        var result = queryService.Execute(request.Query, request.Skip, request.Take);
        return Ok(new QueryResponse(request.Query, result.Total, request.Skip, request.Take, result.Items));
    }

    [HttpGet("fields")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<string>> GetFields() => Ok(queryService.GetFieldNames());
}
