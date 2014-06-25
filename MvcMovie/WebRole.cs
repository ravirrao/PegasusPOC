using System.Diagnostics.CodeAnalysis;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace MvcCache
{
    public class WebRole : RoleEntryPoint
    {
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands"), SuppressMessage("Microsoft.Design", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "The call restricts the action updating the Logger Settings.")]
        public override bool OnStart()
        {
          

            return base.OnStart();
        }

    }    
}
