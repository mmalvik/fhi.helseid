using System.Collections.Generic;
using Fhi.HelseId.Common.Identity;
using Fhi.HelseId.Web;
using Fhi.HelseId.Web.ExtensionMethods;
using Fhi.HelseId.Web.Hpr;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;

namespace Fhi.HelseId.Tests.Web
{
    public class PoliciesTests
    {
        [TestCase(false, false, false, false, Policies.HidAuthenticated)]
        [TestCase(true, false, false, false, Policies.HidAuthenticated)]
        [TestCase(true, false, true, true, Policies.HidAuthenticated)]
        [TestCase(true, true, false, false, Policies.HprNummer)]
        [TestCase(true, true, true, false, Policies.HprNummer)]
        [TestCase(true,true,true,true,Policies.GodkjentHprKategoriPolicy)]
        public void DeterminePresidingPolicyTest(bool authUse, bool useHprNumber, bool useHpr, bool useHprPolicy,string expected)
        {
            var policy = new Policies();
            var config = Substitute.For<IHelseIdWebKonfigurasjon>();
            config.AuthUse.Returns(authUse);
            config.UseHprNumber.Returns(useHprNumber);
            var hprFlags = Substitute.For<IHprFeatureFlags>();
            hprFlags.UseHpr.Returns(useHpr);
            hprFlags.UseHprPolicy.Returns(useHprPolicy);
            var currentPolicy = policy.DeterminePresidingPolicy(config, hprFlags);
            Assert.That(currentPolicy, Is.EqualTo(expected));
        }
    }

    public class HelseIdWebProtectedPathsOptionsTests
    {
        [Test]
        public void ThatHelseIdWebProtectedPathsOptionsWorks()
        {
            var redirect = Substitute.For<IRedirectPagesKonfigurasjon>();
            var sut = new HelseIdWebProtectedPathsOptions("SomePolicy", redirect, new List<PathString> {@"/SomePath"});
            Assert.That(sut.Exclusions, Is.Not.Null);
            Assert.That(sut.Exclusions?.Count, Is.EqualTo(5));
            Assert.That(sut.Policy,Is.EqualTo("SomePolicy"));
        }
    }
}