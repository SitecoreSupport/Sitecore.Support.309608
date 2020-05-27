using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using Sitecore.ContentTesting.Configuration;
using Sitecore.ContentTesting.Diagnostics;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;

namespace Sitecore.Support.ContentTesting.Pipelines.Initialize
{
    public class RegisterWebApiActiveTestsRoute
    {
        public virtual void Process(PipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (Settings.IsAutomaticContentTestingEnabled)
            {
                RegisterActiveTestsRoute(RouteTable.Routes, args);
            }
        }

        protected virtual void RegisterActiveTestsRoute(RouteCollection routes, PipelineArgs args)
        {
            if (routes["Sitecore.Support.309608 - ActiveTests"] != null)
            {
                Logger.Warn("Route 'Sitecore.Support.309608 - ActiveTests' has already been added. Ensure only a single route processor for Content Testing.");
            }
            else
            {
                routes.MapHttpRoute("Sitecore.Support.309608 - ActiveTests", Settings.CommandRoutePrefix + "Tests/GetActiveTests", new
                {
                    controller = "Tests309608",
                    action = "GetActiveTests"
                });
            }
        }
    }
}