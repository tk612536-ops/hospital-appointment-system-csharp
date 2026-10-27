using System.Security.Claims;
using HospitalAppointments.Application.Security;

namespace HospitalAppointments.Web.Security;

public sealed class HttpCurrentActor(IHttpContextAccessor httpContext) : ICurrentActor
{
    public long? AccountId => long.TryParse(httpContext.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        && id > 0 ? id : null;
}
