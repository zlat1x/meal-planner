using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace MealPlanner.Web.Security;

public class AdminOnlyControllersConvention : IControllerModelConvention
{
    private readonly HashSet<string> _controllerNames;

    public AdminOnlyControllersConvention(params string[] controllerNames)
    {
        _controllerNames = controllerNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public void Apply(ControllerModel controller)
    {
        if (_controllerNames.Contains(controller.ControllerName))
        {
            controller.Filters.Add(new AuthorizeFilter(
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireRole("Admin")
                    .Build()));
        }
    }
}