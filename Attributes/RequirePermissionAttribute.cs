using FourSPM_WebService.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace FourSPM_WebService.Attributes
{
    /// <summary>
    /// Requires that the current user has the specified permission to access the endpoint
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IActionFilter
    {
        private readonly string _permissionName;

        public RequirePermissionAttribute(string permissionName)
        {
            _permissionName = permissionName;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Skip if user is authenticated via AllowAnonymous (unlikely but possible)
            if (context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowAnonymousAttribute))
            {
                return;
            }

            // Get logger
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RequirePermissionAttribute>>();

            // Only work with controllers that inherit from FourSPMODataController
            if (context.Controller is FourSPMODataController controller)
            {
                // Check if user has the required permission
                if (!controller.HasPermission(_permissionName))
                {
                    logger?.LogWarning($"User {controller.CurrentUser.Email} attempted to access resource requiring permission {_permissionName}");
                    context.Result = new ForbidResult();
                }
            }
            else
            {
                // Error if used on a controller that doesn't inherit from FourSPMODataController
                logger?.LogError($"RequirePermissionAttribute used on controller {context.Controller.GetType().Name} that does not inherit from FourSPMODataController");
                context.Result = new StatusCodeResult(500);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Nothing to do after the action executes
        }
    }
}
