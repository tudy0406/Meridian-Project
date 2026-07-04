using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Common.Web;

/// <summary>
/// Base for all API controllers. Controllers stay thin: they bind/validate input
/// (via <c>[ApiController]</c> model validation) and delegate to services. No
/// business logic lives here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
}
