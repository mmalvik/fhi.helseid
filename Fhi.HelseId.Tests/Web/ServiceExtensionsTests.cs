using System;
using System.Collections.Generic;
using Fhi.HelseId.Common.Identity;
using Fhi.HelseId.Web;
using Fhi.HelseId.Web.ExtensionMethods;
using Fhi.HelseId.Web.Hpr;
using Fhi.HelseId.Web.Middleware;
using Fhi.HelseId.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Fhi.HelseId.Tests.Web
{
    public class ServiceExtensionsTests
    {
        /// <summary>
        /// Tests AddHelseIdAuthorizationPolicy
        /// </summary>
        [TestCase(true, true, Policies.GodkjentHprKategoriPolicy)]
        [TestCase(true, false, Policies.HprNummer)]
        [TestCase(false, true, Policies.HidAuthenticated)]
        [TestCase(false, false, Policies.HidAuthenticated)]
        public void ThatAddingAuthorizationPoliciesHonourFeatureflags(bool useHprNumber, bool useHpr,
            string expectedPolicyName)
        {
            var helseIdFeatures = Substitute.For<IHelseIdHprFeatures>();
            helseIdFeatures.UseHprNumber.Returns(useHprNumber);
            var sc = Substitute.For<IServiceCollection>();
            var hprFeatures = Substitute.For<IHprFeatureFlags>();
            hprFeatures.UseHpr.Returns(useHpr);
            hprFeatures.UseHprPolicy.Returns(useHpr);
            var wl = Substitute.For<IWhitelist>();
            var helseIdWebConfig = Substitute.For<IHelseIdWebKonfigurasjon>();

            var sut = sc.AddHelseIdAuthorizationPolicy(helseIdFeatures, hprFeatures, helseIdWebConfig, wl);
            Assert.That(sut.PolicyName, Is.EqualTo(expectedPolicyName));
        }

    }
}
