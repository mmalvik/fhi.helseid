﻿using Fhi.HelseId.Common.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fhi.HelseId.Web.Services
{
    public interface IHelseIdSecretHandler
    {
        void AddSecretConfiguration(IHelseIdWebKonfigurasjon configAuth, OpenIdConnectOptions options);
    }

    public class HelseIdJwkSecretHandler : IHelseIdSecretHandler
    {
        public void AddSecretConfiguration(IHelseIdWebKonfigurasjon configAuth, OpenIdConnectOptions options)
        {
            var jwk = File.ReadAllText(configAuth.ClientSecret);
            var jwkSecurityKey = new JsonWebKey(jwk);

            options.Events.OnAuthorizationCodeReceived = ctx =>
            {
                ctx.TokenEndpointRequest.ClientAssertionType = IdentityModel.OidcConstants.ClientAssertionTypes.JwtBearer;
                ctx.TokenEndpointRequest.ClientAssertion = ClientAssertion.Generate(configAuth, jwkSecurityKey);

                return Task.CompletedTask;
            };
        }
    }

    public class HelseIdRsaXmlSecretHandler : IHelseIdSecretHandler
    {
        public void AddSecretConfiguration(IHelseIdWebKonfigurasjon configAuth, OpenIdConnectOptions options)
        {
            var xml = File.ReadAllText(configAuth.ClientSecret);
            var rsa = RSA.Create();
            rsa.FromXmlString(xml);
            var rsaSecurityKey = new RsaSecurityKey(rsa);

            options.Events.OnAuthorizationCodeReceived = ctx =>
            {
                ctx.TokenEndpointRequest.ClientAssertionType = IdentityModel.OidcConstants.ClientAssertionTypes.JwtBearer;
                ctx.TokenEndpointRequest.ClientAssertion = ClientAssertion.Generate(configAuth, rsaSecurityKey);

                return Task.CompletedTask;
            };
        }
    }

    public class HelseIdEnterpriseCertificateSecretHandler : IHelseIdSecretHandler
    {
        public void AddSecretConfiguration(IHelseIdWebKonfigurasjon configAuth, OpenIdConnectOptions options)
        {
            var secretParts = configAuth.ClientSecret.Split(':');
            if(secretParts.Length != 2)
            {
                throw new InvalidEnterpriseCertificateSecretException(configAuth.ClientSecret);
            }

            var storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), secretParts[0]);
            var thumprint = secretParts[1];
            
            var store = new X509Store(storeLocation);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumprint, true);

            if(certificates.Count == 0)
            {
                throw new Exception($"No certificate with thumbprint {options.ClientSecret} found in store LocalMachine");
            }

            var x509SecurityKey = new X509SecurityKey(certificates[0]);

            options.Events.OnAuthorizationCodeReceived = ctx =>
            {
                ctx.TokenEndpointRequest.ClientAssertionType = IdentityModel.OidcConstants.ClientAssertionTypes.JwtBearer;
                ctx.TokenEndpointRequest.ClientAssertion = ClientAssertion.Generate(configAuth, x509SecurityKey);

                return Task.CompletedTask;
            };
        }

        public class InvalidEnterpriseCertificateSecretException : Exception
        {
            private const string StandardMessage = "For enterprise certificates we expect secret in the format STORE:Thumbprint. For example: 'LocalMachine:1234567890'";

            public InvalidEnterpriseCertificateSecretException(string secret) : base(StandardMessage)
            {
                Secret = secret;
            }

            public string Secret { get; }
        }
    }

    public class HelseIdSharedSecretHandler : IHelseIdSecretHandler
    {
        public void AddSecretConfiguration(IHelseIdWebKonfigurasjon configAuth, OpenIdConnectOptions options)
        {
            options.ClientSecret = configAuth.ClientSecret;
        }
    }

}
