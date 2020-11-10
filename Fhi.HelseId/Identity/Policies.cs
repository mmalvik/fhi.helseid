// ReSharper disable once CheckNamespace

using System.Linq;
using Fhi.HelseId.Web;
using Fhi.HelseId.Web.Hpr;

namespace Fhi.HelseId.Common.Identity
{
    public class Policies
    {
        public const string HidAuthenticated = nameof(HidAuthenticated);
        public const string ApiAccess = nameof(ApiAccess);
        public const string HprNummer = nameof(HprNummer);
        public const string GodkjentHprKategoriPolicy = nameof(GodkjentHprKategoriPolicy);
       
        /// <summary>
        /// Determine the presiding policy from configuration.
        /// Will return Policies.HidAuthenticated if no other policies are configured.
        /// </summary>
        /// <param name="helseIdWebKonfigurasjon"></param>
        /// <param name="hprFeatureFlags"></param>
        /// <returns></returns>
        public string DeterminePresidingPolicy(IHelseIdWebKonfigurasjon helseIdWebKonfigurasjon, IHprFeatureFlags hprFeatureFlags)
        {
            var policy = new[]
                {
                    new
                    {
                        PolicyActive = helseIdWebKonfigurasjon.UseHprNumber && hprFeatureFlags.UseHprPolicy,
                        Policy = Policies.GodkjentHprKategoriPolicy
                    },
                    new {PolicyActive = helseIdWebKonfigurasjon.UseHprNumber, Policy = Policies.HprNummer},
                    new {PolicyActive = true, Policy = Policies.HidAuthenticated}
                }
                .ToList()
                .First(p => p.PolicyActive).Policy;
            CurrentPolicy = policy;
            return policy;
        }

        public string CurrentPolicy { get; private set; } = "";
    }
}